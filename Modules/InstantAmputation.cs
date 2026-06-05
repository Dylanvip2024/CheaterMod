using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    public class InstantAmputation : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_instant_amputation");
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable() => ModSettings.IsInstantAmputationEnabled = true;
        public override void OnDisable() => ModSettings.IsInstantAmputationEnabled = false;
    }
}