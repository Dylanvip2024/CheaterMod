using FullBrightMod.Core;
using HarmonyLib;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// 隐藏 CheaterMod GUID —— 当 HideCheaterModGUID 开启时，
    /// 从联机模组的 Mod 列表中移除 CheaterMod 的 GUID，
    /// 避免在大厅公示。
    /// </summary>
    [HarmonyPatch(typeof(KrokoshaCasualtiesMP.KrokoshaScavMultiplayer), "GetModListGUIDs")]
    internal static class HideCheaterModGUIDPatch
    {
        private const string CHEATERMOD_GUID = "com.mod.casualties.cheatermod";

        [HarmonyPostfix]
        private static void Postfix(ref string[] __result)
        {
            if (!GlobalSettings.HideCheaterModGUID) return;
            if (__result == null || __result.Length == 0) return;

            __result = System.Array.FindAll(__result, g => g != CHEATERMOD_GUID);
        }
    }
}
