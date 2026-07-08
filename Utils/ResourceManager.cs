using System.Reflection;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Utils
{
    public static class ResourceManager
    {
        private static Texture2D _logoTexture;
        private static bool _logoLoaded;

        private static Texture2D _logo2Texture;
        private static bool _logo2Loaded;

        private static Texture2D _cmLogoTexture;
        private static bool _cmLogoLoaded;

        private static Material _glowMaterial;
        private static bool _glowMaterialLoaded;

        public static Texture2D LogoTexture
        {
            get
            {
                if (!_logoLoaded)
                {
                    _logoTexture = LoadEmbeddedTexture("CheaterMod.Resources.logo.png");
                    _logoLoaded = true;
                }
                return _logoTexture;
            }
        }

        public static Texture2D Logo2Texture
        {
            get
            {
                if (!_logo2Loaded)
                {
                    _logo2Texture = LoadEmbeddedTexture("CheaterMod.Resources.logo2.png");
                    _logo2Loaded = true;
                }
                return _logo2Texture;
            }
        }

        /// <summary>根据 GlobalSettings 返回当前使用的 Logo 纹理</summary>
        public static Texture2D ActiveLogoTexture
        {
            get
            {
                return GlobalSettings.LogoIndex == 1 ? Logo2Texture : LogoTexture;
            }
        }

        public static Texture2D CMLogoTexture
        {
            get
            {
                if (!_cmLogoLoaded)
                {
                    _cmLogoTexture = LoadEmbeddedTexture("CheaterMod.Resources.CheaterMod.png");
                    _cmLogoLoaded = true;
                }
                return _cmLogoTexture;
            }
        }

        public static Material GlowMaterial
        {
            get
            {
                if (!_glowMaterialLoaded)
                {
                    LoadEmbeddedBundle("CheaterMod.Resources.glowesp.bundle", out _glowMaterial);
                    _glowMaterialLoaded = true;
                }
                return _glowMaterial;
            }
        }

        /// <summary>从嵌入资源名称加载 Texture2D</summary>
        private static Texture2D LoadEmbeddedTexture(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogWarning($"[CheaterMod] 嵌入资源未找到: {resourceName}");
                    return null;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                tex.hideFlags = HideFlags.HideAndDontSave;
                tex.filterMode = FilterMode.Bilinear;

                if (tex.LoadImage(buffer))
                {
                    tex.Apply();
                    return tex;
                }

                Debug.LogWarning("[CheaterMod] 嵌入资源图片加载失败");
                Object.Destroy(tex);
                return null;
            }
        }

        /// <summary>
        /// 从嵌入资源加载 AssetBundle 并提取 GlowMaterial。
        /// </summary>
        private static void LoadEmbeddedBundle(string resourceName, out Material material)
        {
            material = null;
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Debug.LogWarning($"[CheaterMod] 嵌入资源未找到: {resourceName}");
                    return;
                }

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                AssetBundle bundle = AssetBundle.LoadFromMemory(buffer);
                if (bundle == null)
                {
                    Debug.LogWarning($"[CheaterMod] AssetBundle 加载失败: {resourceName}");
                    return;
                }

                material = bundle.LoadAsset<Material>("GlowMaterial");
                bundle.Unload(false);

                if (material == null)
                    Debug.LogWarning("[CheaterMod] AssetBundle 中未找到 GlowMaterial");
            }
        }

        public static void ReleaseAll()
        {
            if (_logoTexture != null)
            {
                Object.Destroy(_logoTexture);
                _logoTexture = null;
            }
            _logoLoaded = false;

            if (_logo2Texture != null)
            {
                Object.Destroy(_logo2Texture);
                _logo2Texture = null;
            }
            _logo2Loaded = false;

            if (_glowMaterial != null)
            {
                Object.Destroy(_glowMaterial);
                _glowMaterial = null;
            }
            _glowMaterialLoaded = false;
        }
    }
}

