using FullBrightMod.Core;
using HarmonyLib;
using UnityEngine;

namespace FullBrightMod.Patches
{
    [HarmonyPatch(typeof(Input))]
    internal static class KeyMapperPatch
    {
        private static int _internalCallDepth;

        private static bool HandleKey(ref bool __result, KeyCode key, System.Func<KeyCode, bool> internalCheck)
        {
            if (_internalCallDepth > 0 || !ModSettings.IsKeyMapperEnabled) return false;

            _internalCallDepth++;
            try
            {
                var list = ModSettings.KeyMappings;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var e = list[i];
                    if (e.Source == KeyCode.None || e.Target == KeyCode.None) continue;

                    // 正向：游戏查�?Target �?玩家按了 Source
                    if (key == e.Target && internalCheck(e.Source))
                    { __result = true; return true; }

                    // 反向：双向模式下游戏查询 Source �?玩家按了 Target
                    if (e.Bidirectional && key == e.Source && internalCheck(e.Target))
                    { __result = true; return true; }
                }
            }
            finally { _internalCallDepth--; }
            return false;
        }

        [HarmonyPatch("GetKey", typeof(KeyCode))]
        [HarmonyPrefix]
        private static bool GetKey_Prefix(KeyCode key, ref bool __result)
        {
            return !HandleKey(ref __result, key, k => Input.GetKey(k));
        }

        [HarmonyPatch("GetKeyDown", typeof(KeyCode))]
        [HarmonyPrefix]
        private static bool GetKeyDown_Prefix(KeyCode key, ref bool __result)
        {
            return !HandleKey(ref __result, key, k => Input.GetKeyDown(k));
        }

        [HarmonyPatch("GetKeyUp", typeof(KeyCode))]
        [HarmonyPrefix]
        private static bool GetKeyUp_Prefix(KeyCode key, ref bool __result)
        {
            return !HandleKey(ref __result, key, k => Input.GetKeyUp(k));
        }
    }
}
