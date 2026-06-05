using UnityEngine;
using FullBrightMod.Core;

namespace FullBrightMod.Utils
{
    /// <summary>
    /// UISoundHelper —— 纯代码合成模块开关提示音。
    /// 开启 → 800Hz 高音，关闭 → 400Hz 低音。
    /// 不依赖外部音频文件。内置懒加载防错，即使未显式 Init() 也能自动恢复。
    /// </summary>
    public static class UISoundHelper
    {
        private static AudioSource _audioSource;
        private static AudioClip _clipOn;
        private static AudioClip _clipOff;

        /// <summary>
        /// 初始化：创建 AudioSource + 合成 AudioClip。
        /// 应由 Plugin.Awake() 尽早调用，但即使遗漏也会在首次播放时自动补调。
        /// </summary>
        public static void Init()
        {
            // ★ 防重入：已初始化则直接返回
            if (_audioSource != null) return;

            // 创建持久化 GameObject
            var go = new GameObject("CheaterMod_UISound");
            Object.DontDestroyOnLoad(go);
            _audioSource = go.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0.3f;
            _audioSource.playOnAwake = false;

            // 合成音效：800Hz 开启音，400Hz 关闭音
            _clipOn  = CreateBeep(800f, 0.1f);
            _clipOff = CreateBeep(400f, 0.1f);
        }

        /// <summary>
        /// 合成短促正弦波提示音，带线性淡出包络防爆音。
        /// </summary>
        private static AudioClip CreateBeep(float frequency, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                // 正弦波 + 线性淡出包络
                float envelope = 1f - (float)i / samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }

            AudioClip clip = AudioClip.Create("ToggleBeep", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 播放开关音效。
        /// </summary>
        /// <param name="enabled">true=开启音(800Hz), false=关闭音(400Hz)</param>
        public static void PlayToggleSound(bool enabled)
        {
            if (!GlobalSettings.EnableToggleSound) return;

            // ★ 懒加载防错：如果还没初始化或场景切换导致组件丢失，则重新初始化
            if (_audioSource == null)
            {
                Init();
                // 如果初始化后依然为 null，说明引擎层面存在问题，安全退出
                if (_audioSource == null) return;
            }

            AudioClip clip = enabled ? _clipOn : _clipOff;
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }
    }
}
