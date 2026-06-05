using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class AirJump : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_airjump");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => ModSettings.IsAirJumpEnabled = true;
        public override void OnDisable() => ModSettings.IsAirJumpEnabled = false;
    }

    public class Jetpack : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_jetpack");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => ModSettings.IsJetpackEnabled = true;
        public override void OnDisable() => ModSettings.IsJetpackEnabled = false;
    }
}
