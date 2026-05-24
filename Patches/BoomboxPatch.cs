using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using FullBrightMod.Core;
using KrokoshaCasualtiesMP;
using System.IO;
using System.Collections;

namespace FullBrightMod.Patches
{
    // ==========================================
    // 📻 音响管理器 (修复版：带状态重置机制)
    // ==========================================
    public class BoomboxManager : MonoBehaviour
    {
        public static BoomboxManager Instance;
        
        private AudioSource _localAudioSource;
        private AudioClip _currentClip;
        private int _lastTrackIndex = -1;
        private int _networkSamplePosition = 0;

        private void Awake()
        {
            Instance = this;
            _localAudioSource = gameObject.AddComponent<AudioSource>();
            _localAudioSource.loop = true;          
            _localAudioSource.spatialBlend = 0f;    
            _localAudioSource.volume = 0.5f;        
        }

        private void Update()
        {
            // 如果模块关闭，停止本地播放
            if (!Settings.IsBoomboxEnabled)
            {
                if (_localAudioSource.isPlaying) _localAudioSource.Stop();
                
                // ✨【核心 Bug 修复点】✨
                // 关闭模块时强行重置缓存索引与网络指针！
                // 这样下次重新勾选开启模块时，即便还是同一首歌，也能顺利再次触发加载与播放。
                _lastTrackIndex = -1; 
                _networkSamplePosition = 0;
                return;
            }

            // 检查 UI 上的歌曲是否被切换
            if (Settings.CurrentTrackIndex != _lastTrackIndex && Settings.AvailableMusicTracks.Count > 0)
            {
                _lastTrackIndex = Settings.CurrentTrackIndex;
                string fileName = Settings.AvailableMusicTracks[_lastTrackIndex];
                string filePath = Path.Combine(Application.dataPath, "../music", fileName);
                
                StartCoroutine(LoadAudioFile(filePath));
            }

            // 取消了说话避让，本地音量恒定维持在 0.5f 稳定输出
            _localAudioSource.volume = 0.5f;
        }

        private IEnumerator LoadAudioFile(string path)
        {
            string uri = "file://" + path.Replace("\\", "/");
            AudioType audioType = path.EndsWith(".mp3") ? AudioType.MPEG : AudioType.WAV;

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                www.timeout = 5; // 设置超时防止文件损坏卡死
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    _currentClip = DownloadHandlerAudioClip.GetContent(www);
                    _localAudioSource.clip = _currentClip;
                    _localAudioSource.Play();
                    _networkSamplePosition = _localAudioSource.timeSamples; // 指针与当前同步
                    
                    PlayerCamera.main?.DoAlert($"<color=cyan>🎵 正在播放: {Path.GetFileName(path)}</color>", false);
                }
                else
                {
                    PlayerCamera.main?.DoAlert($"<color=red>加载音频失败: {www.error}</color>", false);
                }
            }
        }

        // 获取下一帧的音乐数据 (供给麦克风网络流)
        public void GetNextMusicFrame(float[] buffer)
        {
            if (_currentClip == null || !_localAudioSource.isPlaying)
            {
                System.Array.Clear(buffer, 0, buffer.Length);
                return;
            }

            int targetSampleRate = Voicechat.SAMPLE_RATE; 
            int sourceSampleRate = _currentClip.frequency; 

            float ratio = (float)sourceSampleRate / targetSampleRate;
            int requiredInputSamples = Mathf.RoundToInt(buffer.Length * ratio);
            
            float[] rawInput = new float[requiredInputSamples];

            int localPos = _localAudioSource.timeSamples;
            // 网络流进度与本地出现严重偏差或单曲重播时，强制同步指针位置
            if (Mathf.Abs(_networkSamplePosition - localPos) > sourceSampleRate * 0.1f)
            {
                _networkSamplePosition = localPos;
            }

            if (_networkSamplePosition + requiredInputSamples < _currentClip.samples)
            {
                _currentClip.GetData(rawInput, _networkSamplePosition);
            }

            _networkSamplePosition += requiredInputSamples;

            // 完美重采样
            float[] resampled = Voicechat.ResampleVoiceBlob(rawInput, sourceSampleRate, targetSampleRate);

            System.Array.Clear(buffer, 0, buffer.Length);
            int copyLength = Mathf.Min(buffer.Length, resampled.Length);
            System.Array.Copy(resampled, buffer, copyLength);
        }
    }

    // ==========================================
    // 🎙️ 语音数据流劫持补丁
    // ==========================================
    [HarmonyPatch(typeof(Voicechat), "Client_SendMicDataFrame")]
    internal static class BoomboxPatch
    {
        [HarmonyPatch(typeof(Voicechat), "Start")]
        [HarmonyPostfix]
        private static void StartPostfix(Voicechat __instance)
        {
            if (BoomboxManager.Instance == null)
            {
                GameObject managerObj = new GameObject("BoomboxManager");
                Object.DontDestroyOnLoad(managerObj);
                managerObj.AddComponent<BoomboxManager>();
            }
        }

        [HarmonyPrefix]
        private static void Prefix(float[] data)
        {
            if (!Settings.IsBoomboxEnabled || BoomboxManager.Instance == null) return;

            bool isTalking = Voicechat.IsPushToTalkKeyPushed() || (Voicechat.my_output_volume > Voicechat.MINIMUM_VOLUME_TRESHOLD);

            // 无论如何都高频空转推进采样指针，彻底杜绝跳帧卡顿
            float[] musicFrame = new float[data.Length];
            BoomboxManager.Instance.GetNextMusicFrame(musicFrame);

            // 移除避让条件判断，直接进行全时段音频处理
            for (int i = 0; i < data.Length; i++)
            {
                if (isTalking)
                {
                    // 混音广播：同时完美保留你的说话声与音乐声，防止尖锐爆音
                    data[i] = Mathf.Clamp(data[i] + musicFrame[i] * 0.7f, -1f, 1f);
                }
                else
                {
                    // 纯音乐广播：没人说话时，直接传输高纯度音乐数据
                    data[i] = musicFrame[i];
                }
            }
            
            Voicechat.my_output_volume = 1f; 
        }
    }

    // ==========================================
    // 🎙️ 强制录音引擎补丁
    // ==========================================
    [HarmonyPatch(typeof(Voicechat), "RecordMicUpdate")]
    internal static class ForceMicRecordPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            if (Settings.IsBoomboxEnabled)
            {
                Traverse.Create(typeof(Voicechat)).Field("_push_to_talk_pushed").SetValue(true);
                Voicechat.AlwaysOn_TimeBelowTreshold = 0f;
            }
        }
    }
}