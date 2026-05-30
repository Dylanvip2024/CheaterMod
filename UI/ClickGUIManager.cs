using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.Modules;
using UnityEngine;

namespace FullBrightMod.UI
{
    /// <summary>
    /// ClickGUI 管理器 —— 纯代码手绘的模块化 GUI 系统。
    /// 不使用 GUI.Window / GUILayout / GUI.Button 等内置组件。
    ///
    /// 交互逻辑：
    ///   - 鼠标左键拖拽标题栏 → 移动面板（若启用 GridSnap 则吸附到网格）
    ///   - 鼠标右键点击标题栏 → 折叠/展开整个分类面板
    ///   - 鼠标左键点击模块行   → 切换模块启用/禁用
    ///   - 鼠标右键点击模块行   → 展开/收起模块设置子菜单
    ///   - 展开后第一行          → Keybind 绑定（点击进入监听，按键确认）
    ///   - 展开后后续行          → 模块自定义设置（如 Slider）
    ///
    /// 新增功能：
    ///   1. 顶部 Tabs：Modules（功能界面）vs Global Settings（全局设置）
    ///   2. 面板拖拽网格吸附（Grid Snapping）
    ///   3. Global Settings 面板：控制小地图、肢体 HUD、网格吸附等
    /// </summary>
    public class ClickGUIManager
    {
        // ---- 核心引用 ----
        private readonly ModuleManager _moduleManager;
        private readonly List<ClickGUIPanel> _panels;

        /// <summary>暴露面板列表，供 ConfigManager 持久化 UI 布局</summary>
        public List<ClickGUIPanel> Panels => _panels;

        // ---- Tab 系统 ----
        private enum ActiveTab { Modules, GlobalSettings }
        private ActiveTab _activeTab = ActiveTab.Modules;

