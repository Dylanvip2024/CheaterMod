using HarmonyLib;
using UnityEngine;
using FullBrightMod.Core;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// AutoGenerator — 自动摇发电机。
    /// 模拟高速圆周运动，瞬间充满电池。
    /// 参考 AutoBandagePatch 的原理开发。
    /// </summary>

    // ── 状态记录器 ──
    internal static class AutoGeneratorState
    {
        /// <summary>当前累计角度（每帧递增）</summary>
        public static float CurrentAngle;
        /// <summary>本轮是否已充满</summary>
        public static bool IsFinished;
    }

    /// <summary>重置状态</summary>
    [HarmonyPatch(typeof(HandCrankMinigame), "Start")]
    internal static class AutoGeneratorStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            AutoGeneratorState.CurrentAngle = 0f;
            AutoGeneratorState.IsFinished = false;
        }
    }

    /// <summary>更新阶段：强制抓住摇杆手柄</summary>
    [HarmonyPatch(typeof(HandCrankMinigame), "Update")]
    internal static class AutoGeneratorUpdatePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            if (!ModSettings.IsAutoGeneratorEnabled) return;
            if (Minigame.game == null) return;
            if (AutoGeneratorState.IsFinished) return;

            // ★ 检查电池是否已充满（如果 item 已销毁说明用完/充满）
            if (Minigame.game.currentItem == null || !Minigame.game.currentItem)
            {
                AutoGeneratorState.IsFinished = true;
                return;
            }
            // 体力不足则停止
            if (Minigame.game.body != null && Minigame.game.body.stamina < 15f)
            {
                AutoGeneratorState.IsFinished = true;
                return;
            }

            // ★ 强制抓住手柄
            Minigame.game.handStartedClicking = true;
            Minigame.game.handStoppedClicking = false;
        }
    }

    /// <summary>物理更新阶段：高速旋转摇杆</summary>
    [HarmonyPatch(typeof(HandCrankMinigame), "PhysicsUpdate")]
    internal static class AutoGeneratorPhysicsPatch
    {
        [HarmonyPrefix]
        private static void Prefix(HandCrankMinigame __instance, float deltaTime)
        {
            if (!ModSettings.IsAutoGeneratorEnabled) return;
            if (Minigame.game == null) return;
            if (AutoGeneratorState.IsFinished) return;

            // ★ 每帧累加大角度增量（5000 度/秒 ≈ 13.9 转/秒）
            AutoGeneratorState.CurrentAngle += deltaTime * 5000f;

            // ★ 计算圆周坐标（HandCrankMinigame 使用半径为 412f 的约束圆）
            float rad = AutoGeneratorState.CurrentAngle * Mathf.Deg2Rad;
            Minigame.game.handPos = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * 412f;

            // ★ 赋予极高速度确保物理引擎捕获
            Minigame.game.handVelocity = new Vector2(
                Mathf.Cos(rad), -Mathf.Sin(rad)) * 5000f;
        }
    }
}
