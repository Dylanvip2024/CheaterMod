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
            h += 28f;        // 优先锁头
            h += 28f;        // 指示器样式
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

            CustomGUI.DrawToggle(x, ref y, width, e, Utils.I18n.Get("set_ka_headshot"), ref ModSettings.KillAuraPrioritizeHead);

            // 目标指示器样式循环选择
            string[] indicatorOptions = { "Circle", "Crosshair", "Arrow", "Diamond", "Pulse" };
            int styleIdx = ModSettings.KillAuraIndicatorStyle;
            DrawIndicatorCycle(x, ref y, width, e, Utils.I18n.Get("set_ka_indicator") ?? "Indicator", indicatorOptions, ref styleIdx);
            ModSettings.KillAuraIndicatorStyle = styleIdx;
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

                // ★ 计算目标视角落点（优先锁头）
                Vector3 aimPoint = GetAimPoint(_currentTarget, targetEdge);
                body.targetLookPos = isGlass ? _currentTarget.position : aimPoint;

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
                Vector3 aimPoint = GetAimPoint(target, targetEdge);
                body.targetLookPos = isGlass ? target.position : aimPoint;
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
                    Vector3 aimPoint = GetAimPoint(target, targetEdge);
                    body.targetLookPos = isGlass2 ? target.position : aimPoint;
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
                Vector3 targetPos = _currentTarget.position;
                Color color = ModSettings.KillAuraTargetColor;

                switch (ModSettings.KillAuraIndicatorStyle)
                {
                    case 0: // Circle
                        RenderUtils.DrawWorldCircle(cam, targetPos, 0.8f, color, 24);
                        break;
                    case 1: // Crosshair
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.left * 0.8f, targetPos + Vector3.right * 0.8f, color);
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.up * 0.8f, targetPos + Vector3.down * 0.8f, color);
                        break;
                    case 2: // Arrow
                        RenderUtils.DrawWorldLine(cam, targetPos, targetPos + Vector3.up * 1.2f, color);
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.up * 1.2f, targetPos + Vector3.up * 0.8f + Vector3.left * 0.4f, color);
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.up * 1.2f, targetPos + Vector3.up * 0.8f + Vector3.right * 0.4f, color);
                        break;
                    case 3: // Diamond
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.up * 0.8f, targetPos + Vector3.right * 0.8f, color);
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.right * 0.8f, targetPos + Vector3.down * 0.8f, color);
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.down * 0.8f, targetPos + Vector3.left * 0.8f, color);
                        RenderUtils.DrawWorldLine(cam, targetPos + Vector3.left * 0.8f, targetPos + Vector3.up * 0.8f, color);
                        break;
                    case 4: // Pulse
                        float pulse = Mathf.Sin(Time.time * 4f) * 0.3f + 0.7f;
                        RenderUtils.DrawWorldCircle(cam, targetPos, 0.5f + pulse * 0.5f, color, 24);
                        RenderUtils.DrawWorldCircle(cam, targetPos, 0.3f, color * 0.5f, 16);
                        break;
                }
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

        /// <summary>循环选择按钮（用于指示器样式）</summary>
        private static void DrawIndicatorCycle(float x, ref float y, float width, Event e, string label, string[] options, ref int index)
        {
            Rect btnRect = new Rect(x + 8, y + 2, width - 16, 22);
            string display = label + ": " + options[System.Math.Max(0, System.Math.Min(options.Length - 1, index))];
            bool hover = btnRect.Contains(e.mousePosition);
            GUI.color = new Color(0.2f, 0.5f, 0.3f, 0.85f);
            if (hover) GUI.color = new Color(0.3f, 0.6f, 0.4f, 0.9f);
            GUI.DrawTexture(btnRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            GUI.Label(btnRect, display, btnStyle);
            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition)) { index = (index + 1) % options.Length; e.Use(); }
            y += 28f;
        }

        /// <summary>
        /// ★ 计算攻击瞄点。
        /// 当 KillAuraPrioritizeHead 开启且目标为玩家（有 Body 组件）时，返回头部 limbs[0] 坐标。
        /// 其他情况返回胸部位置（targetEdge + 0.5f Y）。
        /// </summary>
        private static Vector3 GetAimPoint(Transform targetTransform, Vector3 targetEdge)
        {
            if (ModSettings.KillAuraPrioritizeHead && targetTransform.TryGetComponent<Body>(out var targetBody))
            {
                if (targetBody.limbs != null && targetBody.limbs.Length > 0 && targetBody.limbs[0] != null && !targetBody.limbs[0].dismembered)
                    return targetBody.limbs[0].transform.position;
            }
            // 非玩家或头部已截肢 → 瞄准胸部
            return targetEdge + Vector3.up * 0.5f;
        }
    }
}
