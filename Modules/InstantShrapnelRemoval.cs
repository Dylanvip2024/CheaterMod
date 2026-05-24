using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    public class InstantShrapnelRemoval : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_instant_shrapnel") ?? "神医圣手 (秒拔破片)";
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable() => Settings.IsInstantShrapnelRemovalEnabled = true;
        public override void OnDisable() => Settings.IsInstantShrapnelRemovalEnabled = false;
    }
}