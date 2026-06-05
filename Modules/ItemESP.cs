using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class ItemESP : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_itemesp");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => ModSettings.IsItemEspEnabled = true;
        public override void OnDisable() => ModSettings.IsItemEspEnabled = false;

        public override float GetSettingsHeight() => 60f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_esp_wireframe"),
                ref ModSettings.IsItemEspWireframeEnabled);

            CustomGUI.DrawColorPicker(x, ref y, width, e,
                Utils.I18n.Get("set_item_color") + ":",
                ref ModSettings.SelectedEspColor);
        }
    }
}
