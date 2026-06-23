using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FullBrightMod.Core;
using FullBrightMod.Patches;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using KrokoshaCasualtiesMP;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace FullBrightMod.Modules.Player
{
    /// <summary>
    /// TroublemakerAura（闯祸光环）—— 自动对目标玩家执行恶搞/整蛊操作。
    /// 所有操作均通过真实触发原版小游戏实现，经服务器验证，杜绝"自慰式"作弊。
    /// 采用瞬移→设置上下文→触发小游戏→等待秒杀Patch完成→返回的协程流程。
    /// </summary>
    public class TroublemakerAura : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_troublemaker_aura") ?? "闯祸光环";
        public override string Description => "自动对目标玩家执行恶搞操作";
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable()  => ModSettings.IsTroublemakerAuraEnabled = true;
        public override void OnDisable() => ModSettings.IsTroublemakerAuraEnabled = false;

        private float _cooldownTimer;
        private const float COOLDOWN = 2.0f;
        private bool _isBusy;

        public override float GetSettingsHeight()
        {
            float h = 28f;  // 目标模式
            h += 28f;       // 范围
            h += 28f;       // 破片制造
            h += 28f;       // 肋骨粉碎
            h += 28f;       // 推倒
            h += 28f;       // 截肢
            h += 28f;       // 激光灼烧
            return h;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // ★ 动态构建目标选择器：从 ESPCache.CachedPlayers 读取在线玩家列表
            var playerNames = new System.Collections.Generic.List<string>
            {
                Utils.I18n.Get("ta_mode_all") ?? "所有人 (All)"  // index 0
            };
            var players = ESPCache.CachedPlayers;
            if (players != null)
            {
                foreach (var body in players)
                {
                    if (body == null || !body) continue;
                    string displayName = body.name;
                    if (string.IsNullOrEmpty(displayName))
                        displayName = body.gameObject?.name ?? "???";
                    playerNames.Add(displayName);
                }
            }

            // 安全校验：防止玩家退出后索引越界
            int mode = ModSettings.TroublemakerTargetMode;
            mode = Mathf.Clamp(mode, 0, playerNames.Count - 1);
            ModSettings.TroublemakerTargetMode = mode;

            CustomGUI.DrawSelector(x, ref y, width, e,
                Utils.I18n.Get("set_ta_target_mode") ?? "目标模式",
                playerNames.ToArray(), ref mode);
            ModSettings.TroublemakerTargetMode = mode;

            float range = ModSettings.TroublemakerRange;
            CustomGUI.DrawSlider(x, ref y, width, e,
                (Utils.I18n.Get("set_ta_range") ?? "光环范围") + $": {range:F0}m",
                ref range, 1f, 500f);
            ModSettings.TroublemakerRange = range;

            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_ta_shrapnel") ?? "破片制造",
                ref ModSettings.TroublemakerEnableShrapnel);
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_ta_ribcrusher") ?? "肋骨粉碎",
                ref ModSettings.TroublemakerEnableRibCrusher);
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_ta_push") ?? "推倒",
                ref ModSettings.TroublemakerEnablePush);
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_ta_amputation") ?? "截肢",
                ref ModSettings.TroublemakerEnableAmputation);
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_ta_burn") ?? "激光灼烧",
                ref ModSettings.TroublemakerEnableBurn);
        }

        public override void OnUpdate()
        {
            if (!ModSettings.IsTroublemakerAuraEnabled) return;
            if (_isBusy) return;

            var myBody = PlayerCamera.main?.body;
            if (myBody == null || !myBody.conscious) return;

            _cooldownTimer += Time.deltaTime;
            if (_cooldownTimer < COOLDOWN) return;
            _cooldownTimer = 0f;

            _isBusy = true;
            FullBrightPlugin.Instance.StartCoroutine(TroublemakerRoutine(myBody));
        }

        // ══════════════════════════════════════════════════════════════
        //  主协程
        // ══════════════════════════════════════════════════════════════
        private IEnumerator TroublemakerRoutine(Body myBody)
        {
            List<Body> targets = GetTargets(myBody);
            if (targets.Count == 0)
            {
                _isBusy = false;
                yield break;
            }

            Vector3 originalPos = myBody.transform.position;

            foreach (Body targetBody in targets)
            {
                if (targetBody == null || !targetBody || !targetBody.alive) continue;
                if (myBody == null || !myBody.conscious) break;

                NetBody targetNetBody = targetBody.GetComponent<NetBody>();
                if (targetNetBody == null) continue;

                Vector3 targetPos = targetBody.transform.position;
                float dist = Vector2.Distance(originalPos, targetPos);

                // ═══ 瞬移到目标身边 ═══
                if (dist > 1.5f)
                {
                    Vector3 safePos = TPHelper.GetSafeTeleportPosition(originalPos, targetPos, 1.2f);
                    try
                    {
                        if (NetPlayer.TryGetLocalNetBody(out var localNb) && localNb != null)
                            localNb.SetBodyPosition(safePos);
                        else
                        {
                            myBody.transform.position = safePos;
                            if (myBody.rb != null) myBody.rb.position = safePos;
                        }
                        ClientMain.Client_SendCharacterSyncPacket();
                    }
                    catch { }
                }
                yield return new WaitForSeconds(0.12f);

                if (targetBody == null || !targetBody || !targetBody.alive) goto restorePos;

                // ═══ 设置 WoundView 上下文 ═══
                Body prevWoundBody = null;
                if (WoundView.view != null)
                {
                    prevWoundBody = WoundView.view.body;
                    WoundView.view.body = targetBody;
                }

                try
                {
                    // 动作 A：破片制造
                    if (ModSettings.TroublemakerEnableShrapnel)
                        yield return FullBrightPlugin.Instance.StartCoroutine(DoShrapnelOnTarget(myBody, targetBody));

                    if (targetBody == null || !targetBody || !targetBody.alive) break;

                    // 动作 B：肋骨粉碎
                    if (ModSettings.TroublemakerEnableRibCrusher)
                    {
                        if (myBody.stamina > 80f)
                        {
                            yield return FullBrightPlugin.Instance.StartCoroutine(
                                DoRibCrusherRetry(targetNetBody, targetBody));
                        }
                    }

                    if (targetBody == null || !targetBody || !targetBody.alive) break;

                    // 动作 C：推倒
                    if (ModSettings.TroublemakerEnablePush)
                    {
                        DoPush(myBody, targetNetBody);
                        yield return new WaitForSeconds(0.1f);
                    }

                    if (targetBody == null || !targetBody || !targetBody.alive) break;

                    // 动作 D：截肢
                    if (ModSettings.TroublemakerEnableAmputation)
                        yield return FullBrightPlugin.Instance.StartCoroutine(DoAmputationOnTarget(myBody, targetBody));

                    if (targetBody == null || !targetBody || !targetBody.alive) break;

                    // 动作 E：激光灼烧
                    if (ModSettings.TroublemakerEnableBurn)
                        yield return FullBrightPlugin.Instance.StartCoroutine(DoBurnOnTarget(myBody, targetBody));
                }
                finally
                {
                    if (WoundView.view != null && prevWoundBody != null)
                        WoundView.view.body = prevWoundBody;
                }

            restorePos:
                // ═══ 瞬移返回 ═══
                yield return new WaitForSeconds(0.1f);
                try
                {
                    if (NetPlayer.TryGetLocalNetBody(out var localNb) && localNb != null)
                        localNb.SetBodyPosition(originalPos);
                    else
                    {
                        myBody.transform.position = originalPos;
                        if (myBody.rb != null) myBody.rb.position = originalPos;
                    }
                    ClientMain.Client_SendCharacterSyncPacket();
                }
                catch { }
                yield return new WaitForSeconds(0.12f);
            }

            // ★ 整蛊完毕，自动关闭模块（让 ClickGUI 上的高亮自动熄灭）
            this.Enabled = false;
            ModSettings.IsTroublemakerAuraEnabled = false;

            _isBusy = false;
        }

        // ══════════════════════════════════════════════════════════════
        //  目标获取（动态模式）
        //     mode == 0 → 返回范围内所有玩家
        //     mode  > 0 → 返回 CachedPlayers[mode-1] 那个特定玩家
        // ══════════════════════════════════════════════════════════════
        private List<Body> GetTargets(Body myBody)
        {
            List<Body> result = new List<Body>();
            var players = ESPCache.CachedPlayers;
            if (players == null || players.Length == 0) return result;

            Vector2 myPos = myBody.transform.position;
            float rangeSqr = ModSettings.TroublemakerRange * ModSettings.TroublemakerRange;

            int mode = ModSettings.TroublemakerTargetMode;
            // mode=0: 所有人; mode>=1: 特定玩家（options[1] = players[0], options[2] = players[1], ...）
            int targetPlayerIndex = mode - 1;

            if (mode == 0)
            {
                // 返回范围内所有玩家
                foreach (Body target in players)
                {
                    if (target == null || !target || !target.alive) continue;
                    if (target == myBody) continue;
                    float distSqr = ((Vector2)target.transform.position - myPos).sqrMagnitude;
                    if (distSqr > rangeSqr) continue;
                    result.Add(target);
                }
                result.Sort((a, b) =>
                {
                    float dA = ((Vector2)a.transform.position - myPos).sqrMagnitude;
                    float dB = ((Vector2)b.transform.position - myPos).sqrMagnitude;
                    return dA.CompareTo(dB);
                });
            }
            else if (targetPlayerIndex >= 0 && targetPlayerIndex < players.Length)
            {
                // 返回指定索引的玩家（如果仍在范围内且存活）
                Body target = players[targetPlayerIndex];
                if (target != null && target && target.alive && target != myBody)
                {
                    float distSqr = ((Vector2)target.transform.position - myPos).sqrMagnitude;
                    if (distSqr <= rangeSqr)
                        result.Add(target);
                }
            }
            // 如果指定玩家不在范围内/已死亡/已退出，result 为空，协程会提前退出

            return result;
        }

        // ══════════════════════════════════════════════════════════════
        //  动作 A：破片制造 —— 通过 PlayerCamera.ApplyWoundItem 模拟真实 UI 拖放
        // ══════════════════════════════════════════════════════════════
        private IEnumerator DoShrapnelOnTarget(Body myBody, Body targetBody)
        {
            // 1. 查找注射器
            Item syringe = FindSyringeItem(myBody);
            if (syringe == null) yield break;

            // 对目标所有非截肢肢体
            Limb[] limbs = targetBody.limbs;
            if (limbs == null) yield break;

            // ★ 临时启用 ShrapnelMaker
            bool wasShrapnelEnabled = ModSettings.IsShrapnelMakerEnabled;
            ModSettings.IsShrapnelMakerEnabled = true;

            // ★ 保存并激活 WoundView UI（多人的 RPC Hook 依赖 UI 处于激活状态）
            bool prevWoundActive = false;
            if (PlayerCamera.main != null && PlayerCamera.main.woundView != null)
            {
                prevWoundActive = PlayerCamera.main.woundView.activeSelf;
                PlayerCamera.main.woundView.SetActive(true);
            }

            try
            {
                foreach (Limb limb in limbs)
                {
                    if (limb == null || limb.dismembered) continue;

                    // ★ 设置 PlayerCamera 上下文：selectedLimb + dragItem
                    if (PlayerCamera.main != null)
                    {
                        PlayerCamera.main.selectedLimb = limb;
                        PlayerCamera.main.dragItem = syringe;
                    }

                    try
                    {
                        // ★ 调用真实的 UI 交互方法（模拟拖拽物品到肢体上）
                        //    PlayerCamera.TryPerformSpecialUIAction 检测到 WoundViewLimb 后调用此方法
                        //    多人模组(KrokoshaCasualtiesMP)的 Hook 侦测到 WoundView 激活 → 发送 RPC
                        PlayerCamera.main.ApplyWoundItem(syringe);
                    }
                    catch { continue; }

                    // 等待 ShrapnelMakerPatch 秒杀小游戏
                    yield return new WaitForSeconds(0.05f);
                    yield return new WaitWhile(() =>
                        MinigameBase.main != null && MinigameBase.main.currentMinigame != null);
                    yield return new WaitForSeconds(0.08f);
                }
            }
            finally
            {
                ModSettings.IsShrapnelMakerEnabled = wasShrapnelEnabled;
                // 恢复 WoundView 显示状态
                if (PlayerCamera.main != null && PlayerCamera.main.woundView != null)
                    PlayerCamera.main.woundView.SetActive(prevWoundActive);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  动作 B：肋骨粉碎 —— 带失败重试（最多 5 次）
        // ══════════════════════════════════════════════════════════════
        private static IEnumerator DoRibCrusherRetry(NetBody targetNetBody, Body targetBody)
        {
            const int MAX_RETRIES = 5;
            for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
            {
                if (targetNetBody == null || targetBody == null || !targetBody.alive) yield break;

                // 发送 CPR 数据包
                yield return SendCprPackets(targetNetBody, 12, 0.20f, 0.08f, 214.2f);

                // 等待服务器处理（1秒）
                yield return new WaitForSeconds(1.0f);

                // ★ 检查肋骨是否已骨折（limbs[1] = UpTorso / 胸腔）
                if (targetBody != null && targetBody && targetBody.limbs != null
                    && targetBody.limbs.Length > 1 && targetBody.limbs[1].broken)
                    yield break; // 成功！
            }
        }

        private static IEnumerator SendCprPackets(NetBody nb, int count, float down, float up, float bpm)
        {
            float cycleTime = down + up;
            for (int i = 0; i < count; i++)
            {
                if (nb == null || nb.body == null || !nb.body.alive) break;
                NetDataWriter writer = Net.CreateWriter((ushort)10049);
                writer.Put(nb.netId);
                writer.Put(down);
                writer.Put(up);
                writer.Put(bpm);
                Net.Client_Send(DeliveryMethod.ReliableOrdered, in writer);
                yield return new WaitForSeconds(cycleTime);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  动作 C：推倒
        // ══════════════════════════════════════════════════════════════
        private static void DoPush(Body myBody, NetBody targetNetBody)
        {
            try
            {
                NetBody localNetBody = NetPlayer.LOCAL_PLAYER?.playerbody;
                if (localNetBody == null || targetNetBody == null) return;
                if (myBody.stamina < 5f) return;
                localNetBody.Push(targetNetBody);
                myBody.stamina -= 5f;
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════
        //  动作 D：截肢 —— 通过 PlayerCamera.ApplyWoundItem 模拟真实 UI 拖放
        // ══════════════════════════════════════════════════════════════
        private IEnumerator DoAmputationOnTarget(Body myBody, Body targetBody)
        {
            // 1. 查找截肢工具
            Item cuttingTool = FindCuttingTool(myBody);
            if (cuttingTool == null) yield break;

            // 截除前臂(3)和前腿(9)  — 对应 Body.LimbNum.ArmF 和 Body.LimbNum.LegF
            int[] targetIndices = new int[] { 3, 9 };
            Limb[] limbs = targetBody.limbs;
            if (limbs == null) yield break;

            // ★ 临时启用 InstantAmputation
            bool wasAmputationEnabled = ModSettings.IsInstantAmputationEnabled;
            ModSettings.IsInstantAmputationEnabled = true;

            // ★ 激活 WoundView UI（多人的 RPC Hook 依赖 UI 处于激活状态）
            bool prevWoundActive = false;
            if (PlayerCamera.main != null && PlayerCamera.main.woundView != null)
            {
                prevWoundActive = PlayerCamera.main.woundView.activeSelf;
                PlayerCamera.main.woundView.SetActive(true);
            }

            try
            {
                foreach (int idx in targetIndices)
                {
                    if (idx >= limbs.Length) continue;
                    Limb limb = limbs[idx];
                    if (limb == null || limb.dismembered) continue;

                    // ★ 绕过 DoAmputate 的感染检查
                    float savedInfection = limb.infectionAmount;
                    limb.infectionAmount = 100f;

                    // ★ 设置 PlayerCamera 上下文：selectedLimb + dragItem
                    if (PlayerCamera.main != null)
                    {
                        PlayerCamera.main.selectedLimb = limb;
                        PlayerCamera.main.dragItem = cuttingTool;
                    }

                    try
                    {
                        // ★ 调用真实的 UI 交互方法
                        PlayerCamera.main.ApplyWoundItem(cuttingTool);
                    }
                    catch
                    {
                        try { limb.infectionAmount = savedInfection; } catch { }
                        continue;
                    }

                    // 恢复感染值
                    try { limb.infectionAmount = savedInfection; } catch { }

                    // 等待 InstantAmputationPatch 秒杀小游戏
                    yield return new WaitForSeconds(0.05f);
                    yield return new WaitWhile(() =>
                        MinigameBase.main != null && MinigameBase.main.currentMinigame != null);
                    yield return new WaitForSeconds(0.12f);
                }
            }
            finally
            {
                ModSettings.IsInstantAmputationEnabled = wasAmputationEnabled;
                if (PlayerCamera.main != null && PlayerCamera.main.woundView != null)
                    PlayerCamera.main.woundView.SetActive(prevWoundActive);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  物品检测（基于真实游戏属性，杜绝硬编码字符串）
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// 查找注射器类物品。
        /// 标准：tag 包含 "medicine" 且 usableOnLimb == true。
        /// </summary>
        private static Item FindSyringeItem(Body myBody)
        {
            try
            {
                List<Item> items = myBody.GetAllItemsThorough();
                if (items == null) return null;
                foreach (Item item in items)
                {
                    if (item == null || !item) continue;
                    if (item.Stats == null) continue;
                    if (item.Stats.usableOnLimb && item.Stats.HasTag("medicine"))
                        return item;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 查找截肢工具。
        /// 标准：拥有 "cutting" CraftingQuality（所有截肢工具均有此品质）。
        /// </summary>
        private static Item FindCuttingTool(Body myBody)
        {
            try
            {
                List<Item> items = myBody.GetAllItemsThorough();
                if (items == null) return null;
                foreach (Item item in items)
                {
                    if (item == null || !item) continue;
                    if (item.Stats == null || item.Stats.qualities == null) continue;
                    foreach (var q in item.Stats.qualities)
                    {
                        if (q.id == "cutting")
                            return item;
                    }
                }
            }
            catch { }
            return null;
        }

        // ══════════════════════════════════════════════════════════════
        //  动作 E：激光灼烧 —— 通过 PlayerCamera.ApplyWoundItem 模拟 UI 拖放
        // ══════════════════════════════════════════════════════════════
        private IEnumerator DoBurnOnTarget(Body myBody, Body targetBody)
        {
            // 1. 查找激光切割器
            Item laser = FindLaserCutter(myBody);
            if (laser == null) yield break;

            // 最多灼烧 5 个肢体
            Limb[] limbs = targetBody.limbs;
            if (limbs == null) yield break;

            // ★ 激活 WoundView UI
            bool prevWoundActive = false;
            if (PlayerCamera.main != null && PlayerCamera.main.woundView != null)
            {
                prevWoundActive = PlayerCamera.main.woundView.activeSelf;
                PlayerCamera.main.woundView.SetActive(true);
            }

            int burnedCount = 0;
            const int MAX_BURN = 5;

            try
            {
                foreach (Limb limb in limbs)
                {
                    if (burnedCount >= MAX_BURN) break;
                    if (limb == null || limb.dismembered) continue;

                    if (PlayerCamera.main != null)
                    {
                        PlayerCamera.main.selectedLimb = limb;
                        PlayerCamera.main.dragItem = laser;
                    }

                    try
                    {
                        // ★ 调用真实 UI 交互方法
                        PlayerCamera.main.ApplyWoundItem(laser);
                    }
                    catch { continue; }

                    burnedCount++;
                    yield return new WaitForSeconds(0.2f);

                    // 等待灼烧小游戏完成（如果有的话）
                    yield return new WaitWhile(() =>
                        MinigameBase.main != null && MinigameBase.main.currentMinigame != null);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            finally
            {
                if (PlayerCamera.main != null && PlayerCamera.main.woundView != null)
                    PlayerCamera.main.woundView.SetActive(prevWoundActive);
            }
        }

        /// <summary>
        /// 查找激光切割器。
        /// 标准：tag 包含 "medical" 且 usableOnLimb == true 且物品名匹配。
        /// </summary>
        private static Item FindLaserCutter(Body myBody)
        {
            try
            {
                List<Item> items = myBody.GetAllItemsThorough();
                if (items == null) return null;
                foreach (Item item in items)
                {
                    if (item == null || !item) continue;
                    if (item.Stats == null) continue;
                    // ★ 激光切割器是医疗工具，usableOnLimb == true
                    if (!item.Stats.usableOnLimb) continue;
                    string id = item.id?.ToLower() ?? "";
                    if (id == "plasmacutter" || id.Contains("laser") || id.Contains("cauter"))
                        return item;
                }
            }
            catch { }
            return null;
        }
    }
}
