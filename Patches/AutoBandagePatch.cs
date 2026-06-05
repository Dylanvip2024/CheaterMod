using HarmonyLib;
using UnityEngine;
using FullBrightMod.Core;

namespace FullBrightMod.Patches
{
    // ==========================================
    // 状态记录器
    // ==========================================
    internal static class AutoBandageState
    {
        public static float GrabTimer = 0f;
        public static bool IsFinished = false; // 记录是否已经止血
    }

    [HarmonyPatch(typeof(BandageMinigame), "Start")]
    internal static class AutoBandageStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            // 每次打开小游戏，重置计时器和完成状态
            AutoBandageState.GrabTimer = 0f;
            AutoBandageState.IsFinished = false;
        }
    }

    [HarmonyPatch(typeof(BandageMinigame), "Update")]
    internal static class AutoBandageUpdatePatch
    {
        [HarmonyPrefix]
        private static void Prefix(BandageMinigame __instance)
        {
            if (!ModSettings.IsAutoBandageEnabled) return;
            if (Minigame.game == null) return;

            // ★ 修复核心：如果已经完成止血，彻底放权，允许用户用真实的鼠标接管后续操作（如涂抹瘀伤药）
            if (AutoBandageState.IsFinished) return;

            // ==========================================
            // 核心刹车逻辑：底层网络流血数据判定
            // ==========================================
            // 直接读取该肢体(Limb)的服务器同步流血量。
            // 只要流血量小于或等于0.05，代表服务器已经判定你完全止血！
            if (__instance.limb != null && __instance.limb.bleedAmount <= 0.05f)
            {
                // 标记为完成
                AutoBandageState.IsFinished = true;

                // 模拟玩家松开鼠标左键（仅执行一次），防止状态粘连
                Minigame.game.handStartedClicking = false;
                Minigame.game.handStoppedClicking = true;

                // ★ 去除了强制关闭界面的代码，直接 Return 将控制权还给玩家
                return; 
            }

            // 如果没止血，继续强行接管输入
            Minigame.game.handStartedClicking = true;
            Minigame.game.handStoppedClicking = false;

            float rad = __instance.bandageAngle * 0.017453292f;
            Minigame.game.handPos = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * 260f;
        }
    }

    [HarmonyPatch(typeof(BandageMinigame), "PhysicsUpdate")]
    internal static class AutoBandagePhysicsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(BandageMinigame __instance, float deltaTime)
        {
            if (!ModSettings.IsAutoBandageEnabled) return;
            if (Minigame.game == null) return;

            // 如果已经检测到止血完成，直接掐断物理旋转引擎
            if (AutoBandageState.IsFinished) return;

            AutoBandageState.GrabTimer += deltaTime;
            float targetAngle;

            if (AutoBandageState.GrabTimer < 0.5f)
            {
                targetAngle = __instance.bandageAngle;
            }
            else
            {
                targetAngle = __instance.bandageAngle + 6f;
            }

            float rad = targetAngle * 0.017453292f;
            Minigame.game.handPos = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * 390f;
            Minigame.game.handVelocity = Vector2.zero;
        }
    }
}