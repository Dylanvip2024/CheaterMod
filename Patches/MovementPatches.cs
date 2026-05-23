using FullBrightMod.Core;
using HarmonyLib;
using UnityEngine;

namespace FullBrightMod.Patches
{
    // =========================================================
    // 跳跃增强 —— 覆写 Body.actualJumpSpeed getter
    // =========================================================
    [HarmonyPatch(typeof(Body), "get_actualJumpSpeed")]
    internal static class JumpBoostPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref float __result)
        {
            if (Settings.IsJumpBoostEnabled && Settings.CustomJumpForce > 0f)
                __result = Settings.CustomJumpForce;
        }
    }

    // =========================================================
    // 实体穿墙 —— Body.FixedUpdate 每帧维持 isTrigger 状态
    //   （防止游戏代码在 Stand/Ragdoll 时重置 col）
    // =========================================================
    [HarmonyPatch(typeof(Body), "FixedUpdate")]
    internal static class NoClipPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Body __instance)
        {
            if (!Settings.IsNoClipEnabled) return;

            // 仅作用于本地玩家，不影响 NPC 和其他玩家
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;

            // 维持穿墙状态（游戏可能在 Stand/Ragdoll 时修改 col）
            if (__instance.col != null && !__instance.col.isTrigger)
                __instance.col.isTrigger = true;

            if (__instance.rb != null)
                __instance.rb.gravityScale = 0f;
        }
    }
}
