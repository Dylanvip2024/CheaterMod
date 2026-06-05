using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.UI
{
    /// <summary>
    /// F6 菜单 GUI —— 负责绘制菜单窗口，读取和修改 Settings 中的变量。
    /// 此模块不直接操作任何功能逻辑，仅作为 UI 入口。
    /// </summary>
    public class MenuGUI : ModuleBase
    {
        public override string Name => "MenuGUI";

        private bool _showMenu;
        private Rect _windowRect = new Rect(100, 100, 320, 800);
        private bool _prevFreecam;

        public override void OnEnable()
        {
            _showMenu = false;
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                _showMenu = !_showMenu;
                if (_showMenu)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (_showMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public override void OnGUI()
        {
            if (!_showMenu) return;
            _windowRect = GUILayout.Window(0, _windowRect, DrawMenuWindow, "未知伤亡 辅助mod");
        }

        private void DrawMenuWindow(int windowID)
        {
            GUILayout.Space(5);
            GUILayout.Label("====== 视觉&辅助功能区 ======");
            ModSettings.IsCameraZoomEnabled    = GUILayout.Toggle(ModSettings.IsCameraZoomEnabled,    " 开启摄像机视距拉远 (扩大你的视野)");
            ModSettings.IsVisionExpandEnabled  = GUILayout.Toggle(ModSettings.IsVisionExpandEnabled,  " 开启局部光照范围扩大");
            ModSettings.IsFullBrightEnabled    = GUILayout.Toggle(ModSettings.IsFullBrightEnabled,    " 开启全亮模式");
            ModSettings.IsItemEspEnabled       = GUILayout.Toggle(ModSettings.IsItemEspEnabled,       " 开启地面物品透视");
            ModSettings.IsCreatureEspEnabled   = GUILayout.Toggle(ModSettings.IsCreatureEspEnabled,   " 开启生物/怪物透视");
            ModSettings.IsTrapEspEnabled       = GUILayout.Toggle(ModSettings.IsTrapEspEnabled,       " 开启陷阱警告雷达 (显示出陷阱与危险范围)");
            ModSettings.IsThroughWallEnabled   = GUILayout.Toggle(ModSettings.IsThroughWallEnabled,   " 开启隔墙取物 (无视墙壁遮挡)");
            ModSettings.IsLongHandsEnabled     = GUILayout.Toggle(ModSettings.IsLongHandsEnabled,     " 开启长手模式 (超远交互/隔墙互动)");
            ModSettings.IsAutoTranslateEnabled = GUILayout.Toggle(ModSettings.IsAutoTranslateEnabled, " 开启聊天机翻 (自动谷歌翻译聊天外语)");
            ModSettings.IsIQ250Enabled         = GUILayout.Toggle(ModSettings.IsIQ250Enabled,         " 开启万事通模式 (无智商限制看物品介绍)");

            _prevFreecam = ModSettings.IsFreecamEnabled;
            ModSettings.IsFreecamEnabled = GUILayout.Toggle(ModSettings.IsFreecamEnabled, " 开启灵魂出窍 (方向键控制视角, Shift加速)");
            if (ModSettings.IsFreecamEnabled && !_prevFreecam && Camera.main != null)
            {
                ModSettings.FreecamPosition = Camera.main.transform.position;
            }

            GUILayout.Space(15);
            GUILayout.Label("====== 降维打击专区 ======");
            ModSettings.IsFlightEnabled    = GUILayout.Toggle(ModSettings.IsFlightEnabled,    " 开启超级飞侠 (WASD控制, Shift加速)");
            ModSettings.IsAutoUnlockEnabled= GUILayout.Toggle(ModSettings.IsAutoUnlockEnabled," 开启秒开锁 (自动解锁)");

            GUILayout.Space(10);
            if (ModSettings.IsCameraZoomEnabled)
            {
                GUILayout.Label("大地图视距尺寸 (Camera Size): " + ModSettings.CustomCameraSize.ToString("F1"));
                ModSettings.CustomCameraSize = GUILayout.HorizontalSlider(ModSettings.CustomCameraSize, 5.0f, 30.0f);
            }
            if (ModSettings.IsVisionExpandEnabled && !ModSettings.IsFullBrightEnabled)
            {
                GUILayout.Label("局部光晕半径 (Radius): " + ModSettings.CustomVisionRadius.ToString("F1"));
                ModSettings.CustomVisionRadius = GUILayout.HorizontalSlider(ModSettings.CustomVisionRadius, 5.0f, 50.0f);
            }
            if (ModSettings.IsFullBrightEnabled)
            {
                GUILayout.Label("全局光照强度 (Intensity): " + ModSettings.BrightenIntensity.ToString("F1"));
                ModSettings.BrightenIntensity = GUILayout.HorizontalSlider(ModSettings.BrightenIntensity, 0.0f, 5.0f);
            }
            if (ModSettings.IsLongHandsEnabled)
            {
                GUILayout.Label("长手物理交互半径: " + ModSettings.CustomPickupRange.ToString("F1") + "m");
                ModSettings.CustomPickupRange = GUILayout.HorizontalSlider(ModSettings.CustomPickupRange, 9.0f, 150f);
            }

            GUILayout.Space(5);
            GUILayout.Label("ESP 字体大小: " + ModSettings.EspFontSize.ToString());
            ModSettings.EspFontSize = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)ModSettings.EspFontSize, 10f, 24f));

            GUILayout.Space(5);
            GUILayout.Label("物品 ESP 字体颜色样式:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("绿色")) ModSettings.SelectedEspColor = Color.green;
            if (GUILayout.Button("红色")) ModSettings.SelectedEspColor = Color.red;
            if (GUILayout.Button("黄色")) ModSettings.SelectedEspColor = Color.yellow;
            if (GUILayout.Button("青色")) ModSettings.SelectedEspColor = Color.cyan;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label("生物 ESP 字体颜色样式:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("青色")) ModSettings.SelectedCreatureColor = Color.cyan;
            if (GUILayout.Button("绿色")) ModSettings.SelectedCreatureColor = Color.green;
            if (GUILayout.Button("红色")) ModSettings.SelectedCreatureColor = Color.red;
            if (GUILayout.Button("黄色")) ModSettings.SelectedCreatureColor = Color.yellow;
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.Label("快捷键: [F6] 隐藏/显示此菜单", GUI.skin.box);
            GUI.DragWindow();
        }
    }
}
