using FullBrightMod.Core;
namespace FullBrightMod.Modules {
    public class AutoBolt : ModuleBase {
        public override string Name => Utils.I18n.Get("mod_auto_bolt");
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable() => Settings.IsAutoBoltEnabled = true;
        public override void OnDisable() => Settings.IsAutoBoltEnabled = false;
    }
}