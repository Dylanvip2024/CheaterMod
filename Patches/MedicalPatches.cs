using System.Collections;
using HarmonyLib;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using FullBrightMod.Core;
using KrokoshaCasualtiesMP;

namespace FullBrightMod.Patches
{
    // ==========================================
    // 💉 破片制造者 (带有精准抓取判定的两段式劫持)
    // ==========================================
    [HarmonyPatch(typeof(SyringeMinigame), "Update")]
    internal static class ShrapnelMakerPatch
    {
        [HarmonyPrefix]
        private static void Prefix(SyringeMinigame __instance)
        {
            if (!Settings.IsShrapnelMakerEnabled) return;
            if (Minigame.game == null) return;

            // 利用 Harmony 的 Traverse 工具直接读取私有状态变量
            bool isHolding = Traverse.Create(__instance).Field("holdingSyringe").GetValue<bool>();

            if (!isHolding)
            {
                // 第一阶段：精准定位，瞬间把手移动到针筒的中心，并模拟鼠标按下
                Minigame.game.handPos = __instance.syringe.anchoredPosition;
                Minigame.game.handStartedClicking = true;
                Minigame.game.handStoppedClicking = false;
            }
            else
            {
                // 第二阶段：一旦引擎确认抓住，立刻向斜下方以恐怖的速度撕裂！
                Minigame.game.handPos = new Vector2(300f, -250f);
            }
        }
    }

