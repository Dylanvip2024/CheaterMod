using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// AntiSolarFlare（反耀斑）—— 开启后无视太阳耀斑事件，强制脑芯片保持激活。
    /// 通过 Patch WorldGeneration.get_unchipped() 实现。
    /// </summary>
    public class AntiSolarFlare : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_anti_solar_flare") ?? "反耀斑";
        public override string Description => "无视太阳耀斑，保持脑芯片正常工作";
        public override ModuleCategory Category => ModuleCategory.Render;

        public override void OnEnable()  => ModSettings.IsAntiSolarFlareEnabled = true;
        public override void OnDisable() => ModSettings.IsAntiSolarFlareEnabled = false;
    }
}
