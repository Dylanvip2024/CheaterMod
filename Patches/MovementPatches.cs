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
            if (ModSettings.IsJumpBoostEnabled && ModSettings.CustomJumpForce > 0f)
                __result = ModSettings.CustomJumpForce;
        }
    }

    // =========================================================
    // 速度修改 —— FixedUpdate Postfix 直接覆写水平速度
    //     不再 Hook legSpeedMult（会污染 actualJumpSpeed 跳跃力）
    //     仅修改 rb.velocity.x，严格保留 rb.velocity.y
    // =========================================================
    [HarmonyPatch(typeof(Body), "FixedUpdate")]
    internal static class SpeedModifierPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            if (!ModSettings.IsSpeedModifierEnabled) return;
            
            // ★ 修复冲突：如果开启了飞行，彻底交出速度控制权，防止相互覆盖
            if (ModSettings.IsFlightEnabled) return;

            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;
            if (!__instance.standing || !__instance.conscious || __instance.rb == null) return;

            float originalMaxSpeed = __instance.actualMaxSpeed;

            // ★ 安全反射：正确通过 Traverse 获取当前 __instance 实例的 moveDir 字段
            //    7.0.1 已验证字段名未变 (public Vector2 moveDir)，保留盲测回退
            Vector2 moveDir = Vector2.zero;
            try
            {
                moveDir = Traverse.Create(__instance).Field("moveDir").GetValue<Vector2>();
            }
            catch
            {
                // 盲测回退：遍历实例字段，按类型 Vector2 匹配含 "move" 的字段
                try
                {
                    foreach (var f in __instance.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        if (f.FieldType == typeof(Vector2) && f.Name.ToLower().Contains("move"))
                        {
                            moveDir = (Vector2)f.GetValue(__instance);
                            break;
                        }
                    }
                }
                catch { }
            }
            
            float inputX = moveDir.x;
            
            // 兜底方案：如果原版反射获取失败，直接读取游戏按键
            if (inputX == 0f)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyBinds.GetBind("left"))) inputX = -1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyBinds.GetBind("right"))) inputX = 1f;
            }

            if (inputX != 0f)
            {
                float targetSpeed = originalMaxSpeed * ModSettings.CustomSpeedMultiplier;
                __instance.rb.velocity = new Vector2(
                    targetSpeed * Mathf.Sign(inputX),
                    __instance.rb.velocity.y);  // 严格保留 Y 轴重力/跳跃，防止干扰
            }
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
            if (!ModSettings.IsAirJumpEnabled) return;
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
                    // ★ 7.0.1 安全反射：原名 + 类型盲测回退
                    Vector2 origColSize = Vector2.one;
                    try
                    {
                        origColSize = Traverse.Create(__instance).Field("origColSize").GetValue<Vector2>();
                    }
                    catch
                    {
                        try
                        {
                            foreach (var f in __instance.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                            {
                                if (f.FieldType == typeof(Vector2) && f.Name.ToLower().Contains("col") && f.Name.ToLower().Contains("size"))
                                {
                                    origColSize = (Vector2)f.GetValue(__instance);
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
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
            Traverse.Create(typeof(Body)).Field("moveDir");

        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            if (!ModSettings.IsJetpackEnabled) return;
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
            if (!ModSettings.IsNoClipEnabled) return;

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
