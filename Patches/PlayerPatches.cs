using System.Collections;
using FullBrightMod.Core;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Patches
{
    // =========================================================
    // TPHelper — 防卡墙安全坐标算法 (协程方案用)
    // =========================================================
    internal static class TPHelper
    {
        /// <summary>
        /// 计算不卡墙的安全瞬移坐标。
        /// 从目标往外推 interactRange 距离，步进检测障碍物。
        /// </summary>
        public static Vector3 GetSafeTeleportPosition(Vector3 myPos, Vector3 targetPos, float interactRange = 2f)
        {
            Vector2 dir = ((Vector2)(myPos - targetPos)).normalized;
            if (dir == Vector2.zero) dir = Vector2.right;

            float stepDistance = 0.3f;
            int groundMask = LayerMask.GetMask("Ground");

            for (float dist = interactRange; dist >= 0.2f; dist -= stepDistance)
            {
                Vector2 testPos = (Vector2)targetPos + dir * dist;
                Collider2D hit = Physics2D.OverlapCircle(testPos, 0.35f, groundMask);
                if (hit == null)
                {
                    RaycastHit2D ray = Physics2D.Linecast((Vector2)targetPos, testPos, groundMask);
                    if (!ray) return (Vector3)testPos;
                }
            }

            return targetPos + (Vector3)(dir * interactRange);
        }
    }

    // =========================================================
    // Body.DoPickupCheck — 本地距离覆盖
    // =========================================================
    [HarmonyPatch(typeof(Body), "DoPickupCheck")]
    internal static class BodyPickupPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Body __instance, Item item, bool noAlerts, ref bool __result)
        {
            if (item == null) return true;
            if (!Settings.IsLongHandsEnabled && !Settings.IsThroughWallEnabled) return true;

            bool holding = __instance.HoldingItem(item)
                || (item.ParentContainer() && __instance.HoldingItem(item.ParentContainer().GetComponent<Item>()));
            if (holding) { __result = true; return false; }

            Vector2 vector = item.transform.parent
                ? item.transform.position
                : item.GetComponent<Collider2D>().bounds.center;

            bool flag_blocked = Physics2D.Linecast(__instance.transform.position, vector, LayerMask.GetMask("Ground"));
            float maxRange = Settings.IsLongHandsEnabled ? Settings.CustomPickupRange : 9.0f;
            if (Settings.IsLongHandsTPModeEnabled) maxRange = 9999f;
            bool flag_inRange = Vector2.Distance(item.transform.position, __instance.transform.position) < maxRange;

            if (Settings.IsThroughWallEnabled) flag_blocked = false;
            else if (flag_blocked && Physics2D.OverlapPoint(vector, LayerMask.GetMask("Ground"))) flag_blocked = false;

            if (!noAlerts)
            {
                if (flag_blocked && !Settings.IsThroughWallEnabled)
                    PlayerCamera.main.DoAlert("前方有障碍物遮挡", false);
                else if (!flag_inRange && !Settings.IsLongHandsEnabled)
                    PlayerCamera.main.DoAlert("物品离得太远了", false);
            }

            __result = !flag_blocked && flag_inRange;
            return false;
        }
    }

    // =========================================================
    // Util.DoFullInteractionCheck — 长手交互距离动态接管
    // =========================================================
    [HarmonyPatch(typeof(KrokoshaCasualtiesUtils.Util), "DoFullInteractionCheck")]
    internal static class GlobalInteractionPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref bool check_obstruction, ref float distance)
        {
            if (Settings.IsLongHandsEnabled)
            {
                check_obstruction = false;
                distance = Settings.IsLongHandsTPModeEnabled ? 9999f : Settings.CustomPickupRange;
            }
            else { distance = 9f; }
        }
    }

    // =========================================================
    // UIInGame._UpdateCheckInteractKeybinds_CheckDistNShi
    //     ★ 所有快捷键交互的硬性距离拦截 (9f) ★
    // =========================================================
    [HarmonyPatch(typeof(UIInGame), "_UpdateCheckInteractKeybinds_CheckDistNShi")]
    internal static class InteractKeybindDistancePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (!Settings.IsLongHandsTPModeEnabled || !Settings.IsLongHandsEnabled)
                return true;
            __result = true;
            return false;
        }
    }

    // =========================================================
    // UIInGame.StartPlayerInteractionMenu — 破解远程打开的距离限制
    //     临时放大 max_player_interaction_distance → 原方法完整运行
    // =========================================================
    [HarmonyPatch(typeof(UIInGame), "StartPlayerInteractionMenu")]
    internal static class InteractionMenuDistancePatch
    {
        private static float _savedDist;
        private static bool _patched;

        [HarmonyPrefix]
        private static void Prefix()
        {
            _patched = false;
            if (!Settings.IsLongHandsTPModeEnabled || !Settings.IsLongHandsEnabled) return;
            _savedDist = SharedMain.max_player_interaction_distance;
            SharedMain.max_player_interaction_distance = 9999f;
            _patched = true;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!_patched) return;
            SharedMain.max_player_interaction_distance = _savedDist;
        }
    }

    // =========================================================
    // UIInGame.StopPlayerInteractionMenu — 破解 Update 中的持续关闭
    //     UIInGame.Update() 每帧 !KM.dist2dsqrcheck(9f) → 强制关闭
    //     TP 模式下若正在检视远程玩家 → 拒绝关闭
    // =========================================================
    [HarmonyPatch(typeof(UIInGame), "StopPlayerInteractionMenu")]
    internal static class InteractionMenuPersistPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!Settings.IsLongHandsTPModeEnabled || !Settings.IsLongHandsEnabled)
                return true;

            // ESC 按下 → 玩家手动关闭 → 允许
            if (Input.GetKeyDown(KeyCode.Escape))
                return true;

            // 其他所有情况（Update 自动关闭 / 代码触发的关闭）→ 拦截
            return false;
        }
    }

    // =========================================================
    // Util.AccurateRaycastInteractionCheckObstruction
    //     "没有直接视野" 的核心检测方法。TP 模式下强制返回 false（无遮挡）
    // =========================================================
    [HarmonyPatch(typeof(KrokoshaCasualtiesUtils.Util), "AccurateRaycastInteractionCheckObstruction")]
    internal static class ObstructionBypassPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (!Settings.IsLongHandsTPModeEnabled || !Settings.IsLongHandsEnabled)
                return true;
            __result = true;   // true = 无遮挡，交互允许
            return false;
        }
    }

    // =========================================================
    // Util.QuickRaycastInteractionCheck — 快捷键交互的遮挡检测
    //     _UpdateCheckInteractKeybinds_CheckDistNShi 中调用
    //     TP 模式下强制返回 true（无障碍）
    // =========================================================
    [HarmonyPatch(typeof(KrokoshaCasualtiesUtils.Util), "QuickRaycastInteractionCheck")]
    internal static class QuickRaycastBypassPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (!Settings.IsLongHandsTPModeEnabled || !Settings.IsLongHandsEnabled)
                return true;
            __result = true;  // true = 无障碍，交互允许
            return false;
        }
    }

    // =========================================================
    // PlayerCamera.ToggleWoundView — 终极护城河：阻止远程面板关闭
    //     MP mod 的 Prefix 会在关闭前将 WoundView.view.body 切换为本地玩家
    //     HigherThanNormal 确保本 Patch 先于 MP mod 执行，在 Body 被切换前拦截
    // =========================================================
    [HarmonyPatch(typeof(PlayerCamera), "ToggleWoundView")]
    internal static class WoundViewKeepAlivePatch
    {
        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyPrefix]
        private static bool Prefix(PlayerCamera __instance)
        {
            if (!Settings.IsLongHandsTPModeEnabled || !Settings.IsLongHandsEnabled)
                return true;
            if (__instance.woundView == null) return true;

            // woundView.activeSelf == true → 本次调用是「关闭」操作
            if (!__instance.woundView.activeSelf) return true;

            // 🎯 【核心修复】：如果你按下了 ESC，直接放行，允许正常关闭！
            // 这里用 GetKey 比 GetKeyDown 更稳，防止帧数波动导致漏判
            if (Input.GetKey(KeyCode.Escape)) 
                return true;

            // WoundView.view.body != player → 正在检视远程玩家
            if (WoundView.view == null) return true;
            if (WoundView.view.body == __instance.body) return true;

            // 护城河：拒绝系统距离检测导致的自动关闭
            return false;
        }
    }

    // =========================================================
    // Body.PickUpItem — 协程 TP 拾取 (0.12s 延迟防 Rubber-banding)
    // =========================================================
    [HarmonyPatch(typeof(Body), "PickUpItem")]
    internal static class PickUpItemTPPatch
    {
        public static bool IsBlinking;

        [HarmonyPrefix]
        private static bool Prefix(Body __instance, Item item, int slot, bool force)
        {
            if (item == null) return true;
            if (!Settings.IsLongHandsTPModeEnabled || !Settings.IsLongHandsEnabled) return true;
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return true;

            // 协程内部的重入调用 → 放行
            if (IsBlinking) return true;

            float dist = Vector2.Distance(item.transform.position, __instance.transform.position);
            if (dist <= Settings.CustomPickupRange) return true;

            // 拦截，转入协程
            FullBrightPlugin.Instance.StartCoroutine(PickupBlinkCoroutine(__instance, item, slot, force));
            return false;
        }

        private static IEnumerator PickupBlinkCoroutine(Body body, Item item, int slot, bool force)
        {
            IsBlinking = true;
            Vector3 originalPos = body.transform.position;

            // 1. 瞬移到安全坐标 + 发包
            Vector3 safePos = TPHelper.GetSafeTeleportPosition(originalPos, item.transform.position, 1.5f);
            body.transform.position = safePos;
            try { ClientMain.Client_SendCharacterSyncPacket(); } catch { }

            // 2. 等待服务端接受新坐标
            yield return new WaitForSeconds(0.12f);

            // 3. 执行真实拾取 (IsBlinking=true → Prefix 放行)
            try { body.PickUpItem(item, slot, force); } catch { }

            // 4. 等待拾取包被服务端处理
            yield return new WaitForSeconds(0.12f);

            // 5. 移回原位 + 发包
            body.transform.position = originalPos;
            try { ClientMain.Client_SendCharacterSyncPacket(); } catch { }

            // 6. 缓冲一帧防闪烁
            yield return null;
            IsBlinking = false;
        }
    }

    // =========================================================
    // PlayerCamera.LateUpdate — 自由视角 + 视距拉远
    // =========================================================
    [HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
    internal static class PlayerCameraLateUpdatePatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerCamera __instance)
        {
            if (Camera.main == null) return;

            if (Settings.IsCameraZoomEnabled)
            {
                MonoBehaviour ppc = Camera.main.GetComponent("PixelPerfectCamera") as MonoBehaviour;
                if (ppc != null && ppc.enabled) ppc.enabled = false;
                Camera.main.orthographicSize = Settings.CustomCameraSize;
            }

            if (Settings.IsFreecamEnabled)
            {
                Vector3 newPos = Settings.FreecamPosition;
                newPos.z = Camera.main.transform.position.z;
                Camera.main.transform.position = newPos;
            }
        }
    }

    // =========================================================
    // Body.FixedUpdate — 物理引擎接管（飞行）
    // =========================================================
    [HarmonyPatch(typeof(Body), "FixedUpdate")]
    internal static class FlightPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            if (!Settings.IsFlightEnabled) return;
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;

            __instance.rb.gravityScale = 0f;

            float moveX = 0f, moveY = 0f;
            if (Input.GetKey(KeyCode.W)) moveY += 1f;
            if (Input.GetKey(KeyCode.S)) moveY -= 1f;
            if (Input.GetKey(KeyCode.A)) moveX -= 1f;
            if (Input.GetKey(KeyCode.D)) moveX += 1f;

            float speed = 12f;
            if (Input.GetKey(KeyCode.LeftShift)) speed *= 2.5f;

            __instance.rb.velocity = new Vector2(moveX, moveY).normalized * speed;
        }
    }
}
