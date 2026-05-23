using FullBrightMod.Core;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Patches
{
    // =========================================================
    // Body.DoPickupCheck — 拾取物长手半径完全可调控
    // =========================================================
    [HarmonyPatch(typeof(Body), "DoPickupCheck")]
    internal static class BodyPickupPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Body __instance, Item item, bool noAlerts, ref bool __result)
        {
            if (item == null) return true;

            if (!Settings.IsLongHandsEnabled && !Settings.IsThroughWallEnabled)
                return true;

            bool holding = __instance.HoldingItem(item) 
                || (item.ParentContainer() && __instance.HoldingItem(item.ParentContainer().GetComponent<Item>()));
            if (holding) { __result = true; return false; }

            Vector2 vector = item.transform.parent 
                ? item.transform.position 
                : item.GetComponent<Collider2D>().bounds.center;

            bool flag_blocked = Physics2D.Linecast(__instance.transform.position, vector, LayerMask.GetMask("Ground"));

            float maxRange = Settings.IsLongHandsEnabled ? Settings.CustomPickupRange : 9.0f;
            bool flag_inRange = Vector2.Distance(item.transform.position, __instance.transform.position) < maxRange;

            if (Settings.IsThroughWallEnabled)
                flag_blocked = false;
            else if (flag_blocked && Physics2D.OverlapPoint(vector, LayerMask.GetMask("Ground")))
                flag_blocked = false;

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
    // Util.DoFullInteractionCheck — 长手交互距离由滑块动态接管
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
                distance = Settings.CustomPickupRange;
            }
            else
            {
                distance = 9f;
            }
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

            // 【极其重要】确保只控制本地玩家的肉体
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
