using UnityEngine;

namespace FullBrightMod.Core
{
    /// <summary>
    /// 全局 QoL 设置 —— 独立于模块开关的功能配置。
    /// 这些设置在 ClickGUI 的 "Global Settings" Tab 中统一控制。
    /// </summary>
    public static class GlobalSettings
    {
        // ======== Grid Snapping（网格吸附） ========
        /// <summary>是否启用面板拖拽网格吸附</summary>
        public static bool EnableGridSnap = false;
        /// <summary>网格大小（像素）</summary>
        public static float GridSize = 20f;

        // ======== Minimap（小地图） ========
        /// <summary>是否启用小地图</summary>
        public static bool EnableMinimap = false;
        /// <summary>小地图在屏幕上的矩形区域（像素坐标系）</summary>
        public static Rect MinimapRect = new Rect(Screen.width - 230f, Screen.height - 230f, 210f, 210f);
        /// <summary>小地图显示的世界半径（单位：米）</summary>
        public static float MinimapRadius = 30f;

        // ======== Limb HUD（肢体状态常驻显示） ========
        /// <summary>是否启用肢体状态 HUD</summary>
        public static bool EnableLimbHUD = false;
        /// <summary>
        /// 肢体 HUD 的偏移位置（相对于屏幕右下角，像素）。
        /// 在右下角锚定(1,0)-(1,0)坐标系中，X负=向左，Y正=向上。
        /// </summary>
        public static Vector2 LimbHUDPosition = new Vector2(-200f, 200f);
        /// <summary>肢体 HUD 缩放比例</summary>
        public static float LimbHUDScale = 0.45f;

        // ======== Logo 水印 ========
        /// <summary>是否在屏幕左上角显示 Logo</summary>
        public static bool EnableLogoOverlay = false;
        /// <summary>Logo 渲染高度（像素，宽度自适应）</summary>
        public static float LogoOverlayHeight = 300f;
        public static int LogoIndex = 0;

        // ======== Mod GUID 隐藏 ========
        public static bool HideCheaterModGUID = true;

        // ======== CM使用者识别 ========
        /// <summary>是否启用CM使用者识别（通过 Steam Lobby MemberData 检测）</summary>
        public static bool EnableModRadar = false;

        // ======== Feature List（功能列表 / ArrayList） ========
        /// <summary>是否在屏幕上显示已开启的功能列表</summary>
        public static bool EnableFeatureList = true;
        /// <summary>功能列表在屏幕上的矩形区域（像素坐标系）</summary>
        public static Rect FeatureListRect = new Rect(20f, 20f, 200f, 200f);
        /// <summary>左对齐开关：true=左对齐, false=右对齐（默认右对齐）</summary>
        public static bool FeatureListAlignLeft = false;
        /// <summary>字体大小</summary>
        public static int FeatureListFontSize = 15;
        /// <summary>文本颜色（非彩虹模式）</summary>
        public static Color FeatureListColor = Color.white;
        /// <summary>彩虹渐变模式</summary>
        public static bool FeatureListRainbow = true;
        /// <summary>是否绘制文字背景框</summary>
        public static bool FeatureListBackground = true;
        /// <summary>是否在边缘绘制强调线</summary>
        public static bool FeatureListAccentLine = true;

        // ======== UI 音效 ========
        /// <summary>是否播放模块开关提示音</summary>
        public static bool EnableToggleSound = true;

        // ======== Toast 通知 ========
        /// <summary>是否弹出模块开关通知</summary>
        public static bool EnableNotifications = true;
        /// <summary>通知在屏幕上的 Y 轴起始位置</summary>
        public static float NotificationYPosition = 60f;
        /// <summary>通知滑入方向：true=从右侧滑入, false=从左侧滑入</summary>
        public static bool NotificationSlideFromRight = false;
    }
}
