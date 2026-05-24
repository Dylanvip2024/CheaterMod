using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class FetchMacro : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_fetch_macro") ?? "隔空取物 (宏)";
        public override ModuleCategory Category => ModuleCategory.Misc;

        public override void OnEnable() => Settings.IsFetchMacroEnabled = true;
        public override void OnDisable() => Settings.IsFetchMacroEnabled = false;

        public override float GetSettingsHeight() => 30f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            Rect labelRect = new Rect(x + 8, y, 70, 22);
            GUI.Label(labelRect, $"  {Utils.I18n.Get("set_fetch_distance") ?? "拾取距离"}: {Settings.FetchDistance:F1}m", labelStyle);

            Rect barRect = new Rect(x + 78, y + 6, width - 93, 14);
            Settings.FetchDistance = ClickGUIManager.DrawSlider(barRect, Settings.FetchDistance, 5f, 100f, e);

            y += 30f;
        }

        // 重写基类的 OnUpdate（ModuleManager 会每帧调用它）
        public override void OnUpdate()
        {
            // 如果底层的 MacroPatches 执行完毕并将 Settings 设为了 false，
            // 这里我们自动同步关闭模块自身的 Enabled 状态，让 ClickGUI 上的高亮消失。
            if (!Settings.IsFetchMacroEnabled && this.Enabled)
            {
                this.Enabled = false;
            }
        }
    }
}