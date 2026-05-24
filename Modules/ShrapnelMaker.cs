using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    public class ShrapnelMaker : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_shrapnel_maker");
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable() => Settings.IsShrapnelMakerEnabled = true;
        public override void OnDisable() => Settings.IsShrapnelMakerEnabled = false;
    }
}