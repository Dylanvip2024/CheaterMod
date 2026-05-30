using FullBrightMod.Core;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Utils
{
    public static class ESPRenderer
    {
        // =========================================================
        //  动态包围盒测算 —— 基于 Renderer/Collider 实际尺寸
        // =========================================================
        /// <summary>
        /// 读取 GameObject 及其子物体的 Renderer 或 Collider2D 的包围盒，
        /// 返回其在世界空间中的实际尺寸 (width, height)。
        /// 若没有任何有效组件，返回 defaultSize 作为保底。
        /// </summary>
        private static Vector2 GetDynamicEntitySize(GameObject obj, Vector2 defaultSize)
        {
            if (obj == null) return defaultSize;

            // 优先使用 SpriteRenderer（2D 游戏最常见）
            var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds total = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        total.Encapsulate(renderers[i].bounds);
                }

                if (total.size.x > 0.1f || total.size.y > 0.1f)
                    return new Vector2(total.size.x, total.size.y);
            }

            // 没有 Renderer，尝试 Collider2D
            var colliders = obj.GetComponentsInChildren<Collider2D>();
            if (colliders != null && colliders.Length > 0)
            {
                Bounds total = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                        total.Encapsulate(colliders[i].bounds);
                }

                if (total.size.x > 0.1f || total.size.y > 0.1f)
                    return new Vector2(total.size.x, total.size.y);
            }

            // 保底
            return defaultSize;
        }

        // =========================================================
        //  线框模式核心渲染（Tracer + Box + Label）
        // =========================================================
        private static void DrawWireframeFor(Camera cam, Vector3 worldPos, Color color,
            string labelText, Vector2 worldSize)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0) return;

            Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
            float distance = Vector3.Distance(cam.transform.position, worldPos);
            int distMeters = (int)(distance * 0.3f);

            float thickness = Settings.EspLineWidth;

            // 1. Tracer line from screen center
            RenderUtils.DrawTracerLine(guiPos, color, thickness);

            // 2. Box: 正交摄像机下 W2P 恒定，世界尺寸 → 像素 = orthographicSize * 2 → Screen.height
            float pixelsPerUnit = Screen.height / (cam.orthographicSize * 2f);
            Vector2 boxSize = new Vector2(worldSize.x * pixelsPerUnit, worldSize.y * pixelsPerUnit);
            boxSize.x += 10f;
            boxSize.y += 10f;
            RenderUtils.DrawScreenBox(guiPos, boxSize, color, Mathf.Max(thickness, 1f));

            // 3. Info label above the box
            int fontSize = Mathf.Max(Settings.EspFontSize - 2, 10);
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = fontSize,
                richText = true
            };
            style.normal.textColor = color;

            string info = labelText + "\n[ " + distMeters + "m ]";
            Rect labelRect = new Rect(guiPos.x - boxSize.x, guiPos.y - boxSize.y * 0.5f - 40f,
                                       boxSize.x * 2f, 40f);
            Color guiColorBackup = GUI.color;
            GUI.color = Color.white;
            GUI.Label(labelRect, info, style);
            GUI.color = guiColorBackup;
        }

        // =========================================================
        // 物品 ESP
        // =========================================================
        private static readonly Vector2 DefaultItemSize = new Vector2(0.6f, 0.6f);

        public static void DrawItems(Camera cam)
        {
            var items = ESPCache.CachedItems;
            if (items == null) return;
            foreach (Item item in items)
            {
                if (item == null || item.transform.parent != null || !ESPCache.IsInRange(item.transform.position)) continue;
                string cleanName = (item.Stats != null) ? item.Stats.fullName : "未知物品";

                if (Settings.IsItemEspWireframeEnabled)
                {
                    Vector2 dynSize = GetDynamicEntitySize(item.gameObject, DefaultItemSize);
                    DrawWireframeFor(cam, item.transform.position, Settings.SelectedEspColor,
                        cleanName, dynSize);
                }
                else
                {
                    RenderUtils.DrawEspLabel(cam, item.transform.position, cleanName,
                        Settings.SelectedEspColor, Settings.EspFontSize);
                }
            }
        }

        // =========================================================
        // 生物 ESP
        // =========================================================
        private static readonly Vector2 DefaultCreatureSize = new Vector2(1.0f, 1.8f);

        public static void DrawCreatures(Camera cam)
        {
            var entities = ESPCache.CachedEntities;
            if (entities == null) return;
            foreach (BuildingEntity entity in entities)
            {
                if (entity == null || !entity.animal || entity.health < 0.5f || !ESPCache.IsInRange(entity.transform.position)) continue;
                string creatureName = string.IsNullOrEmpty(entity.fullName) ? "未知生物" : entity.fullName;
                string tagStr = entity.metallic ? "[机械] " : "[异星] ";
                string info = tagStr + creatureName + "\nHP: " + Mathf.RoundToInt(entity.health);

                if (Settings.IsCreatureEspWireframeEnabled)
                {
                    Vector2 dynSize = GetDynamicEntitySize(entity.gameObject, DefaultCreatureSize);
                    DrawWireframeFor(cam, entity.transform.position, Settings.SelectedCreatureColor,
                        info, dynSize);
                }
                else
                {
                    RenderUtils.DrawEspLabel(cam, entity.transform.position, info,
                        Settings.SelectedCreatureColor, Settings.EspFontSize);
                }
            }
        }

        // =========================================================
        // 陷阱 ESP
        // =========================================================
        private static readonly Vector2 DefaultTrapSize = new Vector2(1.2f, 1.2f);

        public static void DrawTraps(Camera cam)
        {
            Color c = Color.red;

            // 物理射线始终渲染（wireframe or not）
            foreach (var line in ESPCache.CachedLines)
                RenderUtils.DrawWorldLine(cam, line.start, line.end, line.color);

            // ---- Helper to draw trap with wireframe awareness ----
            System.Action<Vector3, GameObject, string> drawTrap = (worldPos, obj, label) =>
            {
                if (Settings.IsTrapEspWireframeEnabled)
                {
                    Vector2 dynSize = GetDynamicEntitySize(obj, DefaultTrapSize);
                    DrawWireframeFor(cam, worldPos, c, label, dynSize);
                }
                else
                {
                    RenderUtils.DrawEspLabel(cam, worldPos, label, c, Settings.EspFontSize);
                }
            };

            if (ESPCache.CachedBearTraps != null)
                foreach (var t in ESPCache.CachedBearTraps)
                    if (t != null && t.caughtLimb == null && ESPCache.IsInRange(t.transform.position))
                    {
                        drawTrap(t.transform.position, t.gameObject, "[ 捕熊夹 ]");
                        RenderUtils.DrawWorldCircle(cam, t.transform.position, 1.5f, c);
                    }

            if (ESPCache.CachedMines != null)
                foreach (var m in ESPCache.CachedMines)
                    if (m != null && m.build != null && m.build.health > 0.5f && ESPCache.IsInRange(m.transform.position))
                    {
                        string name = string.IsNullOrEmpty(m.build.fullName) ? "地雷" : m.build.fullName;
                        drawTrap(m.transform.position, m.gameObject, "[ " + name + " ]");
                        RenderUtils.DrawWorldCircle(cam, m.transform.position, 9f, c);
                    }

            if (ESPCache.CachedSpikes != null)
                foreach (var s in ESPCache.CachedSpikes)
                    if (s != null && ESPCache.IsInRange(s.transform.position))
                        drawTrap(s.transform.position, s.gameObject, "[ 致命地刺 ]");

            if (ESPCache.CachedTurrets != null)
                foreach (var t in ESPCache.CachedTurrets)
                {
                    if (t == null || !ESPCache.IsInRange(t.transform.position)) continue;
                    BuildingEntity b = t.GetComponent<BuildingEntity>();
                    if (b != null && b.health > 0.5f)
                    {
                        string name = string.IsNullOrEmpty(b.fullName) ? "自动炮塔" : b.fullName;
                        string status = t.didBeep ? "\n<color=red>[ 锁定中! ]</color>" : "";
                        drawTrap(t.transform.position, t.gameObject, "[ " + name + " ]" + status);
                    }
                }

            if (ESPCache.CachedCannons != null)
                foreach (var sc in ESPCache.CachedCannons)
                    if (sc != null && ESPCache.IsInRange(sc.transform.position))
                    {
                        drawTrap(sc.transform.position, sc.gameObject, "[ 音波炮 ]");
                        RenderUtils.DrawWorldCircle(cam, sc.transform.position, sc.maxDistance, c);
                    }

            if (ESPCache.CachedDroppers != null)
                foreach (var d in ESPCache.CachedDroppers)
                {
                    if (d == null || !ESPCache.IsInRange(d.transform.position)) continue;
                    BuildingEntity b = d.GetComponent<BuildingEntity>();
                    if (b != null && b.health > 0.5f)
                    {
                        string name = string.IsNullOrEmpty(b.fullName) ? "坠落物" : b.fullName;
                        drawTrap(d.transform.position, d.gameObject, "[ " + name + " ]");
                    }
                }

            if (ESPCache.CachedGeysers != null)
                foreach (var g in ESPCache.CachedGeysers)
                    if (g != null && ESPCache.IsInRange(g.transform.position))
                        drawTrap(g.transform.position, g.gameObject, "[ 间歇泉 ]");

            if (ESPCache.CachedJumpPads != null)
                foreach (var p in ESPCache.CachedJumpPads)
                    if (p != null && !p.disabled && ESPCache.IsInRange(p.transform.position))
                        drawTrap(p.transform.position, p.gameObject, "[ 弹跳跳板 ]\n↑ 强制击飞危险 ↑");

            if (ESPCache.CachedBarbedWires != null)
                foreach (var w in ESPCache.CachedBarbedWires)
                    if (w != null && ESPCache.IsInRange(w.transform.position))
                    {
                        drawTrap(w.transform.position, w.gameObject, "[ 铁丝网 ]");
                        RenderUtils.DrawWorldCircle(cam, w.transform.position + Vector3.up * 4f, 0.5f, c);
                    }

            if (ESPCache.CachedCoils != null)
                foreach (var coil in ESPCache.CachedCoils)
                    if (coil != null && ESPCache.IsInRange(coil.transform.position))
                    {
                        drawTrap(coil.transform.position, coil.gameObject, "[ 危险线圈 ]");
                        RenderUtils.DrawWorldCircle(cam, coil.transform.position, 2.5f, c);
                    }

            if (ESPCache.CachedTentacles != null)
                foreach (var ten in ESPCache.CachedTentacles)
                    if (ten != null && ESPCache.IsInRange(ten.transform.position))
                        drawTrap(ten.transform.position, ten.gameObject, "[ 触手植物 ]");

            if (ESPCache.CachedGlassShards != null)
                foreach (var sh in ESPCache.CachedGlassShards)
                    if (sh != null && ESPCache.IsInRange(sh.transform.position))
                        drawTrap(sh.transform.position, sh.gameObject, "[ 玻璃碎片 ]");

            if (ESPCache.CachedRadObjects != null)
                foreach (var rad in ESPCache.CachedRadObjects)
                    if (rad != null && ESPCache.IsInRange(rad.transform.position))
                    {
                        string objName = rad.gameObject.name.ToLower().Contains("barrel") ? "[ 核燃料桶 ]" : "[ 衰变燃料 ]";
                        drawTrap(rad.transform.position, rad.gameObject, objName + "\n(强辐射)");
                        float radRadius = 8f;
                        try { radRadius = Mathf.Sqrt(rad.radAtZero / 0.1f) / 0.3f; radRadius = Mathf.Clamp(radRadius, 2f, 30f); } catch { }
                        RenderUtils.DrawWorldCircle(cam, rad.transform.position, radRadius, c);
                    }

            if (ESPCache.CachedTickSwarms != null)
                foreach (var sw in ESPCache.CachedTickSwarms)
                    if (sw != null && ESPCache.IsInRange(sw.transform.position))
                    {
                        drawTrap(sw.transform.position, sw.gameObject, "[ 潜伏蜱虫群 ]");
                        RenderUtils.DrawWorldCircle(cam, sw.transform.position, 1f, c);
                    }
        }

        // =========================================================
        // 玩家 ESP
        // =========================================================
        private static readonly Vector2 DefaultPlayerSize = new Vector2(1.0f, 1.8f);

        public static void DrawPlayers(Camera cam)
        {
            var players = ESPCache.CachedPlayers;
            if (players == null) return;

            float distance = 0f;
            foreach (Body body in players)
            {
                if (body == null || !body) continue;

                Vector3 worldPos = body.transform.position;
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z <= 0) continue;

                distance = Vector3.Distance(cam.transform.position, worldPos);
                int distMeters = (int)(distance * 0.3f);

                string playerName = string.IsNullOrEmpty(body.name) ? "未知玩家" : body.name;
                string info = "[ " + playerName + " ]\n[ " + distMeters + "m ]";

                if (Settings.IsPlayerEspWireframeEnabled)
                {
                    Vector2 dynSize = GetDynamicEntitySize(body.gameObject, DefaultPlayerSize);
                    DrawWireframeFor(cam, worldPos, Settings.SelectedPlayerColor,
                        "[ " + playerName + " ]", dynSize);
                }
                else
                {
                    RenderUtils.DrawEspLabel(cam, worldPos, info,
                        Settings.SelectedPlayerColor, Settings.EspFontSize);
                }
            }
        }
    }
}
