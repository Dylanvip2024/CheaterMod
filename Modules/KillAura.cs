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
        public override void OnEnable()  => ModSettings.IsKillAuraEnabled = true;
        public override void OnDisable() => ModSettings.IsKillAuraEnabled = false;

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
            if (ModSettings.KillAuraRenderTarget)
                h += 28f;    // 颜色选择器
            h += 28f;        // 传送攻击
            if (ModSettings.KillAuraTeleportAttack)
                h += 28f;    // 传送范围
            return h;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            CustomGUI.DrawToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_players"), ref ModSettings.KillAuraAttackPlayers);
            CustomGUI.DrawToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_creatures"), ref ModSettings.KillAuraAttackCreatures);
            CustomGUI.DrawToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_glass"), ref ModSettings.KillAuraAttackGlass);

            float aps = ModSettings.KillAuraAPS;
            CustomGUI.DrawSlider(x, ref y, width, e, Utils.I18n.Get("set_ka_aps") + $": {aps:F0}/s", ref aps, 1f, 20f);
            ModSettings.KillAuraAPS = aps;

            float range = ModSettings.KillAuraRange;
            CustomGUI.DrawSlider(x, ref y, width, e, Utils.I18n.Get("set_ka_range") + $": {range:F1}m", ref range, 1f, 10f);
            ModSettings.KillAuraRange = range;

            CustomGUI.DrawToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_render_target"), ref ModSettings.KillAuraRenderTarget);

            if (ModSettings.KillAuraRenderTarget)
                CustomGUI.DrawColorPicker(x, ref y, width, e, Utils.I18n.Get("set_ka_color") + ":", ref ModSettings.KillAuraTargetColor);

            CustomGUI.DrawToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_tp_attack"), ref ModSettings.KillAuraTeleportAttack);

            if (ModSettings.KillAuraTeleportAttack)
            {
                float tpRange = ModSettings.KillAuraTeleportRange;
                CustomGUI.DrawSlider(x, ref y, width, e, Utils.I18n.Get("set_ka_tp_range") + $": {tpRange:F0}m", ref tpRange, 10f, 100f);
                ModSettings.KillAuraTeleportRange = tpRange;
            }
        }

        public override void OnUpdate()
        {
            if (!ModSettings.IsKillAuraEnabled) return;
            var body = PlayerCamera.main?.body;
            if (body == null || !body.conscious) return;

            _attackTimer += Time.deltaTime;
            float interval = 1f / ModSettings.KillAuraAPS;
            if (_attackTimer < interval) return;
            _attackTimer = 0f;

            _currentTarget = FindClosestTarget(body);
            if (_currentTarget == null || !_currentTarget) return;

            // ★ 原版重复代码块已移除 —— 只保留一次攻击判定
            if (ModSettings.KillAuraTeleportAttack)
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
                // ★ 7.0.1 安全反射：attackCooldown（原名已验证存在），带类型盲测回退
                if (_cooldownField == null)
                {
                    _cooldownField = typeof(Body).GetField("attackCooldown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (_cooldownField == null)
                    {
                        foreach (var f in typeof(Body).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            if (f.FieldType == typeof(float) && f.Name.ToLower().Contains("attack") && f.Name.ToLower().Contains("cooldown"))
                            {
                                _cooldownField = f;
                                break;
                            }
                        }
                    }
                }
                if (_useItemMethod == null)
                {
                    _useItemMethod = typeof(Body).GetMethod("UseItemInHand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (_useItemMethod == null)
                    {
                        foreach (var m in typeof(Body).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            if (m.Name.ToLower().Contains("use") && m.Name.ToLower().Contains("item") && m.Name.ToLower().Contains("hand") && m.GetParameters().Length == 0)
                            {
                                _useItemMethod = m;
                                break;
                            }
                        }
                    }
                }

                Vector3 backupPos = body.transform.position;
                Vector3 backupLook = body.targetLookPos;

                // ★ 使用碰撞箱边缘最近点作为目标基准
                Vector3 targetEdge = GetClosestPointOnTarget(_currentTarget, backupPos);
                Vector3 dir = (backupPos - targetEdge).normalized;
                if (dir == Vector3.zero) dir = Vector3.up;

                bool isGlass = _currentTarget.TryGetComponent<GroundGlass>(out var _);

                // 玻璃碴贴地放置，需要更近距离 + 瞄准精确位置避免射线穿过
                float offset = isGlass ? 0.8f : 1.5f;
                body.transform.position = targetEdge + dir * offset;
                // 玻璃碴瞄准中心点（不加 0.5f 偏移），其他目标瞄准胸部
                body.targetLookPos = isGlass
                    ? _currentTarget.position
                    : targetEdge + Vector3.up * 0.5f;

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

            // ★ 预计算目标碰撞箱边缘最近点
            Vector3 targetEdge = GetClosestPointOnTarget(target, originalPos);

            try
            {
                float dist = Vector2.Distance(body.transform.position, targetEdge);
                if (dist > ModSettings.KillAuraTeleportRange)
                {
                    _isBlinking = false;
                    yield break;
                }

                // ★ 使用边缘点作为传送目标，保持安全间距
                Vector3 safePos = TPHelper.GetSafeTeleportPosition(originalPos, targetEdge, 1.2f);
                body.transform.position = safePos;

                bool isGlass = target.TryGetComponent<GroundGlass>(out var _);
                body.targetLookPos = isGlass
                    ? target.position
                    : targetEdge + Vector3.up * 0.5f;
                ClientMain.Client_SendCharacterSyncPacket();
            }
            catch { error = true; }

            if (error) { _isBlinking = false; yield break; }

            // ⑤ 等服务器确认坐标
            yield return new WaitForSeconds(0.12f);

            try
            {
                // ★ 复用 DoBlinkStrike 中已强化的反射查找
                if (_cooldownField == null)
                {
                    _cooldownField = typeof(Body).GetField("attackCooldown", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (_cooldownField == null)
                    {
                        foreach (var f in typeof(Body).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            if (f.FieldType == typeof(float) && f.Name.ToLower().Contains("attack") && f.Name.ToLower().Contains("cooldown"))
                            { _cooldownField = f; break; }
                        }
                    }
                }
                if (_useItemMethod == null)
                {
                    _useItemMethod = typeof(Body).GetMethod("UseItemInHand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (_useItemMethod == null)
                    {
                        foreach (var m in typeof(Body).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            if (m.Name.ToLower().Contains("use") && m.Name.ToLower().Contains("item") && m.Name.ToLower().Contains("hand") && m.GetParameters().Length == 0)
                            { _useItemMethod = m; break; }
                        }
                    }
                }

                // ★ 核心修复：在攻击前一瞬间，再次强行覆盖准星！
                // 因为等待的 0.12 秒内，玩家鼠标滑动会改变 targetLookPos 导致空挥
                if (target != null)
                {
                    bool isGlass2 = target.TryGetComponent<GroundGlass>(out var _);
                    body.targetLookPos = isGlass2
                        ? target.position
                        : targetEdge + Vector3.up * 0.5f;
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
            if (!ModSettings.IsKillAuraEnabled || !ModSettings.KillAuraRenderTarget) return;
            if (_currentTarget == null || !_currentTarget) return;

            try
            {
                var cam = Camera.main;
                if (cam == null) return;
                RenderUtils.DrawWorldCircle(cam, _currentTarget.position, 0.8f, ModSettings.KillAuraTargetColor, 24);
            }
            catch { }
        }

        private Transform FindClosestTarget(Body myBody)
        {
            float minDist = ModSettings.KillAuraTeleportAttack
                ? ModSettings.KillAuraTeleportRange
                : ModSettings.KillAuraRange;
            Transform best = null;
            Vector2 myPos = myBody.transform.position;

            // ★ 性能优化：使用 ESPCache.CachedTraders 替代高频 Object.FindObjectsOfType<TraderScript>()
            var traderRoots = new System.Collections.Generic.HashSet<Transform>();
            var traders = ESPCache.CachedTraders;
            if (traders != null)
            {
                foreach (var t in traders)
                {
                    if (t != null && t)
                        traderRoots.Add(t.transform);
                }
            }

            if (ESPCache.CachedEntities != null && ModSettings.KillAuraAttackCreatures)
            {
                foreach (var entity in ESPCache.CachedEntities)
                {
                    if (entity == null || !entity) continue;
                    if (!entity.animal) continue;
                    if (entity.health < 0.5f) continue;
                    if (entity.transform.position == myBody.transform.position) continue;

                    // ★ 跳过商人
                    if (IsTraderTransform(entity.transform, traderRoots)) continue;

                    // ★ 使用碰撞箱边缘最近点计算距离
                    Vector3 closest = GetClosestPointOnTarget(entity.transform, myBody.transform.position);
                    float dist = Vector2.Distance(myPos, (Vector2)closest);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        best = entity.transform;
                    }
                }
            }

            if (ModSettings.KillAuraAttackPlayers)
            {
                // ★ 性能优化：使用 ESPCache.CachedPlayers 替代高频 Object.FindObjectsOfType<Body>()
                var allBodies = ESPCache.CachedPlayers;
                if (allBodies != null)
                {
                    foreach (var targetBody in allBodies)
                    {
                        if (targetBody == null || !targetBody) continue;
                        if (targetBody == myBody) continue;
                        if (!targetBody.alive) continue;

                        // ★ 跳过商人
                        if (IsTraderTransform(targetBody.transform, traderRoots)) continue;

                        // ★ 使用碰撞箱边缘最近点计算距离
                        Vector3 closest = GetClosestPointOnTarget(targetBody.transform, myBody.transform.position);
                        float dist = Vector2.Distance(myPos, (Vector2)closest);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            best = targetBody.transform;
                        }
                    }
                }
            }

            // ★ 玻璃碴
            if (ModSettings.KillAuraAttackGlass && ESPCache.CachedGlassShards != null)
            {
                foreach (var glass in ESPCache.CachedGlassShards)
                {
                    if (glass == null || !glass || !glass.gameObject.activeInHierarchy) continue;
                    var build = glass.GetComponent<BuildingEntity>();
                    if (build == null) continue;
                    if (build.health <= 0f) continue;

                    Vector3 closest = GetClosestPointOnTarget(glass.transform, myBody.transform.position);
                    float dist = Vector2.Distance(myPos, (Vector2)closest);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        best = glass.transform;
                    }
                }
            }

            return best;
        }

        /// <summary>判断 Transform 是否属于商人（匹配根 Transform 或父子级）</summary>
        private static bool IsTraderTransform(Transform t, System.Collections.Generic.HashSet<Transform> traderRoots)
        {
            if (traderRoots.Contains(t)) return true;
            Transform parent = t.parent;
            while (parent != null)
            {
                if (traderRoots.Contains(parent)) return true;
                parent = parent.parent;
            }
            return false;
        }

        /// <summary>获取目标碰撞箱边缘最近点，无 Collider 则降级为 transform.position</summary>
        private static Vector3 GetClosestPointOnTarget(Transform target, Vector3 fromPos)
        {
            if (target == null) return Vector3.zero;
            var col = target.GetComponentInChildren<Collider2D>();
            if (col != null)
            {
                try { return col.ClosestPoint(fromPos); }
                catch { }
            }
            return target.position;
        }
    }
}
