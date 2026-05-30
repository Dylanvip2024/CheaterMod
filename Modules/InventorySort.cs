using System;
using System.Collections;
using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.UI;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// 背包整理模块 — 一键合并同类型物品并收纳到容器中。
    /// 通过 DrawSettings 中的模式切换按钮选择整理优先级：
    ///   Space (空间优先)：合并同类项后，将零散物品装入任意容器以腾出槽位。
    ///   Weight (负重优先)：合并同类项后，将最重物品优先装入 encumberanceMult 最小的容器以减重。
    /// </summary>
    public class InventorySort : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_inventory_sort");
        public override ModuleCategory Category => ModuleCategory.Player;

        // ======== 自定义设置 ========
        public override float GetSettingsHeight() => 32f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            Rect btnRect = new Rect(x + 8, y + 4, width - 16, 22);
            string modeLabel = Settings.InventorySortMode == InventorySortMode.Space
                ? Utils.I18n.Get("inv_mode_space")
                : Utils.I18n.Get("inv_mode_weight");

            Color btnColor = Settings.InventorySortMode == InventorySortMode.Space
                ? new Color(0.2f, 0.6f, 1f, 0.85f)
                : new Color(0.6f, 0.4f, 0.2f, 0.85f);

            GUI.color = btnColor;
            GUI.DrawTexture(btnRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(btnRect, modeLabel, labelStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                Settings.InventorySortMode = Settings.InventorySortMode == InventorySortMode.Space
                    ? InventorySortMode.Weight : InventorySortMode.Space;
                e.Use();
            }

            y += 32f;
        }

        // ============================================================
        // 生命周期 — 开启时自动执行整理后关闭
        // ============================================================

        public override void OnEnable()
        {
            if (SortWorker.IsBusy) { Enabled = false; return; }

            Body body = PlayerCamera.main?.body;
            if (body == null) { Enabled = false; return; }

            SortWorker.PlanAndExecute(body, Settings.InventorySortMode);

            // 自动弹回关闭
            Enabled = false;
        }
    }

    // ============================================================
    // 后台整理队列
    // ============================================================

    internal static class SortWorker
    {
        private const float Interval = 0.15f;
        private static readonly Queue<Action> _queue = new Queue<Action>();
        private static CoroutineHost _host;
        private static Coroutine _coroutine;

        public static bool IsBusy => _coroutine != null;

        private static void EnsureHost()
        {
            if (_host != null) return;
            var go = new GameObject("__InventorySort_Host");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<CoroutineHost>();
        }

        public static void Enqueue(Action action)
        {
            if (action != null) _queue.Enqueue(action);
        }

        public static void PlanAndExecute(Body body, InventorySortMode mode)
        {
            if (IsBusy) return;

            // 1. 合并同类项
            PlanCombine(body);

            // 2. 容器收纳
            if (mode == InventorySortMode.Space)
                PlanSpacePack(body);
            else
                PlanWeightPack(body);

            // 3. 超重预警 (仅重量模式)
            if (mode == InventorySortMode.Weight)
                PlanOverweightWarning(body);

            // 4. 启动队列
            EnsureHost();
            _coroutine = _host.StartCoroutine(ProcessQueue());
        }

        // ============================================================
        // 合并
        // ============================================================

        private static void PlanCombine(Body body)
        {
            List<Item> items = body.GetAllItemsThorough();
            if (items == null || items.Count < 2) return;

            var groups = new Dictionary<string, List<Item>>();
            foreach (Item item in items)
            {
                if (item == null || item.condition <= 0f || !item.Stats.combineable) continue;
                if (!groups.TryGetValue(item.id, out var list))
                {
                    list = new List<Item>();
                    groups[item.id] = list;
                }
                list.Add(item);
            }

            foreach (var kv in groups)
            {
                List<Item> group = kv.Value;
                if (group.Count < 2) continue;
                group.Sort((a, b) => b.condition.CompareTo(a.condition));

                for (int i = 1; i < group.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        Item donor = group[i], receiver = group[j];
                        if (donor == null || receiver == null) continue;
                        if (receiver.condition >= 1f) break;
                        if (donor.condition <= 0f) continue;

                        Item r = receiver, d = donor;
                        Enqueue(() =>
                        {
                            if (r != null && d != null && body.CanCombine(r, d))
                                body.CombineItems(r, d);
                        });
                        break;
                    }
                }
            }
        }

        // ============================================================
        // 空间优先收纳
        // ============================================================

        private static void PlanSpacePack(Body body)
        {
            List<Item> containers = GatherContainers(body, sortByWeight: false);
            if (containers.Count == 0) return;

            for (int i = 0; i < body.slots.Length; i++)
            {
                Item slotItem = body.GetItem(i);
                if (slotItem == null || body.slots[i].isHand || containers.Contains(slotItem)) continue;

                foreach (Item cItem in containers)
                {
                    Container cont = cItem?.GetComponent<Container>();
                    if (cont == null || !cont.CanHoldItem(slotItem)) continue;
                    Enqueue(MakePackAction(body, slotItem, cont));
                    break;
                }
            }
        }

        // ============================================================
        // 重量优先收纳
        // ============================================================

        private static void PlanWeightPack(Body body)
        {
            List<Item> containers = GatherContainers(body, sortByWeight: true);
            if (containers.Count == 0) return;

            List<Item> looseItems = new List<Item>();
            for (int i = 0; i < body.slots.Length; i++)
            {
                Item item = body.GetItem(i);
                if (item == null || body.slots[i].isHand || containers.Contains(item)) continue;
                looseItems.Add(item);
            }
            looseItems.Sort((a, b) => b.totalWeight.CompareTo(a.totalWeight));

            foreach (Item loose in looseItems)
            {
                foreach (Item cItem in containers)
                {
                    Container cont = cItem?.GetComponent<Container>();
                    if (cont == null || !cont.CanHoldItem(loose)) continue;
                    Enqueue(MakePackAction(body, loose, cont));
                    break;
                }
            }
        }

        // ============================================================
        // 超重预警
        // ============================================================

        private static void PlanOverweightWarning(Body body)
        {
            Enqueue(() =>
            {
                float total = body.GetTotalEncumberance();
                float max = body.maxEncumberance;
                if (total > max)
                {
                    try
                    {
                        Chat.LogMessage("[背包]",
                            $"<color=#FF0000>已到达减重极限 ({total:F1}/{max:F1})，请手动清理！</color>", true);
                    }
                    catch { }
                }
            });
        }

        // ============================================================
        // 工具
        // ============================================================

        private static List<Item> GatherContainers(Body body, bool sortByWeight)
        {
            List<Item> containers = new List<Item>();
            foreach (Item item in body.GetAllItemsThorough())
            {
                Container cont = item?.GetComponent<Container>();
                if (cont != null && cont.maxWeight > 0f)
                    containers.Add(item);
            }
            if (sortByWeight)
                containers.Sort((a, b) => a.GetComponent<Container>().encumberanceMult
                    .CompareTo(b.GetComponent<Container>().encumberanceMult));
            return containers;
        }

        private static Action MakePackAction(Body body, Item slotItem, Container cont)
        {
            Item si = slotItem;
            Container ci = cont;
            return () =>
            {
                if (si == null || ci == null) return;
                if (!body.HoldingItem(si)) return;
                if (!ci.CanHoldItem(si)) return;
                if (si.TryGetParentContainer(out var pc))
                    pc.UnloadItem(si);
                ci.LoadItem(si);
            };
        }

        // ============================================================
        // 协程
        // ============================================================

        private static IEnumerator ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                try { _queue.Dequeue()?.Invoke(); } catch (Exception ex) { Debug.LogError($"[InvSort] {ex.Message}"); }
                if (_queue.Count > 0) yield return new WaitForSeconds(Interval);
            }
            _coroutine = null;
        }

        private class CoroutineHost : MonoBehaviour { }
    }
}
