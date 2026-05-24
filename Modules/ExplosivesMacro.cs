using FullBrightMod.Core;

namespace FullBrightMod.Modules
{
    public class ExplosivesMacro : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_explosives_macro") ?? "一键引爆 (宏)";
        public override ModuleCategory Category => ModuleCategory.Misc;

        public override void OnEnable() => Settings.IsExplosivesMacroEnabled = true;
        public override void OnDisable() => Settings.IsExplosivesMacroEnabled = false;

        // 重写基类的 OnUpdate
        public override void OnUpdate()
        {
            // 同步底层补丁的执行完毕状态
            if (!Settings.IsExplosivesMacroEnabled && this.Enabled)
            {
                this.Enabled = false;
            }
        }
    }
}