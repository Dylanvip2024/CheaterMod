using System.Collections;
using System.Reflection;
using FullBrightMod.Core;
using FullBrightMod.Patches;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class KillAura : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_killaura") ?? "杀戮光环";
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable()  => Settings.IsKillAuraEnabled = true;
        public override void OnDisable() => Settings.IsKillAuraEnabled = false;

        private float _attackTimer;
        private Transform _currentTarget;
        private bool _isBlinking;

        private FieldInfo _cooldownField;
        private MethodInfo _useItemMethod;

        public override float GetSettingsHeight()
        {
            float h = 30f;   // 攻击玩家
            h += 28f;       // 攻击生物
            h += 28f;       // 攻击玻璃碴
            h += 28f;        // 攻速
            h += 28f;        // 范围
            h += 28f;        // 目标渲染
            if (Settings.KillAuraRenderTarget)
                h += 28f;    // 颜色选择器
            h += 28f;        // 传送攻击
            if (Settings.KillAuraTeleportAttack)
                h += 28f;    // 传送范围
            return h;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            DrawCustomToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_players") ?? "攻击玩家", ref Settings.KillAuraAttackPlayers);
            DrawCustomToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_creatures") ?? "攻击生物", ref Settings.KillAuraAttackCreatures);
            DrawCustomToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_glass") ?? "攻击玻璃碴", ref Settings.KillAuraAttackGlass);

            float aps = Settings.KillAuraAPS;
            DrawCustomSlider(x, ref y, width, e, (Utils.I18n.Get("set_ka_aps") ?? "攻击速度") + $": {aps:F0}/s", ref aps, 1f, 20f);
            Settings.KillAuraAPS = aps;

            float range = Settings.KillAuraRange;
            DrawCustomSlider(x, ref y, width, e, (Utils.I18n.Get("set_ka_range") ?? "攻击范围") + $": {range:F1}m", ref range, 1f, 10f);
            Settings.KillAuraRange = range;

            DrawCustomToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_render_target") ?? "渲染目标", ref Settings.KillAuraRenderTarget);

            if (Settings.KillAuraRenderTarget)
                ItemESP.DrawColorPicker(x, ref y, width, e, (Utils.I18n.Get("set_ka_color") ?? "目标颜色") + ":", ref Settings.KillAuraTargetColor);

            // 传送攻击
            DrawCustomToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_tp_attack") ?? "传送攻击", ref Settings.KillAuraTeleportAttack);

            if (Settings.KillAuraTeleportAttack)
            {
                float tpRange = Settings.KillAuraTeleportRange;
                DrawCustomSlider(x, ref y, width, e, (Utils.I18n.Get("set_ka_tp_range") ?? "传送最大范围") + $": {tpRange:F0}m", ref tpRange, 10f, 100f);
                Settings.KillAuraTeleportRange = tpRange;
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
            if (_currentTarget == null || !_currentTarget) return;

            if (Settings.KillAuraTeleportAttack)
            {
                // 传送攻击：走协程防 Rubber-banding
                if (!_isBlinking)
                    FullBrightPlugin.Instance.StartCoroutine(TeleportAttackCoroutine(body, _currentTarget));
            }
            else
            {
                DoBlinkStrike(body);
            }

            if (Settings.KillAuraTeleportAttack)
            {
                // 传送攻击：走协程防 Rubber-banding
                if (!_isBlinking)
                    FullBrightPlugin.Instance.StartCoroutine(TeleportAttackCoroutine(body, _currentTarget));
            }
            else
            {
                // 原版瞬移打击
                DoBlinkStrike(body);
            }
        }

        private void DoBlinkStrike(Body body)
        {
            try
            {
                if (_cooldownField == null)
                    _cooldownField = typeof(Body).GetField("attackCooldown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (_useItemMethod == null)
                    _useItemMethod = typeof(Body).GetMethod("UseItemInHand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                Vector3 backupPos = body.transform.position;
                Vector3 backupLook = body.targetLookPos;

                Vector3 dir = (backupPos - _currentTarget.position).normalized;
                if (dir == Vector3.zero) dir = Vector3.up;

                bool isGlass = _currentTarget.TryGetComponent<GroundGlass>(out var _);

                // 玻璃碴贴地放置，需要更近距离 + 瞄准精确位置避免射线穿过
                float offset = isGlass ? 0.8f : 1.5f;
                body.transform.position = _currentTarget.position + dir * offset;
                // 玻璃碴瞄准中心点（不加 0.5f 偏移），其他目标瞄准胸部
                body.targetLookPos = isGlass
                    ? _currentTarget.position
                    : _currentTarget.position + Vector3.up * 0.5f;

                if (_cooldownField != null) _cooldownField.SetValue(body, 0f);
                if (_useItemMethod != null) _useItemMethod.Invoke(body, null);

                body.transform.position = backupPos;
                body.targetLookPos = backupLook;
            }
            catch { }
        }

        private IEnumerator TeleportAttackCoroutine(Body body, Transform target)
        {
            _isBlinking = true;
            Vector3 originalPos = body.transform.position;
            bool error = false;

            try
            {
                float dist = Vector2.Distance(body.transform.position, target.position);
                if (dist > Settings.KillAuraTeleportRange)
                {
                    _isBlinking = false;
                    yield break;
                }

                // 将传送距离设为 1.2f 保持安全间距
                Vector3 safePos = TPHelper.GetSafeTeleportPosition(originalPos, target.position, 1.2f);
                body.transform.position = safePos;

                bool isGlass = target.TryGetComponent<GroundGlass>(out var _);
                body.targetLookPos = isGlass
                    ? target.position
                    : target.position + Vector3.up * 0.5f;
                ClientMain.Client_SendCharacterSyncPacket();
            }
            catch { error = true; }

            if (error) { _isBlinking = false; yield break; }

            // ⑤ 等服务器确认坐标
            yield return new WaitForSeconds(0.12f);

            try
            {
                if (_cooldownField == null)
                    _cooldownField = typeof(Body).GetField("attackCooldown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (_useItemMethod == null)
                    _useItemMethod = typeof(Body).GetMethod("UseItemInHand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                // ★ 核心修复：在攻击前一瞬间，再次强行覆盖准星！
                // 因为等待的 0.12 秒内，玩家鼠标滑动会改变 targetLookPos 导致空挥
                if (target != null)
                {
                    bool isGlass2 = target.TryGetComponent<GroundGlass>(out var _);
                    body.targetLookPos = isGlass2
                        ? target.position
                        : target.position + Vector3.up * 0.5f;
                }

                if (_cooldownField != null) _cooldownField.SetValue(body, 0f);
                if (_useItemMethod != null) _useItemMethod.Invoke(body, null);
            }
            catch { }

            // ⑦ 等攻击包发送
            yield return new WaitForSeconds(0.12f);

            // ⑧ 移回 + 发包
            try
            {
                body.transform.position = originalPos;
                ClientMain.Client_SendCharacterSyncPacket();
            }
            catch { }

            yield return null;
            _isBlinking = false;
        }

        public override void OnGUI()
        {
            if (!Settings.IsKillAuraEnabled || !Settings.KillAuraRenderTarget) return;
            if (_currentTarget == null || !_currentTarget) return;

            try
            {
                var cam = Camera.main;
                if (cam == null) return;
                RenderUtils.DrawWorldCircle(cam, _currentTarget.position, 0.8f, Settings.KillAuraTargetColor, 24);
            }
            catch { }
        }

        private Transform FindClosestTarget(Body myBody)
        {
            float minDist = Settings.KillAuraTeleportAttack
                ? Settings.KillAuraTeleportRange
                : Settings.KillAuraRange;
            Transform best = null;
            Vector2 myPos = myBody.transform.position;

            if (ESPCache.CachedEntities != null && Settings.KillAuraAttackCreatures)
            {
                foreach (var entity in ESPCache.CachedEntities)
                {
                    if (entity == null || !entity) continue;
                    if (!entity.animal) continue;
                    if (entity.health < 0.5f) continue;
                    if (entity.transform.position == myBody.transform.position) continue;

                    float dist = Vector2.Distance(myPos, entity.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        best = entity.transform;
                    }
                }
            }

            if (Settings.KillAuraAttackPlayers)
            {
                var allBodies = Object.FindObjectsOfType<Body>();
                foreach (var targetBody in allBodies)
                {
                    if (targetBody == null || !targetBody) continue;
                    if (targetBody == myBody) continue;
                    if (!targetBody.alive) continue;

                    // ★ 过滤友好 NPC：Body 的父级 GameObject 上才有 BuildingEntity
                    var targetBuild = targetBody.GetComponentInParent<BuildingEntity>();
                    if (targetBuild != null && !targetBuild.cantHit) continue;

                    float dist = Vector2.Distance(myPos, targetBody.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        best = targetBody.transform;
                    }
                }
            }

            // ★ 玻璃碴：GroundGlass 预制体自带 BuildingEntity 组件，可直接攻击
            if (Settings.KillAuraAttackGlass && ESPCache.CachedGlassShards != null)
            {
                foreach (var glass in ESPCache.CachedGlassShards)
                {
                    if (glass == null || !glass || !glass.gameObject.activeInHierarchy) continue;
                    var build = glass.GetComponent<BuildingEntity>();
                    if (build == null) continue;
                    if (build.health <= 0f) continue;

                    float dist = Vector2.Distance(myPos, glass.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        best = glass.transform;
                    }
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
