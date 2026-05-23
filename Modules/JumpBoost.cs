using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class JumpBoost : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_jumpboost");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => Settings.IsJumpBoostEnabled = true;
        public override void OnDisable() => Settings.IsJumpBoostEnabled = false;

        public override float GetSettingsHeight() => 30f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // 左侧标签 "跳跃力度: XX.X"
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            Rect labelRect = new Rect(x + 8, y, 70, 22);
            GUI.Label(labelRect, $"  {Utils.I18n.Get("set_jump_force")}: {Settings.CustomJumpForce:F1}", labelStyle);

            // 滑块（范围 5f ~ 80f）
            Rect barRect = new Rect(x + 78, y + 6, width - 93, 14);
            Settings.CustomJumpForce = ClickGUIManager.DrawSlider(barRect, Settings.CustomJumpForce, 5f, 80f, e);

            y += 30f;
        }
    }
}
