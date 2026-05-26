using HarmonyLib;
using UnityEngine;
using FullBrightMod.Core;

namespace FullBrightMod.Patches
{
    // ==========================================
    // 🛡️ 反布娃娃 (Anti-Ragdoll) 针对性判定补丁
    // ==========================================
    [HarmonyPatch(typeof(Body), "Ragdoll")]
    internal static class AntiRagdollPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Body __instance)
        {
            // 如果反布娃娃功能关闭，一律放行
            if (!Settings.IsAntiRagdollEnabled) return true;

            // ✨【核心身份判定过滤】✨
            // 检查当前触发布娃娃的肉体 (Body)，是不是本地玩家的真实主肉体
            if (PlayerCamera.main != null && PlayerCamera.main.body == __instance)
            {
                // 如果是本地玩家自己，直接拦截，拒绝进入布娃娃，防止跌落物理失控
                return false; 
            }

            // 如果是其他玩家、队友、或者是野外的怪物，放行执行，正常在你的屏幕上渲染他们的布娃娃姿态！
            return true; 
        }
    }
}