        // ---- 渲染材质 ----
        private static Texture2D _whiteTexture;
        internal static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                    _whiteTexture.hideFlags = HideFlags.HideAndDontSave;
                }
                return _whiteTexture;
            }
        }

        // ---- 配色方案 ----
        private static readonly Color ColorAccent          = new Color(0.2f, 0.6f, 1.0f, 1.0f);
        private static readonly Color ColorTitleBg         = new Color(0.15f, 0.15f, 0.15f, 1.0f);
        private static readonly Color ColorModuleBg        = new Color(0.16f, 0.16f, 0.18f, 0.90f);
        private static readonly Color ColorModuleBgHover   = new Color(0.22f, 0.22f, 0.25f, 0.90f);
        private static readonly Color ColorModuleBgEnabled = new Color(0.2f, 0.6f, 1.0f, 0.85f);
        private static readonly Color ColorDisabledText    = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        private static readonly Color ColorSubBg           = new Color(0.325f, 0.325f, 0.34f, 0.95f);
        private static readonly Color ColorSubBgHover      = new Color(0.18f, 0.18f, 0.22f, 0.90f);
        private static readonly Color ColorSliderFill      = new Color(0.2f, 0.6f, 1.0f, 0.7f);
        private static readonly Color ColorTabActive       = new Color(0.2f, 0.6f, 1.0f, 0.85f);
        private static readonly Color ColorTabInactive     = new Color(0.16f, 0.16f, 0.18f, 0.90f);
        private static readonly Color ColorTabHover        = new Color(0.22f, 0.22f, 0.25f, 0.90f);

        // ---- Tab 栏尺寸 ----
        private const float TabHeight = 30f;
        private const float TabWidth  = 160f;
        private const float TabBarY   = 28f;

        // ---- 事件状态 ----
        private bool _menuVisible;

        public ClickGUIManager(ModuleManager moduleManager)
        {
            _moduleManager = moduleManager;
            _panels = new List<ClickGUIPanel>();
            int index = 0;
            foreach (ModuleCategory cat in System.Enum.GetValues(typeof(ModuleCategory)))
            {
                _panels.Add(new ClickGUIPanel(cat, index));
                index++;
            }
        }

        public void Toggle() => _menuVisible = !_menuVisible;
        public bool Visible => _menuVisible;

        // =========================================================
        // 生命周期
        // =========================================================
        public void OnGUI()
        {
            if (!_menuVisible) return;
            Event e = Event.current;
            if (e == null) return;

            GUI.color = Color.white;

            // ==== 按键绑定监听 ====
            if (_activeTab == ActiveTab.Modules && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                foreach (var panel in _panels)
                {
                    var mods = GetModulesForCategory(panel.Category);
                    foreach (var mod in mods)
                    {
                        if (mod.IsBinding)
                        {
                            if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Backspace)
                                mod.BindKey = KeyCode.None;
                            else
                                mod.BindKey = e.keyCode;
                            mod.IsBinding = false;
                            e.Use();
                            break;
                        }
                    }
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                for (int i = 0; i < _panels.Count; i++)
                    _panels[i].IsDragging = false;
            }

            DrawTabBar(e);

            if (_activeTab == ActiveTab.Modules)
            {
                int draggingIdx = -1;
                for (int i = 0; i < _panels.Count; i++)
                    if (_panels[i].IsDragging) { draggingIdx = i; break; }

                for (int i = 0; i < _panels.Count; i++)
                    if (i != draggingIdx) DrawPanel(i, e);

                if (draggingIdx >= 0)
                    DrawPanel(draggingIdx, e);
            }
            else
            {
                DrawGlobalSettingsPanel(e);
            }

            DrawAuthorCredit();

            GUI.color = Color.white;
        }

        // =========================================================
        // Tab 栏绘制
        // =========================================================
        private void DrawTabBar(Event e)
        {
            float barWidth = TabWidth * 2 + 4f;
            float barX = Screen.width * 0.5f - barWidth * 0.5f;

            Rect modulesTabRect = new Rect(barX, TabBarY, TabWidth, TabHeight);
            bool modulesHover = modulesTabRect.Contains(e.mousePosition);
            GUI.color = _activeTab == ActiveTab.Modules ? ColorTabActive : (modulesHover ? ColorTabHover : ColorTabInactive);
            GUI.DrawTexture(modulesTabRect, WhiteTexture);
            GUI.color = Color.white;
            GUIStyle tabStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(modulesTabRect, Utils.I18n.Get("tab_modules"), tabStyle);

            Rect settingsTabRect = new Rect(barX + TabWidth + 4f, TabBarY, TabWidth, TabHeight);
            bool settingsHover = settingsTabRect.Contains(e.mousePosition);
            GUI.color = _activeTab == ActiveTab.GlobalSettings ? ColorTabActive : (settingsHover ? ColorTabHover : ColorTabInactive);
            GUI.DrawTexture(settingsTabRect, WhiteTexture);
            GUI.color = Color.white;
            GUI.Label(settingsTabRect, Utils.I18n.Get("tab_globalsettings"), tabStyle);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (modulesTabRect.Contains(e.mousePosition))
                {
                    _activeTab = ActiveTab.Modules;
                    e.Use();
                }
                else if (settingsTabRect.Contains(e.mousePosition))
                {
                    _activeTab = ActiveTab.GlobalSettings;
                    e.Use();
                }
            }
        }

        // =========================================================
        // 面板绘制（Modules Tab）
        // =========================================================
        private void DrawPanel(int panelIndex, Event e)
        {
            ClickGUIPanel panel = _panels[panelIndex];
            List<ModuleBase> modules = GetModulesForCategory(panel.Category);

            float totalHeight = ClickGUIPanel.TitleHeight;
            if (panel.IsExpanded)
            {
                foreach (var mod in modules)
                {
                    totalHeight += ClickGUIPanel.ModuleRowHeight;
                    if (mod.IsExpanded)
                    {
                        totalHeight += ClickGUIPanel.ModuleRowHeight;
                        totalHeight += mod.GetSettingsHeight();
                    }
                }
            }

            Rect panelRect = new Rect(panel.Position.x, panel.Position.y, panel.Position.width, totalHeight);

            Rect titleRect = new Rect(panelRect.x, panelRect.y, panelRect.width, ClickGUIPanel.TitleHeight);

            Rect accentRect = new Rect(titleRect.x, titleRect.y, titleRect.width, ClickGUIPanel.AccentLineThickness);
            GUI.color = ColorAccent;
            GUI.DrawTexture(accentRect, WhiteTexture);

            Rect titleBgRect = new Rect(titleRect.x, titleRect.y + ClickGUIPanel.AccentLineThickness,
                titleRect.width, titleRect.height - ClickGUIPanel.AccentLineThickness);
            GUI.color = ColorTitleBg;
            GUI.DrawTexture(titleBgRect, WhiteTexture);

            GUI.color = Color.white;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(titleRect, GetCategoryDisplayName(panel.Category), titleStyle);

            if (panel.IsExpanded)
            {
                float currentY = panelRect.y + ClickGUIPanel.TitleHeight;

                for (int m = 0; m < modules.Count; m++)
                {
                    ModuleBase module = modules[m];

                    Rect rowRect = new Rect(panelRect.x, currentY, panelRect.width, ClickGUIPanel.ModuleRowHeight);

                    bool isHovering = rowRect.Contains(e.mousePosition);
                    GUI.color = module.Enabled ? ColorModuleBgEnabled
                        : (isHovering ? ColorModuleBgHover : ColorModuleBg);
                    GUI.DrawTexture(rowRect, WhiteTexture);

                    GUI.color = Color.white;

                    Color textColor = module.Enabled ? Color.white : ColorDisabledText;
                    GUIStyle rowStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fontSize = 14,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = textColor },
                        padding = new RectOffset(10, 4, 0, 0)
                    };
                    GUI.Label(rowRect, module.Name, rowStyle);

                    currentY += ClickGUIPanel.ModuleRowHeight;

                    if (module.IsExpanded)
                    {
                        float subStartY = currentY;
                        float subTotalHeight = ClickGUIPanel.ModuleRowHeight + module.GetSettingsHeight();

                        Rect subBgRect = new Rect(panelRect.x, subStartY, panelRect.width, subTotalHeight);
                        GUI.color = ColorSubBg;
                        GUI.DrawTexture(subBgRect, WhiteTexture);

                        Rect bindRect = new Rect(panelRect.x, currentY, panelRect.width, ClickGUIPanel.ModuleRowHeight);
                        bool bindHover = bindRect.Contains(e.mousePosition);
                        if (bindHover) { GUI.color = ColorSubBgHover; GUI.DrawTexture(bindRect, WhiteTexture); }

                        GUI.color = Color.white;

                        string bindText;
                        if (module.IsBinding)
                            bindText = "  Bind: [ ... ]";
                        else if (module.BindKey != KeyCode.None)
                            bindText = "  Bind: [ " + module.BindKey.ToString() + " ]";
                        else
                            bindText = "  Bind: [ None ]";

                        GUIStyle bindStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            fontSize = 11,
                            normal = { textColor = Color.white },
                            padding = new RectOffset(16, 4, 0, 0)
                        };
                        GUI.Label(bindRect, bindText, bindStyle);
                        currentY += ClickGUIPanel.ModuleRowHeight;

                        if (e.type == EventType.MouseDown && e.button == 0 && bindRect.Contains(e.mousePosition))
                        {
                            module.IsBinding = !module.IsBinding;
                            e.Use();
                        }

                        GUI.color = Color.white;

                        float settingsX = panelRect.x;
                        module.DrawSettings(settingsX, ref currentY, panelRect.width, e);

                        Rect subSep = new Rect(panelRect.x, subStartY + subTotalHeight - 1, panelRect.width, 1);
                        GUI.color = new Color(0.05f, 0.05f, 0.07f, 0.6f);
                        GUI.DrawTexture(subSep, WhiteTexture);
                    }

                    if (!module.IsExpanded && m < modules.Count - 1)
                    {
                        Rect sepRect = new Rect(panelRect.x, currentY - 1, panelRect.width, 1);
                        GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.5f);
                        GUI.DrawTexture(sepRect, WhiteTexture);
                    }
                }
            }

            if (e.isMouse)
            {
                if (panel.IsDragging && e.type == EventType.MouseDrag && e.button == 0)
                {
                    float newX = e.mousePosition.x - panel.DragOffset.x;
                    float newY = e.mousePosition.y - panel.DragOffset.y;

                    if (GlobalSettings.EnableGridSnap && GlobalSettings.GridSize > 1f)
                    {
                        float gs = GlobalSettings.GridSize;
                        newX = Mathf.Round(newX / gs) * gs;
                        newY = Mathf.Round(newY / gs) * gs;
                    }

                    panel.Position.x = newX;
                    panel.Position.y = newY;
                    e.Use();
                }

                if (e.type == EventType.MouseDown)
                {
                    if (e.button == 1 && titleRect.Contains(e.mousePosition))
                    {
                        panel.IsExpanded = !panel.IsExpanded;
                        e.Use(); return;
                    }
                    if (e.button == 0 && titleRect.Contains(e.mousePosition))
                    {
                        panel.IsDragging = true;
                        panel.DragOffset = e.mousePosition - new Vector2(panel.Position.x, panel.Position.y);
                        e.Use(); return;
                    }

                    if (panel.IsExpanded)
                    {
                        float scanY = panelRect.y + ClickGUIPanel.TitleHeight;
                        for (int m = 0; m < modules.Count; m++)
                        {
                            ModuleBase module = modules[m];
                            Rect rowRect = new Rect(panelRect.x, scanY, panelRect.width, ClickGUIPanel.ModuleRowHeight);

                            if (rowRect.Contains(e.mousePosition))
                            {
                                if (e.button == 1)
                                {
                                    module.IsExpanded = !module.IsExpanded;
                                    e.Use(); return;
                                }
                                if (e.button == 0)
                                {
                                    module.Enabled = !module.Enabled;
                                    if (module.Enabled) module.OnEnable();
                                    else module.OnDisable();
                                    e.Use(); return;
                                }
                            }
                            scanY += ClickGUIPanel.ModuleRowHeight;
                            if (module.IsExpanded)
                                scanY += ClickGUIPanel.ModuleRowHeight + module.GetSettingsHeight();
                        }
                    }
                }
            }

            GUI.color = Color.white;
        }

        // =========================================================
        // ★ 辅助方法：同步 GlobalSettings 开关到 ModuleBase 生命周期 ★
        // =========================================================
        private void SyncModuleLifecycle<T>(bool enabled) where T : ModuleBase
        {
            var mod = _moduleManager.GetModule<T>();
            if (mod == null) return;
            if (mod.Enabled == enabled) return; // 无变化跳过
            mod.Enabled = enabled;
            if (enabled) mod.OnEnable();
            else         mod.OnDisable();
        }

        // =========================================================
        // Global Settings Tab —— 一次绘制，无冗余重绘
        // =========================================================
        private void DrawGlobalSettingsPanel(Event e)
        {
            float panelX = Screen.width * 0.5f - 200f;
            float panelY = TabBarY + TabHeight + 10f;
            float panelW = 400f;
            float rowH = 26f;

            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.6f, 1.0f, 1f) },
                padding = new RectOffset(10, 0, 0, 0)
            };

            // ---- 先计算总高度（用于绘制背景） ----
            float contentHeight = EstimateContentHeight(rowH);
            float totalPanelHeight = contentHeight + 20f; // 10px padding top + 10px padding bottom

            // ---- 绘制背景和标题线（先画，仅一次） ----
            Rect bgRect = new Rect(panelX, panelY, panelW, totalPanelHeight);
            GUI.color = new Color(0.16f, 0.16f, 0.18f, 0.95f);
            GUI.DrawTexture(bgRect, WhiteTexture);

            Rect accentRect = new Rect(panelX, panelY, panelW, 2f);
            GUI.color = ColorAccent;
            GUI.DrawTexture(accentRect, WhiteTexture);
            GUI.color = Color.white;

            // ---- 按顺序依次绘制所有行（仅一次） ----
            float currentY = panelY + 10f;
            bool prevGrid   = GlobalSettings.EnableGridSnap;
            bool prevMinimap = GlobalSettings.EnableMinimap;
            bool prevLimb   = GlobalSettings.EnableLimbHUD;

            // ======== Grid Snapping ========
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_grid"), sectionStyle);
            currentY += rowH + 2f;

            DrawToggleRow(panelX, ref currentY, panelW, rowH, e,
                Utils.I18n.Get("gs_enable_grid"), ref GlobalSettings.EnableGridSnap);

            if (GlobalSettings.EnableGridSnap)
            {
                float val = GlobalSettings.GridSize;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_grid_size") + $": {val:F0}" + Utils.I18n.Get("unit_px"), ref val, 4f, 80f);
                GlobalSettings.GridSize = val;
            }

            currentY += 6f;

            // ======== Minimap ========
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_minimap"), sectionStyle);
            currentY += rowH + 2f;

            DrawToggleRow(panelX, ref currentY, panelW, rowH, e,
                Utils.I18n.Get("gs_enable_minimap"), ref GlobalSettings.EnableMinimap);

            if (GlobalSettings.EnableMinimap)
            {
                float size = GlobalSettings.MinimapRect.width;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_minimap_size") + $": {size:F0}" + Utils.I18n.Get("unit_px"), ref size, 80f, 400f);
                // ★ Bug2 修复：只改宽高，不修改 xy ★
                Rect r = GlobalSettings.MinimapRect;
                GlobalSettings.MinimapRect = new Rect(r.x, r.y, size, size);

                float radius = GlobalSettings.MinimapRadius;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_minimap_radius") + $": {radius:F0}" + Utils.I18n.Get("unit_m"), ref radius, 10f, 100f);
                GlobalSettings.MinimapRadius = radius;
            }

            currentY += 6f;

            // ======== Limb HUD ========
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_limb"), sectionStyle);
            currentY += rowH + 2f;

            DrawToggleRow(panelX, ref currentY, panelW, rowH, e,
                Utils.I18n.Get("gs_enable_limb"), ref GlobalSettings.EnableLimbHUD);

            if (GlobalSettings.EnableLimbHUD)
            {
                float scale = GlobalSettings.LimbHUDScale;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_limb_scale") + $": {scale:F2}" + Utils.I18n.Get("unit_x"), ref scale, 0.3f, 1.5f);
                GlobalSettings.LimbHUDScale = scale;

                float posX = GlobalSettings.LimbHUDPosition.x;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_limb_pos_x") + $": {posX:F0}" + Utils.I18n.Get("unit_px"), ref posX, -2000f, 0f);
                float posY = GlobalSettings.LimbHUDPosition.y;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_limb_pos_y") + $": {posY:F0}" + Utils.I18n.Get("unit_px"), ref posY, 0f, 1500f);

                GlobalSettings.LimbHUDPosition = new Vector2(posX, posY);
            }

            currentY += 6f;

            // ======== Logo Overlay ========
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_logo"), sectionStyle);
            currentY += rowH + 2f;

            DrawToggleRow(panelX, ref currentY, panelW, rowH, e,
                Utils.I18n.Get("gs_enable_logo"), ref GlobalSettings.EnableLogoOverlay);

            if (GlobalSettings.EnableLogoOverlay)
            {
                float h = GlobalSettings.LogoOverlayHeight;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_logo_height") + $": {h:F0}" + Utils.I18n.Get("unit_px"), ref h, 200f, 500f);
                GlobalSettings.LogoOverlayHeight = h;
            }

            // ★ Bug1 修复：同步 ModuleBase 生命周期 ★
            if (prevGrid != GlobalSettings.EnableGridSnap) { /* GridSnap 无对应 Module */ }
            if (prevMinimap != GlobalSettings.EnableMinimap)
                SyncModuleLifecycle<MinimapModule>(GlobalSettings.EnableMinimap);
            if (prevLimb != GlobalSettings.EnableLimbHUD)
                SyncModuleLifecycle<LimbStatusModule>(GlobalSettings.EnableLimbHUD);
        }

        /// <summary>预先估算 Global Settings 面板的内容高度（不含背景 padding）</summary>
        private float EstimateContentHeight(float rowH)
        {
            float h = 0f;
            // Grid Snapping section
            h += rowH + 2f; // section title
            h += rowH;      // toggle
            if (GlobalSettings.EnableGridSnap) h += rowH; // slider
            h += 6f;
            // Minimap section
            h += rowH + 2f;
            h += rowH;
            if (GlobalSettings.EnableMinimap) h += rowH + rowH; // size slider + radius slider
            h += 6f;
            // Limb HUD section
            h += rowH + 2f;
            h += rowH;
            if (GlobalSettings.EnableLimbHUD) h += rowH + rowH + rowH; // scale + posX + posY
            // Logo Overlay section
            h += rowH + 2f;
            h += rowH;
            if (GlobalSettings.EnableLogoOverlay) h += rowH; // height slider
            return h;
        }

        // =========================================================
        // 辅助绘制方法
        // =========================================================
        private void DrawToggleRow(float panelX, ref float y, float width, float rowHeight, Event e,
            string label, ref bool value)
        {
            Rect rowRect = new Rect(panelX, y, width, rowHeight);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? ColorModuleBgEnabled : (hover ? ColorModuleBgHover : ColorModuleBg);
            GUI.DrawTexture(rowRect, WhiteTexture);
            GUI.color = Color.white;

            string display = (value ? "[ON]  " : "[OFF] ") + label;
            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = value ? Color.white : ColorDisabledText },
                padding = new RectOffset(14, 0, 4, 0)
            };
            GUI.Label(rowRect, display, s);

            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                value = !value;
                e.Use();
            }

            y += rowHeight;
        }

        private void DrawSliderRow(float panelX, ref float y, float width, float rowHeight, Event e,
            string label, ref float value, float min, float max)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white },
                padding = new RectOffset(14, 0, 4, 0)
            };
            GUI.Label(new Rect(panelX, y, width, rowHeight * 0.5f), label, labelStyle);

            float sliderY = y + rowHeight * 0.4f;
            float sliderH = rowHeight * 0.5f;
            Rect sliderRect = new Rect(panelX + 14f, sliderY, width - 28f, sliderH);

            float newVal = DrawSlider(sliderRect, value, min, max, e, ColorSliderFill, 8f);
            if (Mathf.Abs(newVal - value) > 0.001f) value = newVal;

            y += rowHeight;
        }

        public static float DrawSlider(Rect sliderRect, float currentValue, float min, float max,
            Event e, Color? fillColor = null, float handleWidth = 8f)
        {
            float slotHeight = 6f;
            Rect slotRect = new Rect(sliderRect.x, sliderRect.y + (sliderRect.height - slotHeight) / 2,
                sliderRect.width, slotHeight);
            GUI.color = new Color(0.25f, 0.25f, 0.28f, 1f);
            GUI.DrawTexture(slotRect, WhiteTexture);

            float t = Mathf.InverseLerp(min, max, currentValue);
            Rect fillRect = new Rect(slotRect.x, slotRect.y, slotRect.width * t, slotHeight);
            GUI.color = fillColor ?? ColorSliderFill;
            GUI.DrawTexture(fillRect, WhiteTexture);

            float handleX = slotRect.x + slotRect.width * t - handleWidth / 2;
            Rect handleRect = new Rect(handleX, sliderRect.y, handleWidth, sliderRect.height);
            GUI.color = Color.white;
            GUI.DrawTexture(handleRect, WhiteTexture);

            if (e.type == EventType.MouseDrag && e.button == 0 && sliderRect.Contains(e.mousePosition))
            {
                float mouseT = (e.mousePosition.x - slotRect.x) / slotRect.width;
                currentValue = Mathf.Lerp(min, max, Mathf.Clamp01(mouseT));
                e.Use();
            }
            if (e.type == EventType.MouseDown && e.button == 0 && sliderRect.Contains(e.mousePosition))
            {
                float mouseT = (e.mousePosition.x - slotRect.x) / slotRect.width;
                currentValue = Mathf.Lerp(min, max, Mathf.Clamp01(mouseT));
                e.Use();
            }

            return currentValue;
        }

        // =========================================================
        // 辅助方法
        // =========================================================
        private List<ModuleBase> GetModulesForCategory(ModuleCategory category)
        {
            var allModules = _moduleManager.GetAllModules();
            List<ModuleBase> result = new List<ModuleBase>();
            foreach (var m in allModules)
                if (m.Category == category) result.Add(m);
            return result;
        }

        private static string GetCategoryDisplayName(ModuleCategory cat)
        {
            switch (cat)
            {
                case ModuleCategory.Combat:   return Utils.I18n.Get("cat_combat");
                case ModuleCategory.Player:   return Utils.I18n.Get("cat_player");
                case ModuleCategory.Movement: return Utils.I18n.Get("cat_movement");
                case ModuleCategory.Render:   return Utils.I18n.Get("cat_render");
                case ModuleCategory.World:    return Utils.I18n.Get("cat_world");
                case ModuleCategory.Misc:     return Utils.I18n.Get("cat_misc");
                default:                      return cat.ToString();
            }
        }

        // =========================================================
        // 作者署名 — 屏幕右下角，低调半透明，始终可见
        // =========================================================
        private static void DrawAuthorCredit()
        {
            float authorW = 190f, authorH = 22f;
            // 紧贴屏幕右下角，留 6px 边距
            Rect authorRect = new Rect(Screen.width - authorW - 6f, Screen.height - authorH - 6f, authorW, authorH);

            GUIStyle authorStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.2f, 0.6f, 1.0f, 0.7f) }
            };
            GUI.Label(authorRect, "作者: BilBil Dylanvip2024", authorStyle);
        }
    }
}
