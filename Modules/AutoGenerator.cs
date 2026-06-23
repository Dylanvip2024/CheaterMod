using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// AutoGenerator（自动发电）—— 自动完成手摇发电机小游戏。
    /// 通过模拟高速圆周运动瞬间充满电池。
    /// </summary>
    public class AutoGenerator : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_auto_generator") ?? "自动发电";
        public override string Description => "自动高速摇动发电机，瞬间充满电池";
        public override ModuleCategory Category => ModuleCategory.World;

        public override void OnEnable()  => ModSettings.IsAutoGeneratorEnabled = true;
        public override void OnDisable() => ModSettings.IsAutoGeneratorEnabled = false;
    }
}
