using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class CameraZoom : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_camerazoom");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => Settings.IsCameraZoomEnabled = true;
        public override void OnDisable() => Settings.IsCameraZoomEnabled = false;

        public override float GetSettingsHeight() => 30f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // 左侧标签
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            Rect labelRect = new Rect(x + 8, y, 65, 22);
            GUI.Label(labelRect, $"  {Utils.I18n.Get("set_camera_size")}: {Settings.CustomCameraSize:F1}", labelStyle);

            // 滑块
            Rect barRect = new Rect(x + 75, y + 6, width - 90, 14);
            Settings.CustomCameraSize = ClickGUIManager.DrawSlider(barRect, Settings.CustomCameraSize, 5f, 50f, e);

            y += 30f;
        }
    }
}
