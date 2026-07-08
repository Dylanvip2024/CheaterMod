using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class TrapESP : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_trapesp");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => ModSettings.IsTrapEspEnabled = true;
        public override void OnDisable() => ModSettings.IsTrapEspEnabled = false;

        public override float GetSettingsHeight() => 90f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_esp_wireframe"),
                ref ModSettings.IsTrapEspWireframeEnabled);

            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_esp_glow"),
                ref ModSettings.IsTrapGlowEnabled);

            CustomGUI.DrawColorPicker(x, ref y, width, e,
                Utils.I18n.Get("set_trap_color") + ":",
                ref ModSettings.SelectedTrapColor);
        }
    }
}
