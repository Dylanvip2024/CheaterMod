using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class RapidFire : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_rapid_fire") ?? "自定义射速";
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable() => Settings.IsRapidFireEnabled = true;
        public override void OnDisable() => Settings.IsRapidFireEnabled = false;

        // 子菜单展开时的高度：一行滑块
        public override float GetSettingsHeight() => 30f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // 配置扁平灰色标签样式
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            
            // 绘制标签 "射速倍率: X.X"
            Rect labelRect = new Rect(x + 8, y, 90, 22);
            GUI.Label(labelRect, $"  {Utils.I18n.Get("set_fire_rate_mult") ?? "射速倍率"}: {Settings.CustomFireRateMultiplier:F1}x", labelStyle);

            // 绘制手绘滑块（范围 1.0f 到 10.0f 倍速）
            Rect barRect = new Rect(x + 95, y + 6, width - 110, 14);
            Settings.CustomFireRateMultiplier = ClickGUIManager.DrawSlider(barRect, Settings.CustomFireRateMultiplier, 1f, 10f, e);

            y += 30f;
        }
    }
}