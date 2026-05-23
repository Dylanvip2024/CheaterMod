using System.Collections.Generic;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.UI
{
    /// <summary>
    /// ClickGUI 管理器 —— 纯代码手绘的模块化 GUI 系统。
    /// 不使用 GUI.Window / GUILayout / GUI.Button 等内置组件。
    ///
    /// 交互逻辑：
    ///   - 鼠标左键拖拽标题栏 → 移动面板
    ///   - 鼠标右键点击标题栏 → 折叠/展开整个分类面板
    ///   - 鼠标左键点击模块行   → 切换模块启用/禁用
    ///   - 鼠标右键点击模块行   → 展开/收起模块设置子菜单
    ///   - 展开后第一行          → Keybind 绑定（点击进入监听，按键确认）
    ///   - 展开后后续行          → 模块自定义设置（如 Slider）
    /// </summary>
    public class ClickGUIManager
    {
        // ---- 核心引用 ----
        private readonly ModuleManager _moduleManager;
        private readonly List<ClickGUIPanel> _panels;

        /// <summary>暴露面板列表，供 ConfigManager 持久化 UI 布局</summary>
        public List<ClickGUIPanel> Panels => _panels;

        // ---- 渲染材质（internal 供模块 DrawSettings 使用） ----
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
        private static readonly Color ColorSubBg           = new Color(0.325f, 0.325f, 0.34f, 0.95f); // 子面板更深背景
        private static readonly Color ColorSubBgHover      = new Color(0.18f, 0.18f, 0.22f, 0.90f); // 子面板行悬停
        private static readonly Color ColorSliderFill      = new Color(0.2f, 0.6f, 1.0f, 0.7f);     // 滑块填充色

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

            // ==== 按键绑定监听（全局：任何模块处于 IsBinding 状态时拦截按键） ====
            if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                foreach (var panel in _panels)
                {
                    var mods = GetModulesForCategory(panel.Category);
                    foreach (var mod in mods)
                    {
                        if (mod.IsBinding)
                        {
                            // Esc 或 Backspace = 取消绑定
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

            // 鼠标松开清除拖拽
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                for (int i = 0; i < _panels.Count; i++)
                    _panels[i].IsDragging = false;
            }

            // 找出拖拽中的面板，放到最后绘制（最上层）
            int draggingIdx = -1;
            for (int i = 0; i < _panels.Count; i++)
                if (_panels[i].IsDragging) { draggingIdx = i; break; }

            for (int i = 0; i < _panels.Count; i++)
                if (i != draggingIdx) DrawPanel(i, e);

            if (draggingIdx >= 0)
                DrawPanel(draggingIdx, e);
        }

        // =========================================================
        // 面板绘制
        // =========================================================
        private void DrawPanel(int panelIndex, Event e)
        {
            ClickGUIPanel panel = _panels[panelIndex];
            List<ModuleBase> modules = GetModulesForCategory(panel.Category);

            // ==== 计算面板总高度（含展开模块的子菜单） ====
            float totalHeight = ClickGUIPanel.TitleHeight;
            if (panel.IsExpanded)
            {
                foreach (var mod in modules)
                {
                    totalHeight += ClickGUIPanel.ModuleRowHeight; // 模块行本身
                    if (mod.IsExpanded)
                    {
                        totalHeight += ClickGUIPanel.ModuleRowHeight; // Keybind 行
                        totalHeight += mod.GetSettingsHeight();       // 自定义设置
                    }
                }
            }

            Rect panelRect = new Rect(panel.Position.x, panel.Position.y, panel.Position.width, totalHeight);

            // ====== 标题栏 ======
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

            // ====== 模块列表（含展开子菜单） ======
            if (panel.IsExpanded)
            {
                float currentY = panelRect.y + ClickGUIPanel.TitleHeight;

                for (int m = 0; m < modules.Count; m++)
                {
                    ModuleBase module = modules[m];

                    // -- 模块行 --
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

                    // -- 展开子菜单（模块右键展开） --
                    if (module.IsExpanded)
                    {
                        float subStartY = currentY;
                        float subTotalHeight = ClickGUIPanel.ModuleRowHeight + module.GetSettingsHeight();

                        // 子面板背景
                        Rect subBgRect = new Rect(panelRect.x, subStartY, panelRect.width, subTotalHeight);
                        GUI.color = ColorSubBg;
                        GUI.DrawTexture(subBgRect, WhiteTexture);

                        // --- Keybind 行 ---
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

                        // --- Keybind 行点击事件 ---
                        if (e.type == EventType.MouseDown && e.button == 0 && bindRect.Contains(e.mousePosition))
                        {
                            module.IsBinding = !module.IsBinding; // 切换监听状态
                            e.Use();
                        }

                        GUI.color = Color.white;

                        // --- 模块自定义设置项 ---
                        float settingsX = panelRect.x;
                        float settingsStartY = currentY;
                        module.DrawSettings(settingsX, ref currentY, panelRect.width, e);

                        // --- 子面板底部分割线 ---
                        Rect subSep = new Rect(panelRect.x, subStartY + subTotalHeight - 1, panelRect.width, 1);
                        GUI.color = new Color(0.05f, 0.05f, 0.07f, 0.6f);
                        GUI.DrawTexture(subSep, WhiteTexture);
                    }

                    // -- 模块行底部分割线（未展开时） --
                    if (!module.IsExpanded && m < modules.Count - 1)
                    {
                        Rect sepRect = new Rect(panelRect.x, currentY - 1, panelRect.width, 1);
                        GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.5f);
                        GUI.DrawTexture(sepRect, WhiteTexture);
                    }
                }
            }

            // ====== 事件处理 ======
            if (e.isMouse)
            {
                if (panel.IsDragging && e.type == EventType.MouseDrag && e.button == 0)
                {
                    panel.Position.x = e.mousePosition.x - panel.DragOffset.x;
                    panel.Position.y = e.mousePosition.y - panel.DragOffset.y;
                    e.Use();
                }

                if (e.type == EventType.MouseDown)
                {
                    // 标题栏右键 → 折叠/展开分类面板
                    if (e.button == 1 && titleRect.Contains(e.mousePosition))
                    {
                        panel.IsExpanded = !panel.IsExpanded;
                        e.Use(); return;
                    }
                    // 标题栏左键 → 拖拽
                    if (e.button == 0 && titleRect.Contains(e.mousePosition))
                    {
                        panel.IsDragging = true;
                        panel.DragOffset = e.mousePosition - new Vector2(panel.Position.x, panel.Position.y);
                        e.Use(); return;
                    }

                    // 模块行事件
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
                                    // 右键 → 展开/收起模块设置
                                    module.IsExpanded = !module.IsExpanded;
                                    e.Use(); return;
                                }
                                if (e.button == 0)
                                {
                                    // 左键 → 切换启用
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
        // Slider 绘制工具（供模块 DrawSettings 调用）
        // =========================================================
        /// <summary>在指定矩形区域内绘制一个水平滑动条，返回滑块值 [min, max]</summary>
        public static float DrawSlider(Rect sliderRect, float currentValue, float min, float max,
            Event e, Color? fillColor = null, float handleWidth = 8f)
        {
            // 背景槽
            float slotHeight = 6f;
            Rect slotRect = new Rect(sliderRect.x, sliderRect.y + (sliderRect.height - slotHeight) / 2,
                sliderRect.width, slotHeight);
            GUI.color = new Color(0.25f, 0.25f, 0.28f, 1f);
            GUI.DrawTexture(slotRect, WhiteTexture);

            // 填充部分
            float t = Mathf.InverseLerp(min, max, currentValue);
            Rect fillRect = new Rect(slotRect.x, slotRect.y, slotRect.width * t, slotHeight);
            GUI.color = fillColor ?? ColorSliderFill;
            GUI.DrawTexture(fillRect, WhiteTexture);

            // 手柄
            float handleX = slotRect.x + slotRect.width * t - handleWidth / 2;
            Rect handleRect = new Rect(handleX, sliderRect.y, handleWidth, sliderRect.height);
            GUI.color = Color.white;
            GUI.DrawTexture(handleRect, WhiteTexture);

            // 鼠标拖拽交互
            if (e.type == EventType.MouseDrag && e.button == 0 && sliderRect.Contains(e.mousePosition))
            {
                float mouseT = (e.mousePosition.x - slotRect.x) / slotRect.width;
                currentValue = Mathf.Lerp(min, max, Mathf.Clamp01(mouseT));
                e.Use();
            }
            // 点击跳转
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
    }
}
