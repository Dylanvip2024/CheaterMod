using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using FullBrightMod.Core;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using UnityEngine;
using UnityEngine.Networking;

namespace FullBrightMod.Patches
{
    [HarmonyPatch]
    internal static class STTPatch
    {
        private static readonly Dictionary<ulong, List<float>> _pendingAudio = new Dictionary<ulong, List<float>>();
        private static readonly Dictionary<ulong, float> _lastPacketTime = new Dictionary<ulong, float>();
        
        // 升级为主动协程句柄追踪字典，实现滑动窗口重置
        private static readonly Dictionary<ulong, Coroutine> _activeCoroutines = new Dictionary<ulong, Coroutine>();

        private const float SILENCE_TIMEOUT = 0.6f;   
        private const float MAX_BUFFER_SECONDS = 5f;  
        private const int TARGET_SAMPLE_RATE = 16000; 

        [HarmonyTargetMethod]
        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(StreamedAudioOutput), "RecieveVoiceBlob");
        }

        [HarmonyPostfix]
        private static void OnRecieveVoiceBlob(StreamedAudioOutput __instance, ushort freq, ref float[] data, ref bool is_not_last)
        {
            if (!ModSettings.IsSTTEnabled) return;
            if (data == null || data.Length == 0) return;

            try { if (!Traverse.Create(__instance).Field("is_voice").GetValue<bool>()) return; }
            catch { return; }

            ulong steamId = 0;
            string playerName = "未知";

            try
            {
                // 终极修复方案：多级立体轰炸式组件与反射探测
                NetPlayer plr = null;
                plr = __instance.GetComponent<NetPlayer>() ?? __instance.GetComponentInParent<NetPlayer>();
                
                if (plr == null)
                {
                    // 盲测：遍历所有非公开字段，抓取类型为 NetPlayer 的实例
                    var fields = __instance.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    foreach (var f in fields)
                    {
                        if (f.FieldType == typeof(NetPlayer))
                        {
                            plr = (NetPlayer)f.GetValue(__instance);
                            if (plr != null) break;
                        }
                    }
                }

                if (plr != null)
                {
                    steamId = plr.steam_id;
                    playerName = plr.playername ?? "未知";
                }
            }
            catch { }

            if (steamId == 0) return; // 此时绝不可能为0，安全放行
            _lastPacketTime[steamId] = Time.realtimeSinceStartup;

            if (!_pendingAudio.TryGetValue(steamId, out var buffer))
            {
                buffer = new List<float>((int)(freq * MAX_BUFFER_SECONDS));
                _pendingAudio[steamId] = buffer;
            }
            buffer.AddRange(data);

            // 长度超限截断
            int maxSamples = (int)(freq * MAX_BUFFER_SECONDS);
            if (buffer.Count >= maxSamples)
            {
                var half = buffer.GetRange(0, buffer.Count / 2);
                buffer.RemoveRange(0, buffer.Count / 2);
                FullBrightPlugin.Instance.StartCoroutine(SendToWhisperCoroutine(steamId, playerName, half.ToArray(), freq));
            }

            // 滑动窗口 VAD：只要收到新包，立刻刷新计时器（掐断老协程，重启新协程）
            if (_activeCoroutines.TryGetValue(steamId, out var runningCoro))
            {
                if (runningCoro != null) FullBrightPlugin.Instance.StopCoroutine(runningCoro);
                _activeCoroutines.Remove(steamId);
            }

            _activeCoroutines[steamId] = FullBrightPlugin.Instance.StartCoroutine(SilenceDetectCoroutine(steamId, playerName, freq));
        }

        private static IEnumerator SilenceDetectCoroutine(ulong steamId, string playerName, ushort freq)
        {
            // 每100ms探测一次静音阈值
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForSecondsRealtime(0.1f);

                if (!ModSettings.IsSTTEnabled)
                {
                    CleanupSpeaker(steamId);
                    yield break;
                }

                if (!_lastPacketTime.TryGetValue(steamId, out var lastTime))
                {
                    CleanupSpeaker(steamId);
                    yield break;
                }

                if (Time.realtimeSinceStartup - lastTime >= SILENCE_TIMEOUT)
                {
                    TriggerFinalRecognition(steamId, playerName, freq);
                    yield break;
                }
            }

            // 3秒兜底截断
            TriggerFinalRecognition(steamId, playerName, freq);
        }

        private static void TriggerFinalRecognition(ulong steamId, string playerName, ushort freq)
        {
            if (_pendingAudio.TryGetValue(steamId, out var buffer) && buffer.Count > 0)
            {
                var samples = buffer.ToArray();
                buffer.Clear();
                FullBrightPlugin.Instance.StartCoroutine(SendToWhisperCoroutine(steamId, playerName, samples, freq));
            }
            _activeCoroutines.Remove(steamId);
        }

        internal static byte[] EncodeToWAV(float[] samples, int sampleRate)
        {
            float[] resampled;
            if (sampleRate == TARGET_SAMPLE_RATE)
            {
                resampled = samples;
            }
            else
            {
                double ratio = (double)TARGET_SAMPLE_RATE / sampleRate;
                int newLen = (int)(samples.Length * ratio);
                resampled = new float[newLen];
                for (int i = 0; i < newLen; i++)
                {
                    double srcIndex = i / ratio;
                    int srcInt = (int)srcIndex;
                    float frac = (float)(srcIndex - srcInt);
                    if (srcInt + 1 < samples.Length)
                        resampled[i] = samples[srcInt] * (1f - frac) + samples[srcInt + 1] * frac;
                    else
                        resampled[i] = samples[srcInt];
                }
            }

            int byteCount = resampled.Length * 2;
            byte[] wav = new byte[44 + byteCount];

            using (var ms = new MemoryStream(wav))
            using (var bw = new BinaryWriter(ms, Encoding.ASCII))
            {
                bw.Write("RIFF".ToCharArray());
                bw.Write(36 + byteCount); 
                bw.Write("WAVE".ToCharArray());
                bw.Write("fmt ".ToCharArray());
                bw.Write(16);           
                bw.Write((short)1);     
                bw.Write((short)1);     // 单声�?                bw.Write(TARGET_SAMPLE_RATE);
                bw.Write(TARGET_SAMPLE_RATE * 2); // ByteRate
                bw.Write((short)2);     // BlockAlign
                bw.Write((short)16);    

                bw.Write("data".ToCharArray());
                bw.Write(byteCount);

                for (int i = 0; i < resampled.Length; i++)
                {
                    float sample = Mathf.Clamp(resampled[i], -1f, 1f);
                    short val = (short)(sample * 32767f);
                    bw.Write(val);
                }
            }
            return wav;
        }

        private static IEnumerator SendToWhisperCoroutine(ulong steamId, string playerName, float[] samples, ushort freq)
        {
            if (samples == null || samples.Length == 0) yield break;

            byte[] wavData = EncodeToWAV(samples, freq);
            if (wavData == null || wavData.Length <= 44) yield break;

            string url = ModSettings.STTServerUrl;
            if (string.IsNullOrEmpty(url)) url = "[http://127.0.0.1:8080/inference](http://127.0.0.1:8080/inference)";

            // 规范表单构建
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
            form.AddField("response_format", "json");

            // 语言强制：如果用户选择了非 Auto 的语言，传给Whisper
            string[] sttLangCodes = { "", "zh", "en", "ru", "ja", "ko", "es", "fr", "de" };
            int langIdx = ModSettings.STTLanguageIndex;
            if (langIdx > 0 && langIdx < sttLangCodes.Length)
            {
                string langCode = sttLangCodes[langIdx];
                if (!string.IsNullOrEmpty(langCode))
                    form.AddField("language", langCode);
            }

            using (var req = UnityWebRequest.Post(url, form))
            {
                req.timeout = 30;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string rawResponse = req.downloadHandler.text?.Trim();
                    if (!string.IsNullOrEmpty(rawResponse))
                    {
                        string resultText = rawResponse;
                        try
                        {
                    if (rawResponse.StartsWith("{") && rawResponse.EndsWith("}"))
                    {
                        var json = Newtonsoft.Json.Linq.JObject.Parse(rawResponse);
                        if (json["text"] != null)
                        {
                            resultText = json["text"].ToString().Trim();
                        }
                    }
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(resultText) && resultText.Length > 0 && !resultText.Contains("[BLANK_AUDIO]"))
                        {
                            Chat.LogMessage(playerName, "<color=#00FF00>[语音]</color> " + resultText, true);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[STT] Whisper HTTP 请求失败: {req.responseCode} {req.error}");
                }
            }
        }

        private static void CleanupSpeaker(ulong steamId)
        {
            _pendingAudio.Remove(steamId);
            _lastPacketTime.Remove(steamId);
            if (_activeCoroutines.TryGetValue(steamId, out var c))
            {
                if (c != null) FullBrightPlugin.Instance.StopCoroutine(c);
            }
            _activeCoroutines.Remove(steamId);
        }

        internal static void CleanupAll()
        {
            _pendingAudio.Clear();
            _lastPacketTime.Clear();
            foreach (var kv in _activeCoroutines)
            {
                if (kv.Value != null) FullBrightPlugin.Instance.StopCoroutine(kv.Value);
            }
            _activeCoroutines.Clear();
        }
    }
}