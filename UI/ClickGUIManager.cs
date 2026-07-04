using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.Modules;
using UnityEngine;

namespace FullBrightMod.UI
{
    public class ClickGUIManager
    {
        private readonly ModuleManager _moduleManager;
        private readonly List<ClickGUIPanel> _panels;
        public List<ClickGUIPanel> Panels => _panels;

        private enum ActiveTab { Modules, GlobalSettings, Credits }
        private ActiveTab _activeTab = ActiveTab.Modules;

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

        private const float TabHeight = 30f;
        private const float TabWidth  = 160f;
        private const float TabBarY   = 28f;

        private bool _menuVisible;

        // ── Tab 切换动画 ──
        private ActiveTab _prevActiveTab;
        private float _tabTransitionStart;
        private const float TAB_TRANSITION_DURATION = 0.25f;

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

        // ══════════════════════════════════════════
        //  OnGUI
        // ══════════════════════════════════════════
        public void OnGUI()
        {
            if (!_menuVisible) return;
            Event e = Event.current;
            if (e == null) return;

            GUI.color = Color.white;

            if (_activeTab == ActiveTab.Modules && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                foreach (var panel in _panels)
                {
                    var mods = GetModulesForCategory(panel.Category);
                    foreach (var mod in mods)
                    {
                        if (mod.IsBinding)
                        {
                            mod.BindKey = e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Backspace
                                ? KeyCode.None : e.keyCode;
                            mod.IsBinding = false;
                            e.Use(); break;
                        }
                    }
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
                for (int i = 0; i < _panels.Count; i++)
                    _panels[i].IsDragging = false;

            DrawTabBar(e);

            // ── Tab 切换动画计算 ──
            float transitionT = _tabTransitionStart > 0f
                ? Mathf.Clamp01((Time.realtimeSinceStartup - _tabTransitionStart) / TAB_TRANSITION_DURATION) : 1f;
            float ease = 1f - Mathf.Pow(1f - transitionT, 3f);
            bool inTransition = transitionT < 1f && _prevActiveTab != _activeTab;
            float slideDist = Screen.height;

            // ── 旧 Tab：向上滑出屏幕 ──
            if (inTransition)
            {
                float oldOffsetY = -slideDist * ease;
                GUI.BeginGroup(new Rect(0, oldOffsetY, Screen.width, Screen.height));
                DrawTabContent(_prevActiveTab, e);
                GUI.EndGroup();
            }

            // ── 新 Tab：从屏幕上方滑入 ──
            {
                float newOffsetY = inTransition ? slideDist * (1f - ease) : 0f;
                GUI.BeginGroup(new Rect(0, newOffsetY, Screen.width, Screen.height));
                DrawTabContent(_activeTab, e);
                GUI.EndGroup();
            }

            GUI.color = Color.white;

            DrawAuthorCredit();
            CustomGUI.DrawFloatingColorPicker(e);
            GUI.color = Color.white;
        }

        // ══════════════════════════════════════════
        //  Tab 栏
        // ══════════════════════════════════════════
        private void DrawTabBar(Event e)
        {
            float gap = 4f;
            float barWidth = TabWidth * 3 + gap * 2;
            float barX = Screen.width * 0.5f - barWidth * 0.5f;

            GUIStyle tabStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 13,
                fontStyle = FontStyle.Bold, normal = { textColor = Color.white }
            };

            Rect[] tabs = {
                new Rect(barX, TabBarY, TabWidth, TabHeight),
                new Rect(barX + TabWidth + gap, TabBarY, TabWidth, TabHeight),
                new Rect(barX + (TabWidth + gap) * 2, TabBarY, TabWidth, TabHeight)
            };

            string[] labels = { Utils.I18n.Get("tab_modules"), Utils.I18n.Get("tab_globalsettings"), Utils.I18n.Get("tab_credits") };
            ActiveTab[] tabEnums = { ActiveTab.Modules, ActiveTab.GlobalSettings, ActiveTab.Credits };

            for (int i = 0; i < 3; i++)
            {
                bool hover = tabs[i].Contains(e.mousePosition);
                GUI.color = _activeTab == tabEnums[i] ? ColorTabActive : (hover ? ColorTabHover : ColorTabInactive);
                GUI.DrawTexture(tabs[i], WhiteTexture);
                GUI.color = Color.white;
                GUI.Label(tabs[i], labels[i], tabStyle);
                if (e.type == EventType.MouseDown && e.button == 0 && tabs[i].Contains(e.mousePosition))
                {
                    if (_activeTab != tabEnums[i])
                    {
                        _prevActiveTab = _activeTab;
                        _tabTransitionStart = Time.realtimeSinceStartup;
                    }
                    _activeTab = tabEnums[i]; e.Use();
                }
            }
        }

        // ══════════════════════════════════════════
        //  Tab 内容分发
        // ══════════════════════════════════════════
        private void DrawTabContent(ActiveTab tab, Event e)
        {
            if (tab == ActiveTab.Modules)
            {
                int draggingIdx = -1;
                for (int i = 0; i < _panels.Count; i++)
                    if (_panels[i].IsDragging) { draggingIdx = i; break; }
                for (int i = 0; i < _panels.Count; i++)
                    if (i != draggingIdx) DrawPanel(i, e);
                if (draggingIdx >= 0) DrawPanel(draggingIdx, e);
            }
            else if (tab == ActiveTab.GlobalSettings)
            {
                DrawGlobalSettingsPanel(e);
            }
            else
            {
                DrawCreditsPanel(e);
            }
        }

        // ══════════════════════════════════════════
        //  面板绘制 — 抽屉式折叠动画
        // ══════════════════════════════════════════
        private void DrawPanel(int panelIndex, Event e)
        {
            ClickGUIPanel panel = _panels[panelIndex];
            List<ModuleBase> modules = GetModulesForCategory(panel.Category);

            float targetH = ClickGUIPanel.TitleHeight;
            if (panel.IsExpanded)
            {
                foreach (var mod in modules)
                {
                    targetH += ClickGUIPanel.ModuleRowHeight;
                    if (mod.IsExpanded)
                        targetH += ClickGUIPanel.ModuleRowHeight + mod.GetSettingsHeight();
                }
            }

            panel.UpdateHeight(targetH, Time.deltaTime);
            float currentH = panel.CurrentHeight;

            // ── 标题栏 ──
            Rect titleRect = new Rect(panel.Position.x, panel.Position.y, panel.Position.width, ClickGUIPanel.TitleHeight);

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
                alignment = TextAnchor.MiddleCenter, fontSize = 15,
                fontStyle = FontStyle.Bold, normal = { textColor = Color.white }
            };
            GUI.Label(titleRect, GetCategoryDisplayName(panel.Category), titleStyle);

            string arrow = panel.IsExpanded ? "▼" : "▶";
            GUIStyle arrowStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 10,
                normal = { textColor = new Color(0.6f, 0.6f, 0.7f) }
            };
            GUI.Label(new Rect(titleRect.x + titleRect.width - 24f, titleRect.y, 20f, titleRect.height), arrow, arrowStyle);

