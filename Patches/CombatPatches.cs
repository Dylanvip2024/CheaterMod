using HarmonyLib;
using FullBrightMod.Core;

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
}
