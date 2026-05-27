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
    // 速度修改 —— Postfix Body.legSpeedMult 乘上倍率
    // 仅对本地玩家生效，不修改其他 Body 的速度
    // =========================================================
    [HarmonyPatch(typeof(Body), "get_legSpeedMult")]
    internal static class SpeedModifierPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref float __result, Body __instance)
        {
            if (!Settings.IsSpeedModifierEnabled) return;
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;

            __result *= Settings.CustomSpeedMultiplier;
        }
    }

    // =========================================================
    // 空气跳跃 —— Postfix Body.Update 手动监听按键
    // =========================================================
    [HarmonyPatch(typeof(Body), "Update")]
    internal static class AirJumpPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            if (!Settings.IsAirJumpEnabled) return;
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;

            // 监听原版跳跃键
            if (Input.GetKeyDown(KeyBinds.GetBind("jump")))
            {
                // 如果在空中（非 grounded），强行赋予向上的跳跃速度
                if (!__instance.grounded && __instance.standing && !__instance.forceWalk)
                {
                    __instance.rb.velocity = new Vector2(__instance.rb.velocity.x, __instance.actualJumpSpeed);
                    
                    // 模拟特效与消耗
                    __instance.stamina -= 1f;
                    var origColSize = Traverse.Create(__instance).Field("origColSize").GetValue<Vector2>();
                    __instance.CreateCloudBig(__instance.transform.position + Vector3.down * origColSize.y * 0.5f);
                }
            }
        }
    }

    // =========================================================
    // 喷气背包 —— Postfix Body.Update：按住 W/↑ 时施加上升力
    // 模拟原版 jetpack 的 AddForce 效果，绕过装备检查
    // =========================================================
    [HarmonyPatch(typeof(Body), "Update")]
    internal static class JetpackPatch
    {
        private static readonly Traverse _moveDirT =
            Traverse.Create(typeof(Body)).Property("moveDir");

        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            if (!Settings.IsJetpackEnabled) return;
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;
            if (__instance.rb == null || !__instance.conscious) return;
            if (!Input.GetKey(KeyBinds.GetBind("up"))) return;

            Vector2 moveDir = _moveDirT.GetValue<Vector2>();
            float force = 198000f * Time.deltaTime;
            float horizontalForce = 60000f * moveDir.x * Time.deltaTime;

            if (!__instance.standing)
            {
                __instance.rb.MoveRotation(
                    Mathf.LerpAngle(__instance.transform.eulerAngles.z, 0f, Time.deltaTime * 15f));
                __instance.rb.AddForce(
                    __instance.transform.up * force
                    + Vector3.right * horizontalForce);
            }
            else
            {
                __instance.rb.AddForce(
                    Vector3.up * force
                    + Vector3.right * horizontalForce);
            }
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