            // ── 抽屉内容（BeginGroup 裁切 + 动画） ──
            float contentHeight = Mathf.Max(currentH - ClickGUIPanel.TitleHeight, 0f);
            if (contentHeight > 1f)
            {
                Rect clipRect = new Rect(panel.Position.x, panel.Position.y + ClickGUIPanel.TitleHeight,
                    panel.Position.width, contentHeight);
                GUI.BeginGroup(clipRect);

                float localY = 0f;
                for (int m = 0; m < modules.Count; m++)
                {
                    ModuleBase module = modules[m];

                    Rect rowRect = new Rect(0, localY, panel.Position.width, ClickGUIPanel.ModuleRowHeight);
                    bool isHovering = rowRect.Contains(new Vector2(
                        e.mousePosition.x - clipRect.x, e.mousePosition.y - clipRect.y));
                    GUI.color = module.Enabled ? ColorModuleBgEnabled
                        : (isHovering ? ColorModuleBgHover : ColorModuleBg);
                    GUI.DrawTexture(rowRect, WhiteTexture);
                    GUI.color = Color.white;

                    Color textColor = module.Enabled ? Color.white : ColorDisabledText;
                    GUIStyle rowStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleLeft, fontSize = 14,
                        fontStyle = FontStyle.Bold, normal = { textColor = textColor },
                        padding = new RectOffset(10, 4, 0, 0)
                    };
                    GUI.Label(rowRect, module.Name, rowStyle);
                    localY += ClickGUIPanel.ModuleRowHeight;

                    if (module.IsExpanded)
                    {
                        float subStartY = localY;
                        float subTotalHeight = ClickGUIPanel.ModuleRowHeight + module.GetSettingsHeight();
                        Rect subBgRect = new Rect(0, subStartY, panel.Position.width, subTotalHeight);
                        GUI.color = ColorSubBg;
                        GUI.DrawTexture(subBgRect, WhiteTexture);

                        Rect bindRect = new Rect(0, localY, panel.Position.width, ClickGUIPanel.ModuleRowHeight);
                        bool bindHover = bindRect.Contains(new Vector2(
                            e.mousePosition.x - clipRect.x, e.mousePosition.y - clipRect.y));
                        if (bindHover) { GUI.color = ColorSubBgHover; GUI.DrawTexture(bindRect, WhiteTexture); }
                        GUI.color = Color.white;

                        string bindText = module.IsBinding ? "  Bind: [ ... ]"
                            : module.BindKey != KeyCode.None ? "  Bind: [ " + module.BindKey.ToString() + " ]"
                            : "  Bind: [ None ]";
                        GUIStyle bindStyle = new GUIStyle(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleLeft, fontSize = 11,
                            normal = { textColor = Color.white }, padding = new RectOffset(16, 4, 0, 0)
                        };
                        GUI.Label(bindRect, bindText, bindStyle);
                        localY += ClickGUIPanel.ModuleRowHeight;

                        if (e.type == EventType.MouseDown && e.button == 0
                            && new Rect(clipRect.x + 0, clipRect.y + localY - ClickGUIPanel.ModuleRowHeight,
                                panel.Position.width, ClickGUIPanel.ModuleRowHeight).Contains(e.mousePosition))
                        {
                            module.IsBinding = !module.IsBinding;
                            e.Use();
                        }

                        GUI.color = Color.white;
                        float settingsX = 0f;
                        module.DrawSettings(settingsX, ref localY, panel.Position.width, e);

                        Rect subSep = new Rect(0, subStartY + subTotalHeight - 1, panel.Position.width, 1);
                        GUI.color = new Color(0.05f, 0.05f, 0.07f, 0.6f);
                        GUI.DrawTexture(subSep, WhiteTexture);
                    }

                    if (!module.IsExpanded && m < modules.Count - 1)
                    {
                        Rect sepRect = new Rect(0, localY - 1, panel.Position.width, 1);
                        GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.5f);
                        GUI.DrawTexture(sepRect, WhiteTexture);
                    }
                }
                GUI.EndGroup();
            }

            GUI.color = Color.white;

            // ── 鼠标交互 ──
            if (e.isMouse)
            {
                Rect fullRect = new Rect(panel.Position.x, panel.Position.y, panel.Position.width, currentH);

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
                    { panel.IsExpanded = !panel.IsExpanded; e.Use(); return; }
                    if (e.button == 0 && titleRect.Contains(e.mousePosition))
                    {
                        panel.IsDragging = true;
                        panel.DragOffset = e.mousePosition - new Vector2(panel.Position.x, panel.Position.y);
                        e.Use(); return;
                    }

                    if (panel.IsExpanded && fullRect.Contains(e.mousePosition))
                    {
                        float scanY = panel.Position.y + ClickGUIPanel.TitleHeight;
                        for (int m = 0; m < modules.Count; m++)
                        {
                            ModuleBase module = modules[m];
                            if (scanY + ClickGUIPanel.ModuleRowHeight > panel.Position.y + currentH) break;

                            Rect rowRect = new Rect(panel.Position.x, scanY, panel.Position.width, ClickGUIPanel.ModuleRowHeight);
                            if (rowRect.Contains(e.mousePosition))
                            {
                                if (e.button == 1) { module.IsExpanded = !module.IsExpanded; e.Use(); return; }
                                if (e.button == 0)
                                {
                                    module.Enabled = !module.Enabled;
                                    Utils.UISoundHelper.PlayToggleSound(module.Enabled);
                                    Utils.NotificationManager.Show(module.Name, module.Enabled);
                                    if (module.Enabled) module.OnEnable(); else module.OnDisable();
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
        }

        // ══════════════════════════════════════════
        //  辅助方法
        // ══════════════════════════════════════════
        private void SyncModuleLifecycle<T>(bool enabled) where T : ModuleBase
        {
            var mod = _moduleManager.GetModule<T>();
            if (mod == null || mod.Enabled == enabled) return;
            mod.Enabled = enabled;
            if (enabled) mod.OnEnable(); else mod.OnDisable();
        }

        private void DrawGlobalSettingsPanel(Event e)
        {
            float panelX = Screen.width * 0.5f - 200f;
            float panelY = TabBarY + TabHeight + 10f;
            float panelW = 400f;
            float rowH = 32f;

            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.6f, 1.0f, 1f) },
                padding = new RectOffset(10, 0, 0, 0)
            };

            float contentHeight = EstimateContentHeight(rowH);
            float totalPanelHeight = contentHeight + 20f;

            Rect bgRect = new Rect(panelX, panelY, panelW, totalPanelHeight);
            GUI.color = new Color(0.16f, 0.16f, 0.18f, 0.95f);
            GUI.DrawTexture(bgRect, WhiteTexture);
            Rect accentRect = new Rect(panelX, panelY, panelW, 2f);
            GUI.color = ColorAccent;
            GUI.DrawTexture(accentRect, WhiteTexture);
            GUI.color = Color.white;

            float currentY = panelY + 10f;
            bool prevGrid = GlobalSettings.EnableGridSnap;
            bool prevMinimap = GlobalSettings.EnableMinimap;
            bool prevLimb = GlobalSettings.EnableLimbHUD;
            bool prevFeature = GlobalSettings.EnableFeatureList;

            // Grid Snapping
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_grid"), sectionStyle);
            currentY += rowH + 2f;
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_enable_grid"), ref GlobalSettings.EnableGridSnap);
            if (GlobalSettings.EnableGridSnap)
            {
                float val = GlobalSettings.GridSize;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_grid_size") + $": {val:F0}" + Utils.I18n.Get("unit_px"), ref val, 4f, 80f);
                GlobalSettings.GridSize = val;
            }
            currentY += 6f;

            // Minimap
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_minimap"), sectionStyle);
            currentY += rowH + 2f;
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_enable_minimap"), ref GlobalSettings.EnableMinimap);
            if (GlobalSettings.EnableMinimap)
            {
                float size = GlobalSettings.MinimapRect.width;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_minimap_size") + $": {size:F0}" + Utils.I18n.Get("unit_px"), ref size, 80f, 400f);
                Rect r = GlobalSettings.MinimapRect;
                GlobalSettings.MinimapRect = new Rect(r.x, r.y, size, size);
                float radius = GlobalSettings.MinimapRadius;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_minimap_radius") + $": {radius:F0}" + Utils.I18n.Get("unit_m"), ref radius, 10f, 100f);
                GlobalSettings.MinimapRadius = radius;
            }
            currentY += 6f;

            // Limb HUD
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_limb"), sectionStyle);
            currentY += rowH + 2f;
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_enable_limb"), ref GlobalSettings.EnableLimbHUD);
            if (GlobalSettings.EnableLimbHUD)
            {
                float scale = GlobalSettings.LimbHUDScale;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_limb_scale") + $": {scale:F2}" + Utils.I18n.Get("unit_x"), ref scale, 0.3f, 1.5f);
                GlobalSettings.LimbHUDScale = scale;
                float posX = GlobalSettings.LimbHUDPosition.x;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_limb_pos_x") + $": {posX:F0}" + Utils.I18n.Get("unit_px"), ref posX, -2000f, 0f);
                float posY = GlobalSettings.LimbHUDPosition.y;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_limb_pos_y") + $": {posY:F0}" + Utils.I18n.Get("unit_px"), ref posY, 0f, 1500f);
                GlobalSettings.LimbHUDPosition = new Vector2(posX, posY);
            }
            currentY += 6f;

            // Logo Overlay
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_logo"), sectionStyle);
            currentY += rowH + 2f;
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_enable_logo"), ref GlobalSettings.EnableLogoOverlay);
            if (GlobalSettings.EnableLogoOverlay)
            {
                float h = GlobalSettings.LogoOverlayHeight;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_logo_height") + $": {h:F0}" + Utils.I18n.Get("unit_px"), ref h, 200f, 500f);
                GlobalSettings.LogoOverlayHeight = h;

                string[] logoNames = { "Logo 1", "Logo 2" };
                int logoIdx = GlobalSettings.LogoIndex;
                DrawSelectorRow(panelX, ref currentY, panelW, rowH, e,
                    Utils.I18n.Get("gs_logo_select") ?? "Logo", logoNames, ref logoIdx);
                GlobalSettings.LogoIndex = Mathf.Clamp(logoIdx, 0, 1);
            }
            currentY += 6f;

            // Mod Radar
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_mod_radar"), ref GlobalSettings.EnableModRadar);
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_hide_guid"), ref GlobalSettings.HideCheaterModGUID);
            currentY += 6f;

            // Feature List
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_featurelist"), sectionStyle);
            currentY += rowH + 2f;
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_enable_featurelist"), ref GlobalSettings.EnableFeatureList);
            if (GlobalSettings.EnableFeatureList)
            {
                DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_fl_align"), ref GlobalSettings.FeatureListAlignLeft);
                float fontSize = GlobalSettings.FeatureListFontSize;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_fl_fontsize") + $": {fontSize:F0}" + Utils.I18n.Get("unit_px"), ref fontSize, 8, 24);
                GlobalSettings.FeatureListFontSize = Mathf.RoundToInt(fontSize);
                Color flColor = GlobalSettings.FeatureListColor;
                CustomGUI.DrawColorPicker(panelX, ref currentY, panelW, e, Utils.I18n.Get("gs_fl_color") + ":", ref flColor);
                GlobalSettings.FeatureListColor = flColor;
                DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_fl_rainbow"), ref GlobalSettings.FeatureListRainbow);
                DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_fl_bg"), ref GlobalSettings.FeatureListBackground);
                DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_fl_accent"), ref GlobalSettings.FeatureListAccentLine);
            }
            currentY += 6f;

            // UI Feedback
            GUI.Label(new Rect(panelX, currentY, panelW, rowH), Utils.I18n.Get("gs_section_feedback"), sectionStyle);
            currentY += rowH + 2f;
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_enable_toggle_sound"), ref GlobalSettings.EnableToggleSound);
            DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_enable_notifications"), ref GlobalSettings.EnableNotifications);
            if (GlobalSettings.EnableNotifications)
            {
                float notifY = GlobalSettings.NotificationYPosition;
                DrawSliderRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_notif_y") + $": {notifY:F0}" + Utils.I18n.Get("unit_px"), ref notifY, 0f, 600f);
                GlobalSettings.NotificationYPosition = notifY;
                DrawToggleRow(panelX, ref currentY, panelW, rowH, e, Utils.I18n.Get("gs_notif_right"), ref GlobalSettings.NotificationSlideFromRight);
            }

            if (prevGrid != GlobalSettings.EnableGridSnap) { }
            if (prevMinimap != GlobalSettings.EnableMinimap) SyncModuleLifecycle<MinimapModule>(GlobalSettings.EnableMinimap);
            if (prevLimb != GlobalSettings.EnableLimbHUD) SyncModuleLifecycle<LimbStatusModule>(GlobalSettings.EnableLimbHUD);
            if (prevFeature != GlobalSettings.EnableFeatureList) SyncModuleLifecycle<FeatureListModule>(GlobalSettings.EnableFeatureList);
        }

        private float EstimateContentHeight(float rowH)
        {
            float h = 0f;
            h += rowH + 6f; h += rowH;
            if (GlobalSettings.EnableGridSnap) h += rowH; h += 6f;
            h += rowH + 6f; h += rowH;
            if (GlobalSettings.EnableMinimap) h += rowH * 2; h += 6f;
            h += rowH + 6f; h += rowH;
            if (GlobalSettings.EnableLimbHUD) h += rowH * 3; h += 6f;
            h += rowH + 6f; h += rowH;
            if (GlobalSettings.EnableLogoOverlay) h += rowH * 2; h += 6f;
            h += rowH * 2; h += 6f;
            h += rowH + 6f; h += rowH;
            if (GlobalSettings.EnableFeatureList) h += rowH * 6; h += 6f;
            h += rowH + 6f; h += rowH * 2;
            if (GlobalSettings.EnableNotifications) h += rowH * 2; h += 6f;
            return h;
        }

        private void DrawSelectorRow(float x, ref float y, float w, float rh, Event e,
            string label, string[] options, ref int currentIndex)
        {
            GUIStyle ls = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = Color.white }, padding = new RectOffset(14, 0, 4, 0) };
            GUI.Label(new Rect(x, y, w * 0.35f, rh), label, ls);
            float sy = y + 2f;
            float bw = 24f;
            Rect lb = new Rect(x + w * 0.35f, sy, bw, rh - 4f);
            Rect rb = new Rect(x + w - 28f, sy, bw, rh - 4f);
            Rect lr = new Rect(x + w * 0.35f + bw, y, w * 0.65f - 52f, rh);
            GUI.color = ColorAccent;
            GUI.DrawTexture(lb, WhiteTexture);
            GUI.color = ColorAccent;
            GUI.DrawTexture(rb, WhiteTexture);
            GUIStyle asy = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, normal = { textColor = new Color(0.35f, 0.35f, 0.38f) } };
            GUI.Label(lb, "◀", asy);
            GUI.Label(rb, "▶", asy);
            GUIStyle os = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11, normal = { textColor = Color.white } };
            if (currentIndex >= 0 && currentIndex < options.Length) GUI.Label(lr, options[currentIndex], os);
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (lb.Contains(e.mousePosition)) { currentIndex--; e.Use(); }
                if (rb.Contains(e.mousePosition)) { currentIndex++; e.Use(); }
            }
            if (currentIndex < 0) currentIndex = options.Length - 1;
            if (currentIndex >= options.Length) currentIndex = 0;
            y += rh;
        }

        private void DrawToggleRow(float x, ref float y, float w, float rh, Event e, string label, ref bool value)
        {
            Rect rowRect = new Rect(x, y, w, rh);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? ColorModuleBgEnabled : (hover ? ColorModuleBgHover : ColorModuleBg);
            GUI.DrawTexture(rowRect, WhiteTexture);
            GUI.color = Color.white;
            string display = (value ? "[ON]  " : "[OFF] ") + label;
            GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = value ? Color.white : ColorDisabledText }, padding = new RectOffset(14, 0, 4, 0) };
            GUI.Label(rowRect, display, s);
            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition)) { value = !value; e.Use(); }
            y += rh;
        }

        private void DrawSliderRow(float x, ref float y, float w, float rh, Event e, string label, ref float value, float min, float max)
        {
            GUIStyle ls = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = Color.white }, padding = new RectOffset(14, 0, 4, 0) };
            GUI.Label(new Rect(x, y, w, rh * 0.5f), label, ls);
            float sy = y + rh * 0.4f;
            Rect sr = new Rect(x + 14f, sy, w - 28f, rh * 0.5f);
            float nv = DrawSlider(sr, value, min, max, e, ColorSliderFill, 8f);
            if (Mathf.Abs(nv - value) > 0.001f) value = nv;
            y += rh;
        }

        public static float DrawSlider(Rect sliderRect, float currentValue, float min, float max,
            Event e, Color? fillColor = null, float handleWidth = 8f)
        {
            float slotHeight = 6f;
            Rect slotRect = new Rect(sliderRect.x, sliderRect.y + (sliderRect.height - slotHeight) / 2, sliderRect.width, slotHeight);
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
            { currentValue = Mathf.Lerp(min, max, Mathf.Clamp01((e.mousePosition.x - slotRect.x) / slotRect.width)); e.Use(); }
            if (e.type == EventType.MouseDown && e.button == 0 && sliderRect.Contains(e.mousePosition))
            { currentValue = Mathf.Lerp(min, max, Mathf.Clamp01((e.mousePosition.x - slotRect.x) / slotRect.width)); e.Use(); }
            return currentValue;
        }

        private void DrawCreditsPanel(Event e)
        {
            float panelW = 460f;
            float panelX = Screen.width * 0.5f - panelW * 0.5f;
            float panelY = Screen.height * 0.1f;
            float rowH = 22f;

            GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.6f, 1.0f) }, padding = new RectOffset(0, 0, 4, 0)
            };
            GUIStyle contentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, normal = { textColor = Color.white }, padding = new RectOffset(22, 0, 2, 0)
            };

            int lines = 1 + 2 + 1 + 3 + 1 + 2;
            float contentH = lines * (rowH + 2f) + 20f;

            Rect bgRect = new Rect(panelX, panelY, panelW, contentH);
            GUI.color = new Color(0.16f, 0.16f, 0.18f, 0.95f);
            GUI.DrawTexture(bgRect, WhiteTexture);
            GUI.color = ColorAccent;
            GUI.DrawTexture(new Rect(panelX, panelY, panelW, 2f), WhiteTexture);
            GUI.color = Color.white;

            float cy = panelY + 10f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), "--- " + Utils.I18n.Get("credits_developer") + " ---", sectionStyle); cy += rowH + 2f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), "Dylanvip2024：" + Utils.I18n.Get("credits_author_desc"), contentStyle); cy += rowH + 2f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), "Darnmik：" + Utils.I18n.Get("credits_darnmik_desc"), contentStyle); cy += rowH + 6f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), "--- " + Utils.I18n.Get("credits_thanks") + " ---", sectionStyle); cy += rowH + 2f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), Utils.I18n.Get("credits_wanxi"), contentStyle); cy += rowH + 2f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), Utils.I18n.Get("credits_yumo"), contentStyle); cy += rowH + 2f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), Utils.I18n.Get("credits_andian"), contentStyle); cy += rowH + 6f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), "--- " + Utils.I18n.Get("credits_about") + " ---", sectionStyle); cy += rowH + 2f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), Utils.I18n.Get("credits_author_account"), contentStyle); cy += rowH + 2f;
            GUI.Label(new Rect(panelX, cy, panelW, rowH), Utils.I18n.Get("credits_source_url"), contentStyle);
        }

        private static void DrawAuthorCredit()
        {
            float authorW = 190f, authorH = 22f;
            Rect authorRect = new Rect(Screen.width - authorW - 6f, Screen.height - authorH - 6f, authorW, authorH);
            GUIStyle authorStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight, fontSize = 10, fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.2f, 0.6f, 1.0f, 0.7f) }
            };
            GUI.Label(authorRect, "Author: BilBil Dylanvip2024", authorStyle);
        }

        private List<ModuleBase> GetModulesForCategory(ModuleCategory category)
        {
            var all = _moduleManager.GetAllModules();
            List<ModuleBase> r = new List<ModuleBase>();
            foreach (var m in all) if (m.Category == category) r.Add(m);
            return r;
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
    }
}
