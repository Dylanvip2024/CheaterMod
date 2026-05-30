using FullBrightMod.Core;

namespace FullBrightMod.Modules.Player
{
    public class AutoDislocation : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_auto_dislocation");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()  => Settings.IsAutoDislocationEnabled = true;
        public override void OnDisable() => Settings.IsAutoDislocationEnabled = false;
    }
}
