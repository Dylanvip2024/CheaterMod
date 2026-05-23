using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class TrapESP : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_trapesp");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => Settings.IsTrapEspEnabled = true;
        public override void OnDisable() => Settings.IsTrapEspEnabled = false;

        public override float GetSettingsHeight() => 30f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            ItemESP.DrawColorPicker(x, ref y, width, e,
                Utils.I18n.Get("set_trap_color") + ":",
                ref Settings.SelectedTrapColor);
        }
    }
}
