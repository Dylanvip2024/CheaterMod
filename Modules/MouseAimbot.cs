using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules {
    public class MouseAimbot : ModuleBase {
        public override string Name => Utils.I18n.Get("mod_mouse_aimbot");
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable() => Settings.IsMouseAimbotEnabled = true;
        public override void OnDisable() => Settings.IsMouseAimbotEnabled = false;

        public override float GetSettingsHeight() => 30f;
        public override void DrawSettings(float x, ref float y, float width, Event e) {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.MiddleLeft, fontSize = 10, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            Rect labelRect = new Rect(x + 8, y, 90, 22);
            GUI.Label(labelRect, $"  {Utils.I18n.Get("set_aimbot_radius") ?? "吸附范围"}: {(int)Settings.AimbotRadius}px", labelStyle);

            Rect barRect = new Rect(x + 95, y + 6, width - 110, 14);
            Settings.AimbotRadius = ClickGUIManager.DrawSlider(barRect, Settings.AimbotRadius, 30f, 400f, e);
            y += 30f;
        }
    }
}