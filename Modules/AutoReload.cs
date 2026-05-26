using FullBrightMod.Core;
namespace FullBrightMod.Modules {
    public class AutoReload : ModuleBase {
        public override string Name => Utils.I18n.Get("mod_auto_reload");
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable() => Settings.IsAutoReloadEnabled = true;
        public override void OnDisable() => Settings.IsAutoReloadEnabled = false;
    }
}