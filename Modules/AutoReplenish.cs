using System.Collections.Generic;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// AutoReplenish（自动补货）—— 当玩家主手物品被消耗（扔出/吃掉/用完）后，
    /// 自动从背包（含背包内的 Container）中寻找同类物品并装备到手中。
    /// 
    /// 状态跟踪逻辑：
    ///   每帧记录主手 Item 实例和 id。当主手突然变空时，用三个条件判断是否"消耗"：
    ///   1. Item 实例已销毁（null / !item）
    ///   2. Item 耐久归零（condition <= 0f）
    ///   3. Item 已不在玩家任何槽位中（!body.HoldingItem(item)）→ 被扔出/消耗了
    ///   以上任一成立 → 消耗，触发补货。否则 → 主动移走，不触发。
    /// 
    /// 补货方式：
    ///   使用 body.GetAllItemsThorough() 遍历全身及背包内物品，
    ///   找到匹配 id 的物品后：
    ///     - 如果它在某个槽位中 → SwapSlots 交换到主手
    ///     - 如果它在背包 Container 中 → PickUpItem(force:true) 装备到主手
    /// </summary>
    public class AutoReplenish : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_auto_replenish") ?? "自动补货";
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable()  => ModSettings.IsAutoReplenishEnabled = true;
        public override void OnDisable()
        {
            ModSettings.IsAutoReplenishEnabled = false;
            ResetTracking();
        }

        // ═══════════════════════════════════════════════════
        //  跟踪状态
        // ═══════════════════════════════════════════════════
        private Item   _lastHandItem;
        private string _lastHandId;
        private bool   _hadItem;

        private void ResetTracking()
        {
            _lastHandItem = null;
            _lastHandId  = null;
            _hadItem     = false;
        }

        // ═══════════════════════════════════════════════════
        //  防抖
        // ═══════════════════════════════════════════════════
        private float _cooldownTimer;
        private const float REPLENISH_COOLDOWN = 0.5f;

        // ═══════════════════════════════════════════════════
        //  OnUpdate
        // ═══════════════════════════════════════════════════
        public override void OnUpdate()
        {
            if (!ModSettings.IsAutoReplenishEnabled) return;

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                return;
            }

            var body = PlayerCamera.main?.body;
            if (body == null || !body.conscious) return;

            Item currentHandItem = body.GetItem(body.handSlot);
            bool hasItemNow = currentHandItem != null;

            if (!hasItemNow && _hadItem && _lastHandItem != null)
            {
                // ★ 三重消耗判定：销毁 / 耐久归零 / 不在身上
                bool isConsumed = _lastHandItem == null
                               || !_lastHandItem
                               || _lastHandItem.condition <= 0f
                               || !body.HoldingItem(_lastHandItem);

                if (isConsumed && !string.IsNullOrEmpty(_lastHandId))
                {
                    TryReplenish(body, _lastHandId);
                    _cooldownTimer = REPLENISH_COOLDOWN;
                }
            }

            if (hasItemNow)
            {
                _lastHandItem = currentHandItem;
                _lastHandId  = currentHandItem.id;
            }
            _hadItem = hasItemNow;
        }

        // ═══════════════════════════════════════════════════
        //  补货逻辑
        // ═══════════════════════════════════════════════════
        private static void TryReplenish(Body body, string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;

            int handSlot = body.handSlot;

            // ★ 全面搜索：全身槽位 + 背包 Container
            List<Item> allItems;
            try { allItems = body.GetAllItemsThorough(); }
            catch { return; }
            if (allItems == null || allItems.Count == 0) return;

            foreach (Item item in allItems)
            {
                if (item == null || !item) continue;
                if (item.id != targetId) continue;

                // 跳过主手自身
                if (body.HoldingItem(item) && body.SlotOf(item) == handSlot) continue;

                try
                {
                    if (body.HoldingItem(item))
                    {
                        // ★ 物品在直接槽位中 → SwapSlots 交换
                        int oldSlot = body.SlotOf(item);
                        body.SwapSlots(oldSlot, handSlot);
                    }
                    else
                    {
                        // ★ 物品在背包 Container 中 → PickUpItem 装备
                        //    PickUpItem(force:true) 内部会自动从容器卸载再挂载到手
                        Item handItem = body.GetItem(handSlot);
                        if (handItem != null)
                            body.DropItem(handSlot);
                        body.PickUpItem(item, handSlot, force: true);
                    }
                }
                catch
                {
                    continue;
                }

                return; // 只补一件
            }
        }
    }
}
