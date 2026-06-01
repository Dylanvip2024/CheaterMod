using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// 近战攻速修改 — 通过 Harmony Postfix 修改 Body.Attack 后的 attackCooldown，
    /// 实现自定义近战武器/徒手攻击速度倍率。
    /// </summary>
    public class MeleeSpeed : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_melee_speed") ?? "攻速调整";
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable()  => Settings.IsMeleeSpeedEnabled = true;
        public override void OnDisable() => Settings.IsMeleeSpeedEnabled = false;

        public override float GetSettingsHeight() => 28f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            float val = Settings.MeleeSpeedMultiplier;
            string label = (Utils.I18n.Get("set_melee_speed") ?? "攻速倍率") + $": {val:F1}x";

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            GUI.Label(new Rect(x + 8, y, 95, 22), label, labelStyle);

            Rect sliderRect = new Rect(x + 100, y + 6, width - 115, 14);
            float newVal = ClickGUIManager.DrawSlider(sliderRect, val, 1f, 8f, e);
            if (Mathf.Abs(newVal - val) > 0.001f) Settings.MeleeSpeedMultiplier = newVal;

            y += 28f;
        }
    }
}
