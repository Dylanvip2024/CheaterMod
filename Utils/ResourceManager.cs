using System.Reflection;
using UnityEngine;

namespace FullBrightMod.Utils
{
    /// <summary>
    /// 嵌入资源管理器 —— 读取 .csproj 中声明的 EmbeddedResource。
    /// 资源路径规则：由于 logo.png 在项目根目录，其清单名称为 {AssemblyName}.logo.png
    /// 即 "CheaterMod.logo.png"。
    /// </summary>
    public static class ResourceManager
    {
        private static Texture2D _logoTexture;
        private static bool _logoLoaded;

        /// <summary>
        /// 缓存的 Logo 纹理。首次访问时从嵌入资源加载，之后返回缓存对象。
        /// </summary>
        public static Texture2D LogoTexture
        {
            get
            {
                if (!_logoLoaded)
                {
                    _logoTexture = LoadEmbeddedTexture("CheaterMod.logo.png");
                    _logoLoaded = true;
                }
                return _logoTexture;
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

        /// <summary>释放所有缓存的资源（模块 OnDisable 时调用）</summary>
        public static void ReleaseAll()
        {
            if (_logoTexture != null)
            {
                Object.Destroy(_logoTexture);
                _logoTexture = null;
            }
            _logoLoaded = false;
        }
    }
}
