using FullBrightMod.Core;

namespace FullBrightMod.Modules.Player
{
    public class RibCrusher : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_rib_crusher");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()  => Settings.IsRibCrusherEnabled = true;
        public override void OnDisable() => Settings.IsRibCrusherEnabled = false;
    }
}
