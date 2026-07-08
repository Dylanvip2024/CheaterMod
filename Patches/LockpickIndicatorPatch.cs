using FullBrightMod.Core;
using FullBrightMod.Modules;
using HarmonyLib;
using UnityEngine;

namespace FullBrightMod.Patches
{
    [HarmonyPatch(typeof(LockpingMinigame), "Start")]
    internal static class LockpickIndicatorStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(LockpingMinigame __instance)
        {
            if (!ModSettings.IsLockpickIndicatorEnabled) return;
            if (__instance != null)
                LockpickIndicator.ActiveMinigame = __instance;
        }
    }

    [HarmonyPatch(typeof(LockpingMinigame), "Update")]
    internal static class LockpickIndicatorUpdatePatch
    {
        [HarmonyPostfix]
        private static void Postfix(LockpingMinigame __instance)
        {
            if (!ModSettings.IsLockpickIndicatorEnabled) return;
            if (__instance != null)
                LockpickIndicator.ActiveMinigame = __instance;
        }
    }
}

