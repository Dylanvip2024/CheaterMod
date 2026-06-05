using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    public class AutoBandage : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_autobandage");
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable() => ModSettings.IsAutoBandageEnabled = true;
        public override void OnDisable() => ModSettings.IsAutoBandageEnabled = false;
    }
}