using System.Collections.Generic;
using FullBrightMod.Core;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FullBrightMod.Patches
{
    // =========================================================
    // Chat.LogMessage — 自动翻译
    // =========================================================
    [HarmonyPatch(typeof(Chat), "LogMessage")]
    internal static class ChatTranslatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string plrname, ref string msg, ref bool richtext)
        {
            if (!Settings.IsAutoTranslateEnabled) return true;
            if (msg.Contains("[译]")) return true;

            FullBrightPlugin.Instance?.StartTranslate(plrname, msg, richtext);
            return true;
        }
    }

    // =========================================================
    // KeypadMinigame.Update — 密码锁爆破
    // =========================================================
    [HarmonyPatch(typeof(KeypadMinigame), "Update")]
    internal static class KeypadMinigamePatch
    {
        private static float _actionTimer;
        private static int _autoKeypadState;
        private static int _targetIndex;
        private static KeypadMinigame _currentMinigame;
        private static float _guiOpenTime;

        [HarmonyPrefix]
        private static void Prefix(KeypadMinigame __instance, List<RaycastResult> uiCasts)
        {
            if (!Settings.IsAutoUnlockEnabled) return;

            if (_currentMinigame != __instance)
            {
                _currentMinigame = __instance;
                _guiOpenTime = Time.time;
                _autoKeypadState = 0;
                _targetIndex = 0;
            }

            if (Time.time - _guiOpenTime < 0.5f) return;
            if (string.IsNullOrEmpty(__instance.match)) return;

            if (__instance.current.Length != _targetIndex)
            {
                _targetIndex = __instance.current.Length;
                _autoKeypadState = 0;
            }

            if (_targetIndex >= __instance.match.Length) return;

            char nextChar = __instance.match[_targetIndex];
            GameObject targetBtn = null;
            foreach (GameObject btn in __instance.inputButtons)
            {
                if (btn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text == nextChar.ToString())
                {
                    targetBtn = btn;
                    break;
                }
            }
            if (targetBtn == null) return;

            switch (_autoKeypadState)
            {
                case 0:
                    _actionTimer = Time.time;
                    _autoKeypadState = 1;
                    break;
                case 1:
                    Minigame.game.handPos = Vector2.Lerp(Minigame.game.handPos, targetBtn.transform.position, Time.deltaTime * 20f);
                    if (Time.time - _actionTimer > 0.2f)
                    {
                        RaycastResult fakeHit = new RaycastResult { gameObject = targetBtn };
                        uiCasts.Insert(0, fakeHit);
                        Minigame.game.handStartedClicking = true;
                        _autoKeypadState = 2;
                    }
                    break;
                case 2:
                    Minigame.game.handStartedClicking = false;
                    break;
            }
        }
    }

    // =========================================================
    // LockpingMinigame.Update — 撬锁秒开
    // =========================================================
    [HarmonyPatch(typeof(LockpingMinigame), "Update")]
    internal static class LockpingMinigamePatch
    {
        [HarmonyPrefix]
        private static void Prefix(LockpingMinigame __instance)
        {
            if (!Settings.IsAutoUnlockEnabled) return;

            float perfectAngleRad = __instance.correctAngle * Mathf.Deg2Rad;
            Vector2 perfectPos = new Vector2(Mathf.Cos(perfectAngleRad), Mathf.Sin(perfectAngleRad)) * 220f;

            Minigame.game.handPos = perfectPos;
            Minigame.game.handStartedClicking = true;
            Minigame.game.handStoppedClicking = false;
            __instance.clickingInside = true;
            __instance.lockProgress += Time.deltaTime * 2.5f;
        }
    }

    // =========================================================
    // 万事通 — 配方与介绍可见性劫持
    // =========================================================
    [HarmonyPatch]
    internal static class MastermindPatch
    {
        [HarmonyTargetMethods]
        static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Recipe), "get_visible");
            yield return AccessTools.Method(typeof(Recognition), "get_recognizable");
        }

        [HarmonyPostfix]
        static void Postfix(ref bool __result)
        {
            if (Settings.IsIQ250Enabled) __result = true;
        }
    }
}
