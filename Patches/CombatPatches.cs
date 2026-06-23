using HarmonyLib;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// 近战攻速修改 — 在 Body.Attack() 完成后，将计算出的 attackCooldown 乘以用户设定的倍率。
    /// 倍率 > 1 表示攻击更快（冷却更短），倍率 < 1 表示攻击更慢。
    /// 使用 Postfix 确保在原版公式（含 consciousness/负重/stimulant 修正）执行完毕后再修改。
    /// </summary>
    [HarmonyPatch(typeof(Body), "Attack")]
    internal static class MeleeSpeedPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            if (!ModSettings.IsMeleeSpeedEnabled) return;
            if (ModSettings.MeleeSpeedMultiplier <= 0f) return;

            // 判断是否是本地玩家（只加速自己）
            var pc = PlayerCamera.main;
            if (pc == null || pc.body != __instance) return;

            // 仅在冷却 > 0（即刚完成一次攻击）时修改
            if (__instance.attackCooldown > 0f)
            {
                // 倍率越高冷却越短：cooldown / multiplier
                __instance.attackCooldown /= ModSettings.MeleeSpeedMultiplier;
            }
        }
    }

    /// <summary>
    /// 反地刺 — 在 SpikeStabberScript.OnTriggerEnter2D 执行前拦截，
    /// 当模块启用时直接阻止触发，确保地刺在任何情况下都不会扎到玩家。
    /// 与 AntiSpikes 模块的主动扫描式禁用互补（前置扫描为第一道防线，此处为最后防爆发）。
    /// </summary>
    [HarmonyPatch(typeof(SpikeStabberScript), "OnTriggerEnter2D")]
    internal static class AntiSpikesTriggerPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            // 反地刺模块启用时，直接跳过 OnTriggerEnter2D，阻止地刺触发
            return !ModSettings.IsAntiSpikesEnabled;
        }
    }
}
