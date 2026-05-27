using FullBrightMod.Core;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using System.Reflection;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// 杀戮光环 (KillAura) —— 瞬移打击 (Blink Strike) 终极安全版
    /// 彻底舍弃高危的 Harmony 参数篡改，采用单帧瞬移欺骗机制，完美绕过攻击范围限制！
    /// </summary>
    public class KillAura : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_killaura") ?? "杀戮光环";
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable()  => Settings.IsKillAuraEnabled = true;
        public override void OnDisable() => Settings.IsKillAuraEnabled = false;

        private float _attackTimer;
        private BuildingEntity _currentTarget;

        // 缓存底层反射，避免 Traverse 造成 GC 内存雪崩
        private FieldInfo _cooldownField;
        private MethodInfo _useItemMethod;

        public override float GetSettingsHeight()
        {
            float h = 30f;   // 攻击玩家
            h += 28f;        // 攻速
            h += 28f;        // 范围
            h += 28f;        // 目标渲染
            if (Settings.KillAuraRenderTarget)
                h += 28f;    // 颜色选择器
            return h;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            DrawCustomToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_players") ?? "攻击玩家", ref Settings.KillAuraAttackPlayers);

            float aps = Settings.KillAuraAPS;
            DrawCustomSlider(x, ref y, width, e, (Utils.I18n.Get("set_ka_aps") ?? "攻击速度") + $": {aps:F0}/s", ref aps, 1f, 20f);
            Settings.KillAuraAPS = aps;

            float range = Settings.KillAuraRange;
            DrawCustomSlider(x, ref y, width, e, (Utils.I18n.Get("set_ka_range") ?? "攻击范围") + $": {range:F1}m", ref range, 1f, 10f);
            Settings.KillAuraRange = range;

            DrawCustomToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_render_target") ?? "渲染目标", ref Settings.KillAuraRenderTarget);

            if (Settings.KillAuraRenderTarget)
            {
                ItemESP.DrawColorPicker(x, ref y, width, e, (Utils.I18n.Get("set_ka_color") ?? "目标颜色") + ":", ref Settings.KillAuraTargetColor);
            }
        }

        public override void OnUpdate()
        {
            if (!Settings.IsKillAuraEnabled) return;
            var body = PlayerCamera.main?.body;
            if (body == null || !body.conscious) return;

            _attackTimer += Time.deltaTime;
            float interval = 1f / Settings.KillAuraAPS;
            if (_attackTimer < interval) return;
            _attackTimer = 0f;

            _currentTarget = FindClosestTarget(body);
            if (_currentTarget == null || !_currentTarget || _currentTarget.gameObject == null) return;

            try
            {
                // 初始化底层反射方法
                if (_cooldownField == null)
                    _cooldownField = typeof(Body).GetField("attackCooldown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (_useItemMethod == null)
                    _useItemMethod = typeof(Body).GetMethod("UseItemInHand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                // 备份当前的坐标和视角
                Vector3 backupPos = body.transform.position;
                Vector3 backupLook = body.targetLookPos;

                // 计算玩家朝向怪物的方向
                Vector3 dir = (backupPos - _currentTarget.transform.position).normalized;
                if (dir == Vector3.zero) dir = Vector3.up;

                // ========================================================
                // ✨ 瞬移打击 (Blink Strike) 核心欺骗机制 ✨
                // 瞬间将玩家传送到怪物脸上 (距离 1.0 米)，强行进入原版武器短手判定区
                // ========================================================
                body.transform.position = _currentTarget.transform.position + dir * 1.0f;
                body.targetLookPos = _currentTarget.transform.position;

                // 清空原版武器攻击后摇
                if (_cooldownField != null) _cooldownField.SetValue(body, 0f);
                
                // 执行挥刀
                if (_useItemMethod != null) _useItemMethod.Invoke(body, null);

                // ========================================================
                // ✨ 瞬间归位 ✨
                // 由于都在同一帧完成，画面不会发生任何闪烁，物理引擎也不会察觉！
                // ========================================================
                body.transform.position = backupPos;
                body.targetLookPos = backupLook;
            }
            catch { /* 屏蔽攻击中的极小概率异常 */ }
        }

        public override void OnGUI()
        {
            if (!Settings.IsKillAuraEnabled || !Settings.KillAuraRenderTarget) return;

            if (_currentTarget == null || !_currentTarget || _currentTarget.gameObject == null) return;

            try
            {
                var cam = Camera.main;
                if (cam == null) return;

                RenderUtils.DrawWorldCircle(cam, _currentTarget.transform.position, 0.8f, Settings.KillAuraTargetColor, 24);
            }
            catch { }
        }

        private BuildingEntity FindClosestTarget(Body body)
        {
            if (ESPCache.CachedEntities == null) return null;

            float minDist = Settings.KillAuraRange;
            BuildingEntity best = null;
            Vector2 myPos = body.transform.position;

            foreach (var entity in ESPCache.CachedEntities)
            {
                if (entity == null || !entity) continue;
                if (entity.health < 0.5f) continue;
                if (entity.transform.position == body.transform.position) continue;

                if (!entity.animal && !Settings.KillAuraAttackPlayers) continue;

                float dist = Vector2.Distance(myPos, entity.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = entity;
                }
            }
            return best;
        }

        private static void DrawCustomToggle(float x, ref float y, float width, Event e, string label, ref bool value)
        {
            Rect rowRect = new Rect(x, y, width, 24f);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? new Color(0.2f, 0.6f, 1.0f, 0.85f)
                : (hover ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(rowRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;

            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal = { textColor = value ? Color.white : new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(14, 0, 4, 0)
            };
            GUI.Label(rowRect, (value ? "[ON]  " : "[OFF] ") + label, s);

            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                value = !value;
                e.Use();
            }
            y += 28f;
        }

        private static void DrawCustomSlider(float x, ref float y, float width, Event e, string label, ref float value, float min, float max)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }, padding = new RectOffset(14, 0, 2, 0)
            };
            GUI.Label(new Rect(x, y, width, 18f), label, labelStyle);

            Rect sliderRect = new Rect(x + 14f, y + 18f, width - 28f, 8f);
            float newVal = ClickGUIManager.DrawSlider(sliderRect, value, min, max, e);
            if (Mathf.Abs(newVal - value) > 0.001f) value = newVal;

            y += 28f;
        }
    }
}