using System.Collections;
using FullBrightMod.Core;
using FullBrightMod.Patches;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// PickupAura（拾取光环）—— 自动捡取范围内最近的物品。
    /// 支持传送捡取模式（无距离限制瞬移拾取）。
    /// </summary>
    public class PickupAura : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_pickup_aura") ?? "拾取光环";
        public override ModuleCategory Category => ModuleCategory.World;
        public override void OnEnable()
        {
            ModSettings.IsPickupAuraEnabled = true;
            _scanCooldown = 0f;
            _isBlinking = false;
        }
        public override void OnDisable()
        {
            ModSettings.IsPickupAuraEnabled = false;
            _isBlinking = false;
            _currentTarget = null;
        }

        private float _scanCooldown;
        private const float SCAN_INTERVAL = 0.3f;
        private const float BLINK_DELAY = 0.1f;
        private bool _isBlinking;
        private Item _currentTarget;

        public override float GetSettingsHeight() => 88f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(14, 0, 2, 0)
            };

            if (ModSettings.PickupAuraTeleportPickup)
            {
                // 传送模式：大范围滑块 + 过滤开关
                float tpRange = ModSettings.PickupAuraTeleportRange;
                GUI.Label(new Rect(x, y, width, 18f), (Utils.I18n.Get("set_pickup_tp_range") ?? "传送捡取范围") + ": " + tpRange.ToString("F0") + "m", labelStyle);
                Rect sliderRect = new Rect(x + 14, y + 16, width - 28, 8);
                float newRange = ClickGUIManager.DrawSlider(sliderRect, tpRange, 5f, 500f, e);
                if (Mathf.Abs(newRange - tpRange) > 0.001f) ModSettings.PickupAuraTeleportRange = newRange;
                y += 30f;
            }
            else
            {
                // 常规模式：小范围滑块
                float range = ModSettings.PickupAuraRange;
                GUI.Label(new Rect(x, y, width, 18f), (Utils.I18n.Get("set_pickup_range") ?? "拾取范围") + ": " + range.ToString("F1") + "m", labelStyle);
                Rect sliderRect = new Rect(x + 14, y + 16, width - 28, 8);
                float newRange = ClickGUIManager.DrawSlider(sliderRect, range, 2f, 30f, e);
                if (Mathf.Abs(newRange - range) > 0.001f) ModSettings.PickupAuraRange = newRange;
                y += 30f;
            }

            // 传送捡取开关
            DrawToggleRow(x, ref y, width, e, Utils.I18n.Get("set_pickup_tp") ?? "传送捡取", ref ModSettings.PickupAuraTeleportPickup);

            // 过滤低价值开关
            DrawToggleRow(x, ref y, width, e, Utils.I18n.Get("set_pickup_filter") ?? "过滤低价值物品", ref ModSettings.PickupAuraFilterLowValue);
        }

        private static void DrawToggleRow(float x, ref float y, float width, Event e, string label, ref bool value)
        {
            Rect rowRect = new Rect(x, y, width, 26f);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? new Color(0.2f, 0.6f, 1.0f, 0.85f) : (hover ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(rowRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;
            GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, normal = { textColor = value ? Color.white : new Color(0.7f, 0.7f, 0.7f) }, padding = new RectOffset(14, 0, 4, 0) };
            GUI.Label(rowRect, (value ? "[ON]  " : "[OFF] ") + label, s);
            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition)) { value = !value; e.Use(); }
            y += 28f;
        }

        public override void OnUpdate()
        {
            if (!ModSettings.IsPickupAuraEnabled) return;
            if (_isBlinking || ModSettings.IsRemotePickupEnabled) return;

            var body = PlayerCamera.main?.body;
            if (body == null || !body.conscious) return;

            _scanCooldown -= Time.deltaTime;
            if (_scanCooldown > 0f) return;
            _scanCooldown = SCAN_INTERVAL;

            Item item = FindBestItem(body);
            if (item == null) return;

            _currentTarget = item;
            FullBrightPlugin.Instance.StartCoroutine(PickupCoroutine(body, item));
        }

        private Item FindBestItem(Body body)
        {
            Item[] allItems = ModSettings.PickupAuraTeleportPickup
                ? GameObject.FindObjectsOfType<Item>()
                : ESPCache.CachedItems;
            if (allItems == null) return null;

            Vector2 myPos = (Vector2)body.transform.position;
            float rangeSqr = ModSettings.PickupAuraTeleportPickup
                ? ModSettings.PickupAuraTeleportRange * ModSettings.PickupAuraTeleportRange
                : ModSettings.PickupAuraRange * ModSettings.PickupAuraRange;
            Item best = null;
            float bestDist = float.MaxValue;

            foreach (Item item in allItems)
            {
                if (item == null || !item) continue;
                if (item.ParentContainer()) continue;
                if (body.HoldingItem(item)) continue;
                if (!item.gameObject.activeInHierarchy) continue;

                float distSqr = ((Vector2)item.transform.position - myPos).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                if (ModSettings.PickupAuraFilterLowValue && item.condition < 0.3f) continue;

                float dist = Mathf.Sqrt(distSqr);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = item;
                }
            }
            return best;
        }

        private IEnumerator PickupCoroutine(Body body, Item item)
        {
            if (_isBlinking) yield break;
            _isBlinking = true;

            Vector3 originalPos = body.transform.position;

            // 传送模式 或 超出普通步行范围 → 瞬移
            if (ModSettings.PickupAuraTeleportPickup || Vector2.Distance(originalPos, item.transform.position) > 3f)
            {
                try
                {
                    Vector3 safePos = TPHelper.GetSafeTeleportPosition(originalPos, item.transform.position, 1.2f);
                    if (NetPlayer.TryGetLocalNetBody(out var localNb) && localNb != null)
                        localNb.SetBodyPosition(safePos);
                    else
                    {
                        body.transform.position = safePos;
                        if (body.rb != null) body.rb.position = safePos;
                    }
                    ClientMain.Client_SendCharacterSyncPacket();
                }
                catch { }
                yield return new WaitForSeconds(BLINK_DELAY);
            }

            try { body.AutoPickUpItem(item); } catch { }

            yield return new WaitForSeconds(BLINK_DELAY);

            // 瞬移回去了，确保归位
            try
            {
                if (NetPlayer.TryGetLocalNetBody(out var localNb2) && localNb2 != null)
                    localNb2.SetBodyPosition(originalPos);
                else
                {
                    body.transform.position = originalPos;
                    if (body.rb != null) body.rb.position = originalPos;
                }
                ClientMain.Client_SendCharacterSyncPacket();
            }
            catch { }

            _isBlinking = false;
        }
    }
}