    // ==========================================
    // 🪚 秒截肢 (物理欺骗版 - 完美绕过服务端拦截)
    // ==========================================
    [HarmonyPatch(typeof(AmputationMinigame), "Update")]
    internal static class InstantAmputationPatch
    {
        [HarmonyPrefix]
        private static void Prefix(AmputationMinigame __instance)
        {
            if (!Settings.IsInstantAmputationEnabled) return;
            if (Minigame.game == null) return;

            // 1. 强行将手压下，确保处于切割判定区内 (cutting = true)
            Minigame.game.handPos = new Vector2(Minigame.game.handPos.x, __instance.maxHandPos);

            // 2. 模拟极限手速 (25000f)
            // 25000 的速度在一帧 (0.016s) 内会造成约 400 点组织伤害，完美破坏皮肉满足服务端的条件。
            // 同时在 PhysicsUpdate 中，25000 * 5E-05 = 1.25，瞬间让进度条大于 100%。
            Minigame.game.handVelocity = new Vector2(25000f, 0f); 
        }
    }
    // ==========================================
    // 🦴 正骨大师 — 生命周期重置 (解决对象池复用导致的失效)
    // ==========================================
    [HarmonyPatch(typeof(DislocationMinigame), "Start")]
    internal static class AutoDislocationStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix(DislocationMinigame __instance)
        {
            int id = __instance.GetHashCode();
            AutoDislocationPatch._instanceStates[id] = 0;
            AutoDislocationPatch._aliveTimers[id] = 0f;
        }
    }

    // ==========================================
    // 🦴 正骨大师 — 渐进式伪造发包 (多实例独立状态追踪防重入)
    // ==========================================
    [HarmonyPatch(typeof(DislocationMinigame), "Update")]
    internal static class AutoDislocationPatch
    {
        // 状态机: 0=初始化缓冲中, 1=发包中, 2=结算完成
        internal static readonly System.Collections.Generic.Dictionary<int, int> _instanceStates = new System.Collections.Generic.Dictionary<int, int>();
        internal static readonly System.Collections.Generic.Dictionary<int, float> _aliveTimers = new System.Collections.Generic.Dictionary<int, float>();

        [HarmonyPrefix]
        private static bool Prefix(DislocationMinigame __instance)
        {
            if (!Settings.IsAutoDislocationEnabled) return true;
            if (Minigame.game == null) return true;

            // 核心修复：获取当前实例的唯一物理 ID
            int id = __instance.GetHashCode();

            // 初始化该实例的独立状态
            if (!_instanceStates.ContainsKey(id))
            {
                _instanceStates[id] = 0;
                _aliveTimers[id] = 0f;
            }

            int state = _instanceStates[id];

            // 状态 2：已经发包完毕。永远放行原版代码去自动检测距离并关闭 UI！
            if (state == 2) return true; 

            // 状态 1：正在发包中。拦截原版 Update，冻结本地物理计算。
            if (state == 1) return false; 

            // 状态 0：初始化阶段。给予 0.6 秒缓冲，让游戏完成骨头的 UI 排版。
            _aliveTimers[id] += Time.deltaTime;
            if (_aliveTimers[id] < 0.6f) return true;

            // 缓冲期结束，切入状态 1，启动协程。
            // 因为是按 ID 锁死的，绝对不可能再次重入！
            _instanceStates[id] = 1;
            FullBrightPlugin.Instance.StartCoroutine(SpoofDislocationRoutine(__instance, id));

            return false;
        }

        private static IEnumerator SpoofDislocationRoutine(DislocationMinigame minigame, int id)
        {

            Vector2 startPos = Vector2.zero;
            RectTransform bone = null;
            try
            {
                bone = Traverse.Create(minigame).Field("bone").GetValue<RectTransform>();
                if (bone != null) startPos = bone.anchoredPosition;
            }
            catch { }

            Vector2 targetPos = new Vector2(375f, 54.6f);
            Vector2 dir = (targetPos - startPos).normalized;
            if (dir == Vector2.zero) dir = Vector2.left;

            // 3 次大锤：1250f 威力，0.95s 间隔确保衰减到 <60 通过守卫
            for (int i = 0; i < 3; i++)
            {
                // ---- 存活检测（发包前） ----
                if (minigame == null || Minigame.game == null)
                    break;

                SendHitPacket(dir * 1250f);

                // 安全 UI 更新
                float progress = (i + 1f) / 3f;
                if (bone != null && bone.gameObject != null && bone.gameObject.activeInHierarchy)
                    bone.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);

                yield return new WaitForSeconds(0.95f);

                // ---- 存活检测（等待后） ----
                if (minigame == null || Minigame.game == null)
                    break;
            }

            // 确保骨头精准停在胜利坐标
            if (bone != null && bone.gameObject != null && bone.gameObject.activeInHierarchy)
                bone.anchoredPosition = targetPos;

            // 不论正常完成还是提前退出，解锁状态
            _instanceStates[id] = 2;
        }

        private static void SendHitPacket(Vector2 velocityDelta)
        {
            var writer = Net.CreateWriter(30065);
            MyLiteNetLibExtensions.Put(writer, velocityDelta);
            writer.Put(true);
            Net.Client_Send(DeliveryMethod.ReliableOrdered, in writer);
        }
    }

    // ==========================================
    // 🩺 秒拔破片小游戏劫持
    // ==========================================
    [HarmonyPatch(typeof(ShrapnelMinigame), "Update")]
    [HarmonyPriority(Priority.First)] // 极其重要：确保我们的补丁抢在联机Mod的同步补丁之前执行！
    internal static class InstantShrapnelRemovalPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ShrapnelMinigame __instance)
        {
            if (!Settings.IsInstantShrapnelRemovalEnabled) return;
            if (Minigame.game == null) return;

            var trav = Traverse.Create(__instance);
            var objectsList = trav.Field("objects").GetValue<System.Collections.Generic.List<RectTransform>>();
            if (objectsList == null) return;

            RectTransform target = null;
            // 找到下一个还在肉里的破片
            foreach (var shrapnel in objectsList)
            {
                if (shrapnel.anchoredPosition.y < 35f)
                {
                    target = shrapnel;
                    break;
                }
            }

            if (target != null)
            {
                // 1. 强行篡改私有变量：让引擎以为我们“刚刚夹住了”这根破片
                trav.Field("currentlyHeld").SetValue(target);
                trav.Field("heldOffset").SetValue(Vector2.zero);
                
                // 2. 核心欺骗：保持破片还在肉里，但把手瞬间扯到安全线以上 (Y = 100f)
                // 这样在下一微秒联机Mod运行同步补丁时，会完美落入它的合法发包判定条件！
                Minigame.game.handPos = new Vector2(target.anchoredPosition.x, 100f);
                
                // 顺便加上极高的向上物理速度，防止任何后续的物理校验
                Minigame.game.handVelocity = new Vector2(0f, 1000f); 
            }
            else
            {
                // 破片已经全被发包扯完了，松开镊子，等待游戏自行正常结算
                trav.Field("currentlyHeld").SetValue((RectTransform)null);
            }
        }
    }
}