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
    // Chat.LogMessage — 自动翻译 (接收端)
    // =========================================================
    [HarmonyPatch(typeof(Chat), "LogMessage")]
    internal static class ChatTranslatePatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string plrname, string msg, bool richtext)
        {
            if (!Settings.IsAutoTranslateEnabled && !Settings.IsTwoWayTranslationEnabled) return true;

            // 拦截空消息、或是我们自己发出的 [译] 消息，防止死循环无限翻译
            if (string.IsNullOrEmpty(msg) || msg.Contains("[译]")) return true;

            // 过滤系统消息（防止空指针异常）
            if (plrname != null && (plrname.Contains("*SYSTEM*") || plrname.Contains("*SERVER*"))) return true;

            // ★ 修复点 2：过滤自己刚刚发出的翻译消息，防止自己的外语又被机翻回中文
            if (!string.IsNullOrEmpty(OutgoingChatPatch.LastSentTranslated) && msg.Contains(OutgoingChatPatch.LastSentTranslated))
            {
                return true;
            }

            FullBrightPlugin.Instance?.StartTranslate(plrname ?? "", msg, richtext);
            return true;
        }
    }

    // =========================================================
    // Chat.Client_ChatMessageRecieve — 自动翻译 (接收端 - 其他玩家的带头像消息)
    //
    // 源码分析：联机模式下，其他玩家的聊天消息通过 Client_ChatMessageRecieve
    // （网络消息 30098 的回调）直接入队到 CHAT_LOG，完全绕过了 LogMessage。
    // 消息格式：[byte type] [uint senderId] [string tag] [string msg]
    // 本 Postfix 在消息入队后读取 CHAT_LOG 最新条目，提取 sender 和 msg 进行翻译。
    // =========================================================
    [HarmonyPatch(typeof(Chat), "Client_ChatMessageRecieve")]
    internal static class ChatPlayerMessagePatch
    {
        // CHAT_LOG 是 private static MaxCapacityQueue<ChatMsgContainer>
        private static readonly Traverse _chatLogTraverse = Traverse.Create(typeof(Chat)).Field("CHAT_LOG");

        /// <summary>从 MaxCapacityQueue 中读取最新一条 ChatMsgContainer</summary>
        private static Chat.ChatMsgContainer GetLastMessage()
        {
            try
            {
                var queue = _chatLogTraverse.GetValue();
                if (queue == null) return default;

                // JustGiveTheQueue() 返回 IEnumerable<ChatMsgContainer>
                var traverse = Traverse.Create(queue);
                var enumerable = traverse.Method("JustGiveTheQueue").GetValue<System.Collections.IEnumerable>();
                if (enumerable == null) return default;

                Chat.ChatMsgContainer last = default;
                foreach (var item in enumerable)
                {
                    if (item is Chat.ChatMsgContainer msg)
                        last = msg;
                }
                return last;
            }
            catch
            {
                return default;
            }
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!Settings.IsAutoTranslateEnabled && !Settings.IsTwoWayTranslationEnabled) return;

            var lastMsg = GetLastMessage();
            if (lastMsg.msg == null || lastMsg.msg.Contains("[译]")) return;

            // ★ 过滤自己刚发出的翻译消息，防止服务端回显导致重复翻译
            if (!string.IsNullOrEmpty(OutgoingChatPatch.LastSentTranslated) && lastMsg.msg.Contains(OutgoingChatPatch.LastSentTranslated))
                return;

            string sender = lastMsg.plr != null ? lastMsg.plr.playername : lastMsg.name;
            string msg = lastMsg.msg;

            if (string.IsNullOrEmpty(msg)) return;
            if (sender != null && (sender.Contains("*SYSTEM*") || sender.Contains("*SERVER*"))) return;

            FullBrightPlugin.Instance?.StartTranslate(sender ?? "", msg, lastMsg.rich);
        }
    }

    // =========================================================
    // 聊天双向翻译 (外发端 - 无缝异步直发)
    // =========================================================
    [HarmonyPatch(typeof(Chat), "OnEnteredUserMessage")]
    internal static class OutgoingChatPatch
    {
        private static bool _isSendingTranslated = false;
        public static string LastSentTranslated = ""; // 用于记录最后发出的外语

        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!Settings.IsAutoTranslateEnabled || !Settings.IsTwoWayTranslationEnabled) return true;
            if (_isSendingTranslated) return true;

            var inputTraverse = Traverse.Create(typeof(Chat)).Field("CHAT_current_input");
            string originalMsg = inputTraverse.GetValue<string>() ?? "";

            if (string.IsNullOrWhiteSpace(originalMsg) || originalMsg.StartsWith("/")) return true;

            // 清空输入框，拦截本次原版发送
            inputTraverse.SetValue("");

            FullBrightPlugin.Instance.StartOutgoingTranslation(originalMsg, translated =>
            {
                _isSendingTranslated = true;

                if (!string.IsNullOrEmpty(translated))
                {
                    LastSentTranslated = translated;
                    inputTraverse.SetValue(translated);
                }
                else
                {
                    inputTraverse.SetValue(originalMsg);
                }

                // OnEnteredUserMessage 是 static 方法，用 typeof(Chat) 调用
                Traverse.Create(typeof(Chat)).Method("OnEnteredUserMessage").GetValue();

                _isSendingTranslated = false;
            });

            return false; // 拦截第一次回车
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
