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
    }
}
