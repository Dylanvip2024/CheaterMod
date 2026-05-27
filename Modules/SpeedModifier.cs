using FullBrightMod.Core;
using FullBrightMod.UI;
using HarmonyLib;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class SpeedModifier : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_speed_modifier");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => Settings.IsSpeedModifierEnabled = true;
        public override void OnDisable() => Settings.IsSpeedModifierEnabled = false;

        public override float GetSettingsHeight() => 26f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            float val = Settings.CustomSpeedMultiplier;
            Rect sliderRect = new Rect(x + 55, y + 3, width - 70, 18);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            GUI.Label(new Rect(x + 8, y, 50, 22),
                Utils.I18n.Get("set_speed_mult") + $": {val:F1}x", labelStyle);

            float newVal = ClickGUIManager.DrawSlider(sliderRect, val, 1f, 10f, e);
            if (Mathf.Abs(newVal - val) > 0.001f) Settings.CustomSpeedMultiplier = newVal;

            y += 26f;
        }
    }

    public class AntiWeight : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_anti_weight");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => Settings.IsAntiWeightEnabled = true;
        public override void OnDisable() => Settings.IsAntiWeightEnabled = false;

        public override void OnUpdate()
        {
            if (!Settings.IsAntiWeightEnabled) return;
            var body = PlayerCamera.main?.body;
            if (body == null) return;

            // 每帧强制清零负重和重量移动惩罚
            Traverse.Create(body).Field("overEncumberance").SetValue(0f);
            Traverse.Create(body).Property("currentWeightMovementMult").SetValue(1f);
        }
    }
}
