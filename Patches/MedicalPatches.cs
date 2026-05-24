using HarmonyLib;
using UnityEngine;
using FullBrightMod.Core;

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