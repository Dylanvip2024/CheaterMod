using FullBrightMod.Core;
using FullBrightMod.Modules;
using HarmonyLib;

namespace FullBrightMod.Patches
{
    [HarmonyPatch(typeof(Body), "UseItem")]
    internal static class DynamiteUseItemPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Body __instance, Item item)
        {
            if (!ModSettings.IsDynamiteCountdownEnabled) return;
            if (item == null || item.id != "dynamite") return;
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;
            CustomItemBehaviour component = item.GetComponent<CustomItemBehaviour>();
            if (component != null)
                DynamiteCountdown.Track(component);
        }
    }

    [HarmonyPatch(typeof(CustomItemBehaviour), "Update")]
    internal static class DynamiteUpdatePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CustomItemBehaviour __instance)
        {
            if (!ModSettings.IsDynamiteCountdownEnabled) return;
            if (__instance == null) return;
            Item item = Traverse.Create(__instance).Field("item").GetValue<Item>();
            if (item == null || item.id != "dynamite") return;
            object[] data = __instance.data;
            if (data != null && data.Length != 0 && data[0] != null)
                DynamiteCountdown.Track(__instance);
            if (__instance.state != 0)
                DynamiteCountdown.Track(__instance);
        }
    }

    [HarmonyPatch(typeof(CustomItemBehaviour), "DynamiteExplode")]
    internal static class DynamiteExplodePatch
    {
        [HarmonyPrefix]
        private static void Prefix(CustomItemBehaviour __instance)
        {
            if (!ModSettings.IsDynamiteCountdownEnabled) return;
            DynamiteCountdown.Untrack(__instance);
        }
    }
}

