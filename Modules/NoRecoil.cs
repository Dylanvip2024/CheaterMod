using FullBrightMod.Core;
namespace FullBrightMod.Modules {
    public class NoRecoil : ModuleBase {
        public override string Name => Utils.I18n.Get("mod_no_recoil");
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable() => ModSettings.IsNoRecoilEnabled = true;
        public override void OnDisable() => ModSettings.IsNoRecoilEnabled = false;
    }
}