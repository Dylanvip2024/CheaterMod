using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class MedicalPanelPlus : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_medical_panel_plus") ?? "医疗面板++";
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable() => ModSettings.IsMedicalPanelPlusEnabled = true;
        public override void OnDisable() => ModSettings.IsMedicalPanelPlusEnabled = false;

        public override float GetSettingsHeight()
        {
            return 2 * 28f;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_mp_panel_always") ?? "医疗面板常显示",
                ref ModSettings.MedicalPanelAlwaysAccessible);
            DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_mp_infection_always") ?? "感染值常显示",
                ref ModSettings.MedicalPanelInfectionAlways);
        }

        private static void DrawToggle(float x, ref float y, float width, Event e, string label, ref bool value)
        {
            Rect rowRect = new Rect(x + 4, y, width - 8, 24f);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? new Color(0.3f, 0.6f, 0.3f, 0.75f)
                : (hover ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(rowRect, UI.ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;

            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Normal,
                normal = { textColor = value ? Color.white : new Color(0.6f, 0.6f, 0.6f) },
                padding = new RectOffset(10, 0, 4, 0)
            };
            GUI.Label(rowRect, (value ? "[开]  " : "[关]  ") + label, s);

            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                value = !value;
                e.Use();
            }
            y += 28f;
        }
    }
}

