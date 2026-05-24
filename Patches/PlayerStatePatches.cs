using HarmonyLib;
using UnityEngine;
using FullBrightMod.Core;
using KrokoshaCasualtiesMP;
using System;

namespace FullBrightMod.Patches
{
    // ==========================================
    // 🛡️ 反布娃娃 (Anti-Ragdoll) 补丁
    // ==========================================
    [HarmonyPatch(typeof(Body), "Ragdoll")]
    internal static class AntiRagdollPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            // 如果反布娃娃开启，直接 return false 拦截原方法的执行。
            // 这样游戏无论如何都无法剥夺你的站立状态并开启物理模拟了！
            if (Settings.IsAntiRagdollEnabled)
            {
                return false; 
            }
            return true; // 否则正常执行原版布娃娃逻辑
        }
    }
}