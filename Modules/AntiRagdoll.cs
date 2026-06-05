using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    public class AntiRagdoll : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_antiragdoll") ?? "反布娃娃";
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable() => ModSettings.IsAntiRagdollEnabled = true;
        public override void OnDisable() => ModSettings.IsAntiRagdollEnabled = false;
    }
}