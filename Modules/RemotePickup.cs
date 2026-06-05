using System;
using System.Collections;
using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.Patches;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// RemotePickup — 掉落物动态箭头拾取 (图文混合版)
    /// 扫描范围内掉落物，屏幕内显示悬浮箭头与文本，屏幕外边缘显示动态指向箭头与图标。
    /// 点击后一帧瞬移拾取并归位。
    /// </summary>
    public class RemotePickup : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_remote_pickup") ?? "远程拾取";
        public override string Description => "点击箭头标识远程拾取物品";
        public override ModuleCategory Category => ModuleCategory.World;

        private float _scanCooldown;
        private const float SCAN_INTERVAL = 0.1f;
        private const float BLINK_DELAY = 0.12f;

        private bool _isBlinking;

        private struct Entry
        {
            public Item item;
            public Vector3 worldPos;
            public string label;
            public float distance;
            public Vector2 scrPos;
            public bool onScreen;
            public Vector2 edgePos;
            public float angleDeg;
            
            // 贴图渲染数据
            public Texture2D iconTex;
            public Rect texCoords;
        }
        private List<Entry> _entries = new List<Entry>();

        /// <summary>
        /// ★ 图标缓存字典 —— 避免每次扫描都执行 GetComponentInChildren 导致高频 GC 分配。
        /// Key 为物品实例，Value 为 (贴图, UV坐标)。
        /// 每次扫描前清理已销毁的条目防止内存泄漏。
        /// </summary>
        private Dictionary<Item, (Texture2D tex, Rect coords)> _iconCache = new Dictionary<Item, (Texture2D, Rect)>();

        /// <summary>★ 复用清理列表，避免 CleanIconCache 每次分配新 List 造成 GC</summary>
        private List<Item> _deadKeys = new List<Item>();

        private GUIStyle _onScreenStyle;
        private GUIStyle _offScreenArrowStyle;
        private GUIStyle _lblStyle;
        private bool _stylesReady;

        public override void OnEnable()
        {
            ModSettings.IsRemotePickupEnabled = true;
            _isBlinking = false;
            ModSettings.IsRemotePickupBlinking = false;
            _scanCooldown = 0f;
            _entries.Clear();
        }

        public override void OnDisable()
        {
            ModSettings.IsRemotePickupEnabled = false;
            _isBlinking = false;
            ModSettings.IsRemotePickupBlinking = false;
            _entries.Clear();
        }

        public override float GetSettingsHeight() => 30f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            Rect labelRect = new Rect(x + 8, y, 90, 22);
            GUI.Label(labelRect, $"  {(Utils.I18n.Get("set_pickup_range") ?? "拾取范围")}: {ModSettings.RemotePickupRange:F0}m", labelStyle);

            Rect barRect = new Rect(x + 98, y + 6, width - 113, 14);
            ModSettings.RemotePickupRange = ClickGUIManager.DrawSlider(barRect, ModSettings.RemotePickupRange, 5f, 200f, e);

            y += 30f;
        }

        public override void OnUpdate()
        {
            if (!ModSettings.IsRemotePickupEnabled) return;
            if (_isBlinking) return;

            _scanCooldown -= Time.deltaTime;
            if (_scanCooldown > 0f) return;
            _scanCooldown = SCAN_INTERVAL;

            var body = PlayerCamera.main?.body;
            if (body == null || !body.conscious) return;

            _entries = ScanItems(body);
        }

        public override void OnGUI()
        {
            if (!Enabled || !ModSettings.IsRemotePickupEnabled) return;

            Camera cam = Camera.main;
            Body body = PlayerCamera.main?.body;
            if (cam == null || body == null) return;
            if (!body.conscious) return;
            EnsureStyles();

            Vector2 myPos = body.transform.position;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry e = _entries[i];
                if (e.item == null || !e.item) { _entries.RemoveAt(i); continue; }
                if (!e.item.gameObject.activeInHierarchy) { _entries.RemoveAt(i); continue; }

                // 每帧重算屏幕坐标，保证移动时箭头丝滑跟随
                var sp = cam.WorldToScreenPoint(e.worldPos);
                e.scrPos = new Vector2(sp.x, Screen.height - sp.y);
                e.onScreen = sp.z > 0f && sp.x >= 0f && sp.x <= Screen.width && sp.y >= 0f && sp.y <= Screen.height;
                e.distance = Vector2.Distance(myPos, e.worldPos);

                if (!e.onScreen)
                    EdgeFromScreenPos(sp, out e.edgePos, out e.angleDeg);

                _entries[i] = e;

                if (e.onScreen)
                    DrawButton(e, body);
                else
                    DrawOffscreen(e, body);
            }
        }

        // ═══════════════════════════════════════════
        //  扫描与坐标计算
        // ═══════════════════════════════════════════
        private List<Entry> ScanItems(Body body)
        {
            List<Entry> results = new List<Entry>();

            // ★ 性能优化：使用全局缓存池替代高频 FindObjectsOfType<Item>()
            Item[] allItems = ESPCache.CachedItems;
            if (allItems == null) return results;

            Camera cam = Camera.main;
            if (cam == null) return results;

            Vector2 myPos = body.transform.position;
            float rangeSqr = ModSettings.RemotePickupRange * ModSettings.RemotePickupRange;

            // ★ 清理图标缓存中已销毁的 Item 条目，防止内存泄漏
            CleanIconCache();

            foreach (Item item in allItems)
            {
                if (item == null || !item) continue;
                if (item.ParentContainer()) continue;
                if (body.HoldingItem(item)) continue;
                if (!item.gameObject.activeInHierarchy) continue;

                float distSqr = ((Vector2)item.transform.position - myPos).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                Vector3 wp = item.transform.position;
                Vector3 sp = cam.WorldToScreenPoint(wp);

                // ★ 图标缓存：优先从字典获取，避免每次扫描都执行 GetComponentInChildren 的 GC 开销
                Texture2D tex = null;
                Rect coords = new Rect(0, 0, 1, 1);
                if (_iconCache.TryGetValue(item, out var cached))
                {
                    tex = cached.tex;
                    coords = cached.coords;
                }
                else
                {
                    SpriteRenderer sr = item.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null && sr.sprite != null && sr.sprite.texture != null)
                    {
                        tex = sr.sprite.texture;
                        Rect tr = sr.sprite.textureRect;
                        float texW = tex.width > 0f ? tex.width : 1f;
                        float texH = tex.height > 0f ? tex.height : 1f;
                        coords = new Rect(tr.x / texW, tr.y / texH, tr.width / texW, tr.height / texH);
                    }
                    // 存入缓存（即使 tex 为 null 也存入，避免重复查询）
                    _iconCache[item] = (tex, coords);
                }

                results.Add(new Entry
                {
                    item = item,
                    worldPos = wp,
                    label = ShortName(item),
                    distance = Mathf.Sqrt(distSqr),
                    iconTex = tex,
                    texCoords = coords
                });
            }

            results.Sort((a, b) => a.distance.CompareTo(b.distance));
            return results;
        }

        /// <summary>
        /// ★ 清理图标缓存中已销毁的 Item 键，防止内存泄漏。
        /// 复用 _deadKeys 列表避免每次调用时分配新 List 造成 GC 开销。
        /// </summary>
        private void CleanIconCache()
        {
            _deadKeys.Clear();
            foreach (var kvp in _iconCache)
            {
                if (kvp.Key == null || !kvp.Key)
                    _deadKeys.Add(kvp.Key);
            }
            for (int i = 0; i < _deadKeys.Count; i++)
                _iconCache.Remove(_deadKeys[i]);
        }

        // ═══════════════════════════════════════════
        //  屏幕内：悬浮向下箭头 + 物品名称
        // ═══════════════════════════════════════════
        private void DrawButton(Entry e, Body body)
        {
            const float BW = 100f, BH = 40f;
            // 矩形放置在物品的略上方
            Rect r = new Rect(e.scrPos.x - BW * 0.5f, e.scrPos.y - BH, BW, BH);

            float t = Mathf.Clamp01(e.distance / ModSettings.RemotePickupRange);
            _onScreenStyle.normal.textColor = Color.Lerp(Color.green, Color.red, t);

            // 绘制清爽的无边框按钮（▼形状 + 名称）
            if (GUI.Button(r, $"▼\n{e.label}", _onScreenStyle) && !_isBlinking)
                FullBrightPlugin.Instance.StartCoroutine(PickupOne(body, e));

            // 显示距离
            GUI.Label(
                new Rect(e.scrPos.x - 30, e.scrPos.y + 5, 60, 14),
                $"{e.distance:F0}m", _lblStyle);
        }

        // ═══════════════════════════════════════════
        //  屏幕外：边缘指向箭头 -> 图标 -> 文本
        // ═══════════════════════════════════════════
        private void DrawOffscreen(Entry e, Body body)
        {
            const float AW = 36f, AH = 36f;
            const float ICON_SIZE = 32f;
            
            // 1. 箭头颜色随距离变化
            float t = Mathf.Clamp01(e.distance / ModSettings.RemotePickupRange);
            _offScreenArrowStyle.normal.textColor = Color.Lerp(Color.green, Color.red, t);

            // 2. 渲染旋转箭头
            Rect arrowRect = new Rect(e.edgePos.x - AW * 0.5f, e.edgePos.y - AH * 0.5f, AW, AH);
            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(e.angleDeg, arrowRect.center);
            GUI.Label(arrowRect, "➤", _offScreenArrowStyle); // 纯文本绘制
            GUI.matrix = matrixBackup;

            // 3. 渲染物品图标 (在箭头下方)
            Rect iconRect = new Rect(e.edgePos.x - ICON_SIZE * 0.5f, e.edgePos.y + AH * 0.5f, ICON_SIZE, ICON_SIZE);
            if (e.iconTex != null)
            {
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(iconRect, e.iconTex, e.texCoords);
            }

            // 4. 渲染文字 (在图标下方)
            Rect textRect = new Rect(e.edgePos.x - 45, e.edgePos.y + AH * 0.5f + ICON_SIZE + 2f, 90, 14);
            GUI.Label(textRect, $"{e.label} {e.distance:F0}m", _lblStyle);

            // 5. 统一的可点击热区（包裹箭头、图标、文字的隐形按钮）
            Rect hitBox = new Rect(e.edgePos.x - 45, e.edgePos.y - AH * 0.5f, 90, AH + ICON_SIZE + 20f);
            Color old = GUI.color;
            GUI.color = Color.clear;
            if (GUI.Button(hitBox, "") && !_isBlinking)
                FullBrightPlugin.Instance.StartCoroutine(PickupOne(body, e));
            GUI.color = old;
        }

        // ═══════════════════════════════════════════
        //  单个闪现拾取协程
        // ═══════════════════════════════════════════
        private IEnumerator PickupOne(Body body, Entry e)
        {
            if (_isBlinking) yield break;
            if (e.item == null || !e.item || !e.item.gameObject.activeInHierarchy) yield break;

            _isBlinking = true;
            ModSettings.IsRemotePickupBlinking = true;

            Vector3 originalPos = body.transform.position;
            bool success = false;

            try
            {
                Vector3 safePos = TPHelper.GetSafeTeleportPosition(
                    originalPos, e.item.transform.position, 1.5f);
                if (NetPlayer.TryGetLocalNetBody(out var localNb) && localNb != null)
                {
                    localNb.SetBodyPosition(safePos);
                }
                else
                {
                    body.transform.position = safePos;
                    if (body.rb != null) body.rb.position = safePos;
                }
                ClientMain.Client_SendCharacterSyncPacket();
            }
            catch { _isBlinking = false; ModSettings.IsRemotePickupBlinking = false; yield break; }

            yield return new WaitForSeconds(BLINK_DELAY);

            try
            {
                body.AutoPickUpItem(e.item);
                success = true;
            }
            catch { }

            yield return new WaitForSeconds(BLINK_DELAY);

            try
            {
                if (NetPlayer.TryGetLocalNetBody(out var localNb) && localNb != null)
                {
                    localNb.SetBodyPosition(originalPos);
                }
                else
                {
                    body.transform.position = originalPos;
                    if (body.rb != null) body.rb.position = originalPos;
                }
                ClientMain.Client_SendCharacterSyncPacket();
            }
            catch { }

            if (success && PlayerCamera.main != null)
                PlayerCamera.main.DoAlert($"<color=green>拾取 {e.label}</color>", false);

            _isBlinking = false;
            ModSettings.IsRemotePickupBlinking = false;
        }

        // ═══════════════════════════════════════════
        //  工具方法
        // ═══════════════════════════════════════════
        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _onScreenStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            _offScreenArrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            _lblStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow },
            };
        }

        private static string ShortName(Item item)
        {
            if (item == null) return "???";
            string n = item.name ?? item.gameObject?.name ?? "Item";
            int idx = n.IndexOf('(');
            if (idx > 0) n = n.Substring(0, idx).Trim();
            if (n.Length > 6) n = n.Substring(0, 5) + ".";
            return n;
        }

        private static void EdgeFromScreenPos(Vector3 sp, out Vector2 edgePos, out float angleDeg)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Vector2 dir = new Vector2(sp.x - center.x, (Screen.height - sp.y) - center.y);
            angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            const float MARGIN = 40f;
            float hw = Screen.width * 0.5f - MARGIN;
            float hh = Screen.height * 0.5f - MARGIN;

            if (Mathf.Approximately(dir.x, 0f) && Mathf.Approximately(dir.y, 0f))
            {
                edgePos = center;
                return;
            }

            float tx = dir.x != 0f ? hw / Mathf.Abs(dir.x) : float.MaxValue;
            float ty = dir.y != 0f ? hh / Mathf.Abs(dir.y) : float.MaxValue;
            float t = Mathf.Min(tx, ty);
            edgePos = center + dir * t;
        }
    }
}