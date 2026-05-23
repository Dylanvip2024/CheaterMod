using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class NoClip : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_noclip");
        public override ModuleCategory Category => ModuleCategory.Movement;

        public override void OnEnable()
        {
            Settings.IsNoClipEnabled = true;
            // 立即对本地玩家应用穿墙
            ApplyNoClip(true);
        }

        public override void OnDisable()
        {
            Settings.IsNoClipEnabled = false;
            // 恢复碰撞
            ApplyNoClip(false);
        }

        /// <summary>对本地玩家的碰撞体进行触发器/物理切换</summary>
        private static void ApplyNoClip(bool enable)
        {
            if (PlayerCamera.main == null || PlayerCamera.main.body == null) return;
            var col = PlayerCamera.main.body.col;
            if (col == null) return;

            col.isTrigger = enable;

            // 配合：穿墙时关闭重力防止掉出地图底部
            if (enable)
                PlayerCamera.main.body.rb.gravityScale = 0f;
        }
    }
}
