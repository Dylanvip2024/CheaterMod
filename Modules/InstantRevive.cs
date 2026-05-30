using FullBrightMod.Core;

namespace FullBrightMod.Modules.Player
{
    public class InstantRevive : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_instant_revive");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()  => Settings.IsInstantReviveEnabled = true;
        public override void OnDisable() => Settings.IsInstantReviveEnabled = false;
    }
}
