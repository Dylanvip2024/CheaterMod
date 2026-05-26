using FullBrightMod.Core;
namespace FullBrightMod.Modules {
    public class NoJam : ModuleBase {
        public override string Name => Utils.I18n.Get("mod_no_jam");
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable() => Settings.IsNoJamEnabled = true;
        public override void OnDisable() => Settings.IsNoJamEnabled = false;
    }
}