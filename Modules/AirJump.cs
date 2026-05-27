using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class AirJump : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_airjump");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => Settings.IsAirJumpEnabled = true;
        public override void OnDisable() => Settings.IsAirJumpEnabled = false;
    }

    public class Jetpack : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_jetpack");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => Settings.IsJetpackEnabled = true;
        public override void OnDisable() => Settings.IsJetpackEnabled = false;
    }
}
