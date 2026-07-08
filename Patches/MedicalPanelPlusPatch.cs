using FullBrightMod.Core;
using HarmonyLib;
using UnityEngine;

namespace FullBrightMod.Patches
{
    [HarmonyPatch(typeof(PlayerCamera), "ToggleWoundView")]
    internal static class MedicalPanelOpenPatch
    {
        [HarmonyPrefix]
        private static void Prefix(PlayerCamera __instance, out float __state)
        {
            __state = __instance.body.consciousness;
            if (ModSettings.IsMedicalPanelPlusEnabled && ModSettings.MedicalPanelAlwaysAccessible && !__instance.body.conscious)
                __instance.body.consciousness = 100f;
        }

        [HarmonyPostfix]
        private static void Postfix(PlayerCamera __instance, float __state)
        {
            if (ModSettings.IsMedicalPanelPlusEnabled && ModSettings.MedicalPanelAlwaysAccessible)
                __instance.body.consciousness = __state;
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "OnBecameUnconscious")]
    internal static class MedicalPanelNoClosePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(PlayerCamera __instance)
        {
            if (!ModSettings.IsMedicalPanelPlusEnabled || !ModSettings.MedicalPanelAlwaysAccessible) return true;
            AccessTools.Field(typeof(PlayerCamera), "wasUnconscious").SetValue(__instance, true);
            return false;
        }
    }

    [HarmonyPatch(typeof(WoundView), "UpdateView")]
    internal static class InfectionAlwaysPatch
    {
        [HarmonyPostfix]
        private static void Postfix(WoundView __instance)
        {
            if (!ModSettings.IsMedicalPanelPlusEnabled || !ModSettings.MedicalPanelInfectionAlways) return;
            if (__instance.body == null || __instance.infecText == null) return;
            int limbIndex = (int)AccessTools.Field(typeof(WoundView), "limbLookingAt").GetValue(__instance);
            if (limbIndex < 0 || limbIndex >= __instance.body.limbs.Length) return;
            Limb limb = __instance.body.limbs[limbIndex];
            if (limb == null) return;
            int infecVal = Mathf.RoundToInt(limb.infectionAmount);
            __instance.infecText.text = infecVal.ToString();
            __instance.infecText.color = (infecVal >= 25) ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}

