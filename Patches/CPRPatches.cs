using FullBrightMod.Core;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace FullBrightMod.Patches
{
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

    // ---- 完美复苏 (稳定 100 BPM) ----
    [HarmonyPatch(typeof(CPRMinigame), "Start")]
    internal static class InstantRevivePatch
    {
        [HarmonyPostfix]
        private static void Postfix(CPRMinigame __instance)
        {
            if (!Settings.IsInstantReviveEnabled) return;

            // 优化参数：
            //   BPM = 100 (安全中间值，远低于 220 上限，容差大)
            //   down=0.3, up=0.3 → cycle=0.6s，容差=0.54s
            //   网络抖动 ±100ms 也不会触发 BPM 重算导致超限
            //   30 次刚好够心脏起跳 (REQUIRED=30)，节省体力
            //   每次按压血量恢复效率：down=0.3 → Clamp(0.3×1.5,0.1,0.6)=0.45
            //   num7 = 150×0.45×0.5 = 33.75/次，效率远高于 214 BPM 的 15.75/次
            CPRPacketSender.Send(
                __instance,
                count: 30,
                down: 0.30f,
                up: 0.30f,
                bpm: 100f,
                stopWhenConscious: true
            );
        }
    }

    // ---- 肋骨粉碎者 ----
    [HarmonyPatch(typeof(CPRMinigame), "Start")]
    internal static class RibCrusherPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CPRMinigame __instance)
        {
            if (!Settings.IsRibCrusherEnabled) return;
            
            // 限制为100次，防止施救者过度疲劳
            CPRPacketSender.Send(
                __instance, 
                count: 100, 
                down: 0.20f, 
                up: 0.08f, 
                bpm: 214.2f, 
                stopWhenConscious: false
            );
        }
    }

    // ---- 共享发包逻辑 (极限协程版) ----
    internal static class CPRPacketSender
    {
        public static void Send(CPRMinigame minigame, int count, float down, float up, float bpm, bool stopWhenConscious)
        {
            var pacient = minigame.pacient;
            if (pacient == null || !pacient.alive) return;

            NetBody nb = pacient.GetComponent<NetBody>();
            if (nb == null) return;

            FullBrightPlugin.Instance.StartCoroutine(SendRoutine(nb, count, down, up, bpm, stopWhenConscious));
        }

        private static System.Collections.IEnumerator SendRoutine(NetBody nb, int count, float down, float up, float bpm, bool stopWhenConscious)
        {
            float cycleTime = down + up; 

            for (int i = 0; i < count; i++)
            {
                if (nb == null || nb.body == null || !nb.body.alive) break; 
                
                if (stopWhenConscious && nb.body.conscious)
                {
                    break;
                }

                NetDataWriter writer = Net.CreateWriter((ushort)30050);
                writer.Put(nb.netId);
                writer.Put(down);
                writer.Put(up);
                writer.Put(bpm);
                
                Net.Client_Send(DeliveryMethod.ReliableOrdered, in writer);

                yield return new WaitForSeconds(cycleTime);
            }
        }
    }
}