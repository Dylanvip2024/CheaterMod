using System.Collections.Generic;
using FullBrightMod.Core;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// SoundCannonIndicator（声波炮指示器）—— 实时检测地图上的声波炮，
    /// 仅在声波炮处于"蓄力中"状态时，绘制从玩家位置到炮台位置的粗连线。
    /// 蓄力结束并开炮后（spent=true）线条自动消失。
    /// 
    /// 蓄力状态检测：
    ///   多人模式：读取 KrokoshaSoundCannonNetworkTrackerComponent 的 public 字段
    ///   单人模式：通过 Reflection 读取 SoundCannon 的 private 字段
    /// 
    /// 颜色规则：
    ///   红色 = 玩家暴露（任意肢体未被掩体遮挡）
    ///   绿色 = 玩家安全（所有肢体均被墙体完全遮挡）
    /// </summary>
    public class SoundCannonIndicator : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_sound_cannon_indicator") ?? "声波炮指示器";
        public override ModuleCategory Category => ModuleCategory.Render;

        public override void OnEnable()  => ModSettings.IsSoundCannonIndicatorEnabled = true;
        public override void OnDisable() => ModSettings.IsSoundCannonIndicatorEnabled = false;

        // ═══════════════════════════════════════════════════
        //  纹理（粗线绘制用）
        // ═══════════════════════════════════════════════════
        private static Texture2D _whiteTex;
        private static Texture2D WhiteTex
        {
            get
            {
                if (_whiteTex == null)
                {
                    _whiteTex = new Texture2D(1, 1);
                    _whiteTex.SetPixel(0, 0, Color.white);
                    _whiteTex.Apply();
                }
                return _whiteTex;
            }
        }

        // ═══════════════════════════════════════════════════
        //  扫描状态
        // ═══════════════════════════════════════════════════
        private SoundCannon[] _cachedCannons;
        private float _scanTimer;
        private const float SCAN_INTERVAL = 0.05f;
        private int _groundMask = -1;

        // ═══════════════════════════════════════════════════
        //  渲染缓存
        // ═══════════════════════════════════════════════════
        private struct Line { public Vector2 a, b; public Color c; }
        private readonly List<Line> _lines = new List<Line>();

        /// <summary>连线粗细</summary>
        private const float LINE_THICKNESS = 5f;

        private static readonly Color ExposedColor = new Color(1f, 0.15f, 0.15f, 0.9f);
        private static readonly Color SafeColor    = new Color(0.15f, 1f, 0.2f, 0.9f);

        // ═══════════════════════════════════════════════════
        //  单机模式 Reflection 缓存（SoundCannon 的 private 字段）
        // ═══════════════════════════════════════════════════
        private static System.Reflection.FieldInfo _chargingField;
        private static System.Reflection.FieldInfo _spentField;

        private static bool GetPrivateCharging(SoundCannon cannon)
        {
            if (_chargingField == null)
                _chargingField = typeof(SoundCannon).GetField("charging",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return _chargingField != null && (bool)_chargingField.GetValue(cannon);
        }

        private static bool GetPrivateSpent(SoundCannon cannon)
        {
            if (_spentField == null)
                _spentField = typeof(SoundCannon).GetField("spent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return _spentField != null && (bool)_spentField.GetValue(cannon);
        }

        // ═══════════════════════════════════════════════════
        //  OnUpdate：高频扫描，仅处理处于蓄力中的声波炮
        // ═══════════════════════════════════════════════════
        public override void OnUpdate()
        {
            if (!Enabled || !ModSettings.IsSoundCannonIndicatorEnabled) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = SCAN_INTERVAL;

            var body = PlayerCamera.main?.body;
            if (body == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            // 直接扫描全部 SoundCannon
            _cachedCannons = Object.FindObjectsOfType<SoundCannon>();
            if (_cachedCannons == null || _cachedCannons.Length == 0)
                _cachedCannons = Resources.FindObjectsOfTypeAll<SoundCannon>();

            if (_cachedCannons == null || _cachedCannons.Length == 0) return;

            if (_groundMask == -1)
                _groundMask = LayerMask.GetMask("Ground");

            Vector2 playerPos = body.transform.position;
            _lines.Clear();

            foreach (var sc in _cachedCannons)
            {
                if (sc == null || !sc) continue;
                if (!sc.gameObject.activeInHierarchy) continue;

                // ── 蓄力状态检测 ──
                bool isCharging = false;
                bool isSpent = false;

                // 多人模式：读取 KrokoshaSoundCannonNetworkTrackerComponent 的 public 字段
                var tracker = sc.GetComponent<KrokoshaSoundCannonNetworkTrackerComponent>();
                if (tracker != null)
                {
                    isCharging = tracker.charging;
                    isSpent = tracker.spent;
                }
                else
                {
                    // 单人模式：Reflection 读取 SoundCannon 的 private 字段
                    isCharging = GetPrivateCharging(sc);
                    isSpent = GetPrivateSpent(sc);
                }

                // 只显示正在蓄力中、尚未发射的声波炮
                if (!isCharging || isSpent) continue;

                Vector2 cannonPos = sc.transform.position;

                // 世界坐标 → 屏幕坐标
                Vector3 ps = cam.WorldToScreenPoint((Vector3)playerPos);
                Vector3 cs = cam.WorldToScreenPoint((Vector3)cannonPos);
                if (ps.z <= 0f || cs.z <= 0f) continue;

                Vector2 pGui = new Vector2(ps.x, Screen.height - ps.y);
                Vector2 cGui = new Vector2(cs.x, Screen.height - cs.y);

                // ★ 肢体级暴露判定：遍历所有 limb，任意无遮挡 → 红色
                bool exposed = IsAnyLimbExposed(sc, body, cannonPos);
                Color color = exposed ? ExposedColor : SafeColor;

                _lines.Add(new Line { a = pGui, b = cGui, c = color });
            }
        }

        // ═══════════════════════════════════════════════════
        //  OnGUI：绘制粗线
        // ═══════════════════════════════════════════════════
        public override void OnGUI()
        {
            if (!Enabled || _lines.Count == 0) return;
            for (int i = 0; i < _lines.Count; i++)
                DrawLine(_lines[i].a, _lines[i].b, _lines[i].c, LINE_THICKNESS);
        }

        // ═══════════════════════════════════════════════════
        //  肢体暴露判定（核心）
        //    从炮台向每个 limb.transform.position 发射 LinecastAll，
        //    排除炮台自身 Collider，任意肢体无 Ground 遮挡 → 判定暴露。
        // ═══════════════════════════════════════════════════
        private bool IsAnyLimbExposed(SoundCannon cannon, Body body, Vector2 cannonPos)
        {
            Limb[] limbs = body.limbs;
            if (limbs == null) return false;

            for (int i = 0; i < limbs.Length; i++)
            {
                Limb limb = limbs[i];
                if (limb == null) continue;

                Vector2 limbPos = limb.transform.position;
                RaycastHit2D[] hits = Physics2D.LinecastAll(cannonPos, limbPos, _groundMask);

                bool blocked = false;
                foreach (var hit in hits)
                {
                    // 排除炮台自身的 Collider
                    if (hit.transform == cannon.transform) continue;
                    blocked = true;
                    break;
                }

                // 任意一个肢体无遮挡 → 立即返回暴露
                if (!blocked) return true;
            }

            return false;
        }

        // ═══════════════════════════════════════════════════
        //  屏幕空间粗线绘制
        // ═══════════════════════════════════════════════════
        private static void DrawLine(Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 d = b - a;
            if (d.magnitude < 0.5f) return;

            float angle = Mathf.Rad2Deg * Mathf.Atan2(d.y, d.x);
            float len = d.magnitude;

            Matrix4x4 bk = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, a);
            Color cb = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, len, thickness), WhiteTex);
            GUI.color = cb;
            GUI.matrix = bk;
        }
    }
}
