using FullBrightMod.Core;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// CPR 自动化补丁 — 打开 CPR 小游戏时自动执行按压发包。
    ///
    /// InstantRevive: 40次完美按压 (bpm=100, down=0.3, up=0.3, 频率在范围)
    /// RibCrusher:   200次恶劣按压 (bpm≈171, down=0.1, up=0.25, 频率在范围, 不同步)
    ///
    /// 服务端分析结论: 30050 包处理没有 consciousness/alive 前置拦截，
    /// 只检查频率范围(AcceptableRate 65~220 BPM)、力量(STR)和时间有效性。
    /// 对清醒玩家同样生效 (增加血氧和血压)。
    /// </summary>

    // ---- 绕过健康检查 —— 强制允许 CPR 按钮对任何目标可用 ----
    [HarmonyPatch(typeof(CPRHandler), "CheckIfBodyIsNotOkAndNeedCPR")]
    internal static class BypassHealthCheckPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref bool __result)
        {
            if (Settings.IsInstantReviveEnabled || Settings.IsRibCrusherEnabled)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    // ---- InstantRevive ----
    [HarmonyPatch(typeof(CPRMinigame), "Start")]
    internal static class InstantRevivePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CPRMinigame __instance)
        {
            if (!Settings.IsInstantReviveEnabled) return;
            CPRPacketSender.Send(__instance, count: 150, down: 0.3f, up: 0.3f, bpm: 100f, tag: "[神医]",
                msg: "已向目标发送 150 次完美心肺复苏！");
        }
    }

    // ---- RibCrusher ----
    [HarmonyPatch(typeof(CPRMinigame), "Start")]
    internal static class RibCrusherPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CPRMinigame __instance)
        {
            if (!Settings.IsRibCrusherEnabled) return;
            CPRPacketSender.Send(__instance, count: 100, down: 0.1f, up: 0.25f, bpm: 171f, tag: "[庸医]",
                msg: "已向目标发送 100 次粉碎性按压！");
        }
    }

    // ---- 共享发包逻辑 ----
    internal static class CPRPacketSender
    {
        public static void Send(CPRMinigame minigame, int count, float down, float up, float bpm,
            string tag, string msg)
        {
            var pacient = minigame.pacient;
            if (pacient == null || !pacient.alive) return;

            NetBody nb = pacient.GetComponent<NetBody>();
            if (nb == null) return;

            for (int i = 0; i < count; i++)
            {
                NetDataWriter writer = Net.CreateWriter((ushort)30050);
                writer.Put(nb.netId);
                writer.Put(down);
                writer.Put(up);
                writer.Put(bpm);
                Net.Client_Send(DeliveryMethod.ReliableUnordered, in writer);
            }

            try { Chat.LogMessage(tag, msg, true); }
            catch { }
        }
    }
}
