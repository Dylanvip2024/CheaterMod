using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// 反屏幕特效模块 — 提供 7 项独立开关，允许用户选择性屏蔽特定类别的负面屏幕特效。
    /// 模块启用后，由 AntiScreenEffectsPatch 根据各开关控制 PlayerCamera 的 Shader 参数和 UI 覆盖层。
    /// </summary>
    public class AntiScreenEffects : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_anti_screen_fx") ?? "反屏幕特效";
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  { Settings.IsAntiScreenEffectsActive = true; }
        public override void OnDisable() { Settings.IsAntiScreenEffectsActive = false; }

        public override float GetSettingsHeight()
        {
            return 7 * 28f;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // 子开关始终可见，独立于模块的启用状态
            DrawToggle(x, ref y, width, e, Utils.I18n.Get("fx_blood")         ?? "流血/疼痛红屏",    ref Settings.DisableBloodFx);
            DrawToggle(x, ref y, width, e, Utils.I18n.Get("fx_temperature")   ?? "温度异常特效",      ref Settings.DisableTemperatureFx);
            DrawToggle(x, ref y, width, e, Utils.I18n.Get("fx_mental")        ?? "精神异常特效",      ref Settings.DisableMentalFx);
            DrawToggle(x, ref y, width, e, Utils.I18n.Get("fx_blur")          ?? "模糊/饥饿特效",     ref Settings.DisableBlurFx);
            DrawToggle(x, ref y, width, e, Utils.I18n.Get("fx_radiation")     ?? "辐射噪点特效",      ref Settings.DisableRadiationFx);
            DrawToggle(x, ref y, width, e, Utils.I18n.Get("fx_damage")        ?? "损伤视觉遮罩",      ref Settings.DisableDamageFx);
            DrawToggle(x, ref y, width, e, Utils.I18n.Get("fx_ui_overlays")   ?? "UI 覆盖层",         ref Settings.DisableUIOverlays);
        }

        private static void DrawToggle(float x, ref float y, float width, Event e, string label, ref bool value)
        {
            Rect rowRect = new Rect(x + 4, y, width - 8, 24f);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? new Color(0.8f, 0.3f, 0.3f, 0.75f)
                : (hover ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(rowRect, UI.ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;

            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Normal,
                normal = { textColor = value ? Color.white : new Color(0.6f, 0.6f, 0.6f) },
                padding = new RectOffset(10, 0, 4, 0)
            };
            GUI.Label(rowRect, (value ? "[屏蔽] " : "[显示] ") + label, s);

            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                value = !value;
                e.Use();
            }
            y += 28f;
        }
    }
}
