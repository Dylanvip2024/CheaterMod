using FullBrightMod.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// 肢体状态常驻 HUD 模块 —— 克隆原版肢体图标的坐标/贴图（皮囊），
    /// 然后独立计算状态颜色（灵魂），完全不受原版 WoundView 开关影响。
    ///
    /// 颜色规则：
    ///   截肢 → 深灰
    ///   骨折/脱臼 → 深红
    ///   健康(>80%) → 白色原色
    ///   警告(40%~80%) → 橙黄
    ///   危险(<40%) → 纯红
    ///   流血 → Alpha 正弦闪烁 0.3~1.0
    /// </summary>
    public class LimbStatusModule : ModuleBase
    {
        public override string Name => "Limb Status HUD";
        public override string Description => "Always-on limb health display cloned from native WoundView.";
        public override ModuleCategory Category => (ModuleCategory)999;

        // ---- Canvas ----
        private GameObject _hudCanvasObj;
        private Canvas _hudCanvas;
        private CanvasGroup _canvasGroup;

        // ---- 总容器 ----
        private RectTransform _containerRt;

        // ---- 克隆的肢体 Image ----
        private struct LimbIconPair
        {
            public Image iconImage;
            public RectTransform iconRt;
        }

        private LimbIconPair[] _limbIcons;
        private int _limbCount;

        // ---- 原版 WoundView 引用（仅用于偷坐标和 Sprite） ----
        private Image[] _sourceLimbImages;

        // ---- 预设颜色 ----
        private static readonly Color ColorDismembered = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color ColorBroken     = new Color(0.6f, 0f, 0f, 1f);
        private static readonly Color ColorHealthy    = Color.white;
        private static readonly Color ColorWarn       = new Color(1f, 0.8f, 0f, 1f);
        private static readonly Color ColorDanger     = Color.red;

        // ============================================================
        // 生命周期
        // ============================================================

        public override void OnEnable() { }

        public override void OnDisable()
        {
            if (_hudCanvasObj != null)
            {
                Object.Destroy(_hudCanvasObj);
                _hudCanvasObj = null;
                _hudCanvas = null;
                _canvasGroup = null;
                _containerRt = null;
            }
            _limbIcons = null;
            _sourceLimbImages = null;
        }

        public override void OnUpdate()
        {
            if (!GlobalSettings.EnableLimbHUD) return;

            // 延迟初始化
            if (_hudCanvasObj == null)
            {
                InitializeHUD();
                if (_hudCanvasObj == null) return;
            }

            if (_limbIcons == null || _sourceLimbImages == null) return;

            // ★ 动态显隐控制：检测玩家是否死亡或场景已卸载 ★
            if (_hudCanvasObj != null)
            {
                bool isAliveAndValid = PlayerCamera.main != null &&
                                       PlayerCamera.main.body != null &&
                                       PlayerCamera.main.body.alive;

                if (!isAliveAndValid)
                {
                    if (_hudCanvasObj.activeSelf)
                        _hudCanvasObj.SetActive(false);
                    return;
                }
                else
                {
                    if (!_hudCanvasObj.activeSelf)
                        _hudCanvasObj.SetActive(true);
                }
            }

            if (PlayerCamera.main == null || PlayerCamera.main.body == null) return;

            var body = PlayerCamera.main.body;
            if (body.limbs == null) return;

            // 更新容器位置和缩放
            _containerRt.anchoredPosition = GlobalSettings.LimbHUDPosition;
            _containerRt.localScale = Vector3.one * GlobalSettings.LimbHUDScale;

            // 同步原版 Sprite（原版可能动态切换图标）
            for (int i = 0; i < _limbCount && i < _sourceLimbImages.Length; i++)
            {
                if (_sourceLimbImages[i] != null)
                    _limbIcons[i].iconImage.sprite = _sourceLimbImages[i].sprite;
            }

            // ★ 自定义状态颜色驱动 ★
            float bleedFlash = (Mathf.Sin(Time.unscaledTime * 10f) + 1f) / 2f; // 0~1 正弦波

            int count = Mathf.Min(_limbCount, body.limbs.Length);
            for (int i = 0; i < count; i++)
            {
                var limb = body.limbs[i];
                if (limb == null) continue;

                var pair = _limbIcons[i];
                Color color;

                // 优先级 1：截肢
                if (limb.dismembered)
                {
                    color = ColorDismembered;
                }
                // 优先级 2：骨折 / 脱臼
                else if (limb.broken || limb.dislocated)
                {
                    color = ColorBroken;
                }
                // 优先级 3：血量渐变
                else
                {
                    float healthPct = Mathf.Min(limb.skinHealth, limb.muscleHealth) / 100f;

                    if (healthPct > 0.8f)
                        color = ColorHealthy;
                    else if (healthPct > 0.4f)
                        color = ColorWarn;
                    else
                        color = ColorDanger;
                }

                // ★ 独立状态：流血 Alpha 闪烁 ★
                if (limb.bleedAmount > 0.05f)
                {
                    color.a = Mathf.Lerp(0.3f, 1f, bleedFlash);
                }
                else
                {
                    color.a = 1f; // 不流血时强制不透明，不受原版面板关闭影响
                }

                _limbIcons[i].iconImage.color = color;
            }
        }

        // ============================================================
        // 初始化 HUD
        // ============================================================

        private void InitializeHUD()
        {
            if (PlayerCamera.main == null) return;

            var woundViewGo = PlayerCamera.main.woundView;
            if (woundViewGo == null) return;

            var sourceWoundView = woundViewGo.GetComponent<WoundView>();
            if (sourceWoundView == null || sourceWoundView.limbImages == null) return;

            _sourceLimbImages = sourceWoundView.limbImages;
            _limbCount = _sourceLimbImages.Length;
            if (_limbCount == 0) return;

            // ---- 1. 创建 Canvas ----
            _hudCanvasObj = new GameObject("LimbStatusHUD_Canvas");
            Object.DontDestroyOnLoad(_hudCanvasObj);
            _hudCanvasObj.hideFlags = HideFlags.DontSave;

            _hudCanvas = _hudCanvasObj.AddComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 50;

            var scaler = _hudCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = _hudCanvasObj.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // ---- 2. 创建 HUDContainer（锚点钉死在右下角） ----
            var containerObj = new GameObject("LimbHUD_Container", typeof(RectTransform));
            containerObj.transform.SetParent(_hudCanvasObj.transform, worldPositionStays: false);
            _containerRt = containerObj.GetComponent<RectTransform>();
            _containerRt.anchorMin = new Vector2(1f, 0f);
            _containerRt.anchorMax = new Vector2(1f, 0f);
            _containerRt.pivot     = new Vector2(1f, 0f);
            _containerRt.anchoredPosition = GlobalSettings.LimbHUDPosition;
            _containerRt.localScale = Vector3.one * GlobalSettings.LimbHUDScale;
            _containerRt.sizeDelta = Vector2.zero;

            // ---- 3. 遍历原版 limbImages，完美复制坐标/旋转/缩放 ----
            _limbIcons = new LimbIconPair[_limbCount];

            for (int i = 0; i < _limbCount; i++)
            {
                if (_sourceLimbImages[i] == null) continue;

                var srcRt = _sourceLimbImages[i].rectTransform;

                var iconObj = new GameObject($"LimbIcon_{i}", typeof(RectTransform));
                iconObj.transform.SetParent(_containerRt, worldPositionStays: false);

                var iconRt = iconObj.GetComponent<RectTransform>();

                // ★ 完美复刻原版坐标布局 ★
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.localPosition    = srcRt.localPosition;
                iconRt.sizeDelta        = srcRt.sizeDelta;
                iconRt.pivot            = new Vector2(0.5f, 0.5f);
                iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
                iconRt.localScale       = srcRt.localScale;    // 镜像翻转（左右肢体）
                iconRt.localRotation    = srcRt.localRotation;

                var iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = _sourceLimbImages[i].sprite;
                iconImage.color  = Color.white; // 初始白色，OnUpdate 会覆盖
                iconImage.raycastTarget = false;

                _limbIcons[i] = new LimbIconPair
                {
                    iconImage = iconImage,
                    iconRt    = iconRt,
                };
            }
        }
    }
}
