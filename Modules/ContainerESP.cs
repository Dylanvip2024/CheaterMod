using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// ContainerESP — 箱子指示器
    /// 通过 BuildingEntity.id 匹配箱子类型（物资箱、医疗箱、食物盒、空投箱）。
    /// 支持两种渲染模式：
    ///   - 线条模式：从屏幕中心画 Tracer Line 指向箱子位置，显示名称和距离
    ///   - 箭头模式：屏幕外箱子在屏幕边缘显示旋转箭头，屏幕内箱子不渲染
    /// </summary>
    public class ContainerESP : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_container_esp") ?? "箱子指示器";
        public override ModuleCategory Category => ModuleCategory.Render;

        private float _scanCooldown;
        private const float SCAN_INTERVAL = 1.5f;

        private struct Entry
        {
            public Vector3 worldPos;
            public string label;
        }
        private List<Entry> _entries = new List<Entry>();

        private GUIStyle _lineLabelStyle;
        private GUIStyle _arrowStyle;
        private GUIStyle _arrowLabelStyle;
        private bool _styleReady;

        public override void OnEnable()  => Settings.IsContainerEspEnabled = true;
        public override void OnDisable() => Settings.IsContainerEspEnabled = false;

        public override float GetSettingsHeight()
        {
            float h = 0f;
            h += 28f; // 范围滑块
            h += 26f; // 颜色
            h += 28f; // 模式切换按钮
            return h;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // ── 范围滑块 ──
            float range = Settings.ContainerEspRange;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(14, 0, 2, 0)
            };
            GUI.Label(new Rect(x, y, width, 18),
                $"{(Utils.I18n.Get("set_container_range") ?? "探测范围")}: {range:F0}m", labelStyle);

            Rect barRect = new Rect(x + 14, y + 18, width - 28, 8);
            float newRange = ClickGUIManager.DrawSlider(barRect, range, 5f, 200f, e);
            if (Mathf.Abs(newRange - range) > 0.001f) Settings.ContainerEspRange = newRange;
            y += 28f;

            // ── 颜色选择器 ──
            ItemESP.DrawColorPicker(x, ref y, width, e,
                (Utils.I18n.Get("set_container_color") ?? "箱子颜色") + ":",
                ref Settings.ContainerEspColor);

            // ── 模式切换按钮 ──
            string modeLabel = (Utils.I18n.Get("container_esp_mode") ?? "渲染模式") + ": "
                + (Settings.ContainerEspUseArrows
                    ? (Utils.I18n.Get("mode_arrows") ?? "箭头")
                    : (Utils.I18n.Get("mode_lines") ?? "线条"));
            Rect btnRect = new Rect(x + 8, y, width - 16, 24);

            bool hover = btnRect.Contains(e.mousePosition);
            GUI.color = Settings.ContainerEspUseArrows
                ? new Color(0.8f, 0.5f, 0.2f, 0.85f)
                : (hover ? new Color(0.35f, 0.35f, 0.4f, 0.9f) : new Color(0.25f, 0.25f, 0.3f, 0.9f));
            GUI.DrawTexture(btnRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(btnRect, modeLabel, btnStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                Settings.ContainerEspUseArrows = !Settings.ContainerEspUseArrows;
                e.Use();
            }
            y += 28f;
        }

        public override void OnUpdate()
        {
            if (!Settings.IsContainerEspEnabled) return;

            _scanCooldown -= Time.deltaTime;
            if (_scanCooldown > 0f) return;
            _scanCooldown = SCAN_INTERVAL;

            RefreshEntries();
        }

        public override void OnGUI()
        {
            if (!Enabled || !Settings.IsContainerEspEnabled) return;

            var cam = Camera.main;
            var body = PlayerCamera.main?.body;
            if (cam == null || body == null) return;
            if (!body.conscious) return;
            EnsureStyle();

            Vector2 myPos = body.transform.position;
            float maxRange = Settings.ContainerEspRange;
            Color col = Settings.ContainerEspColor;
            bool useArrows = Settings.ContainerEspUseArrows;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                float dist = Vector2.Distance(myPos, e.worldPos);
                if (dist > maxRange) continue;

                Vector3 sp = cam.WorldToScreenPoint(e.worldPos);
                if (sp.z <= 0) continue;

                bool onScreen = sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;

                if (useArrows)
                {
                    // 箭头模式：仅渲染屏幕外的箱子
                    if (onScreen) continue;
                    DrawOffscreenArrow(sp, e.label, dist, col);
                }
                else
                {
                    // 线条模式：所有箱子都画线
                    Vector2 guiPos = new Vector2(sp.x, Screen.height - sp.y);
                    RenderUtils.DrawTracerLine(guiPos, col, 1.5f);

                    string text = $"{e.label} {dist:F0}m";
                    Vector2 labelSize = _lineLabelStyle.CalcSize(new GUIContent(text));
                    Rect labelRect = new Rect(guiPos.x - labelSize.x * 0.5f, guiPos.y - 22, labelSize.x + 8, 20);
                    GUI.Label(labelRect, text, _lineLabelStyle);
                }
            }
        }

        // ═══════════════════════════════════════════
        //  箭头模式：屏幕外边缘指向箭头
        // ═══════════════════════════════════════════
        private void DrawOffscreenArrow(Vector3 sp, string label, float dist, Color col)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = new Vector2(sp.x - center.x, (Screen.height - sp.y) - center.y);
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            const float MARGIN = 50f;
            float hw = Screen.width * 0.5f - MARGIN;
            float hh = Screen.height * 0.5f - MARGIN;

            Vector2 edge;
            if (Mathf.Approximately(dir.x, 0f) && Mathf.Approximately(dir.y, 0f))
                edge = center;
            else
            {
                float tx = dir.x != 0f ? hw / Mathf.Abs(dir.x) : float.MaxValue;
                float ty = dir.y != 0f ? hh / Mathf.Abs(dir.y) : float.MaxValue;
                edge = center + dir * Mathf.Min(tx, ty);
            }

            const float AW = 40f, AH = 40f;
            Rect arrowRect = new Rect(edge.x - AW * 0.5f, edge.y - AH * 0.5f, AW, AH);

            _arrowStyle.normal.textColor = col;

            Matrix4x4 backup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angleDeg, arrowRect.center);
            GUI.Label(arrowRect, "➤", _arrowStyle);
            GUI.matrix = backup;

            // 距离标签
            GUI.Label(new Rect(edge.x - 40, edge.y + 15, 80, 14), $"{label} {dist:F0}m", _arrowLabelStyle);
        }

        // ═══════════════════════════════════════════
        //  扫描 BuildingEntity，通过 id 字段匹配箱子类型
        // ═══════════════════════════════════════════
        private static readonly HashSet<string> ContainerIds = new HashSet<string>
        {
            "medcrate",         // 医疗箱
            "containercrate",   // 物资箱
            "foodbox",          // 食物盒
            "dropcapsule",       // 空投箱
        };

        private void RefreshEntries()
        {
            _entries.Clear();
            var allBuildings = Object.FindObjectsOfType<BuildingEntity>();
            if (allBuildings == null) return;

            foreach (var be in allBuildings)
            {
                if (be == null || !be || !be.gameObject.activeInHierarchy) continue;
                if (be.health <= 0f) continue; // 已损坏的不显示

                // ID 精确匹配
                if (string.IsNullOrEmpty(be.id)) continue;
                if (!ContainerIds.Contains(be.id)) continue;

                string label = be.fullName ?? be.name ?? be.id;

                _entries.Add(new Entry
                {
                    worldPos = be.transform.position,
                    label = label,
                });
            }
        }

        private void EnsureStyle()
        {
            if (_styleReady) return;
            _styleReady = true;

            _lineLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Settings.ContainerEspColor },
            };

            _arrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            _arrowLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow },
            };
        }
    }
}
