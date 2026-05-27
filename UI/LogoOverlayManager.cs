using FullBrightMod.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FullBrightMod.UI
{
    /// <summary>
    /// Logo 水印管理器 —— 使用原生 UGUI (Canvas + RawImage) 渲染内嵌 Logo。
    /// CanvasGroup.blocksRaycasts = false 确保鼠标完全穿透，不阻挡游戏交互。
    /// </summary>
    public class LogoOverlayManager : MonoBehaviour
    {
        private GameObject _canvasObj;
        private CanvasGroup _canvasGroup;
        private RawImage _logoImage;
        private RectTransform _logoRt;

        private void Start()
        {
            var logoTexture = Utils.ResourceManager.LogoTexture;
            if (logoTexture == null) return;

            // ---- 1. 创建 Canvas ----
            _canvasObj = new GameObject("CheaterMod_LogoCanvas");
            _canvasObj.transform.SetParent(transform, worldPositionStays: false);
            DontDestroyOnLoad(_canvasObj);

            var canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = _canvasObj.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // ---- 2. 创建 RawImage 显示 Logo ----
            var imgObj = new GameObject("LogoImage");
            imgObj.transform.SetParent(_canvasObj.transform, worldPositionStays: false);

            _logoImage = imgObj.AddComponent<RawImage>();
            _logoImage.texture = logoTexture;
            _logoImage.raycastTarget = false;

            _logoRt = imgObj.GetComponent<RectTransform>();
            _logoRt.anchorMin = new Vector2(0f, 1f);
            _logoRt.anchorMax = new Vector2(0f, 1f);
            _logoRt.pivot     = new Vector2(0f, 1f);
            _logoRt.anchoredPosition = new Vector2(30f, -30f);

            // 初始大小
            UpdateLogoSize();
        }

        private void Update()
        {
            if (_canvasObj == null) return;

            bool shouldShow = GlobalSettings.EnableLogoOverlay && _logoImage != null && _logoImage.texture != null;
            if (_canvasObj.activeSelf != shouldShow)
                _canvasObj.SetActive(shouldShow);

            if (shouldShow)
                UpdateLogoSize();
        }

        private void UpdateLogoSize()
        {
            if (_logoImage == null || _logoImage.texture == null || _logoRt == null) return;

            var tex = _logoImage.texture;
            float aspect = (float)tex.width / tex.height;
            float height = GlobalSettings.LogoOverlayHeight;
            float width = height * aspect;

            _logoRt.sizeDelta = new Vector2(width, height);
        }
    }
}
