using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    public class AutoBandage : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_autobandage");
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable() => Settings.IsAutoBandageEnabled = true;
        public override void OnDisable() => Settings.IsAutoBandageEnabled = false;
    }
}