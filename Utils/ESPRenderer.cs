using FullBrightMod.Core;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Utils
{
    /// <summary>
    /// ESP 渲染器 —— 静态工具类，从旧 ESPModule 提取的纯渲染逻辑。
    /// 由 Plugin.OnGUI() 直接调用，读取 Settings 开关决定是否绘制。
    /// 不是 ModuleBase，不参与 ClickGUI 生命周期。
    /// </summary>
    public static class ESPRenderer
    {
        // =========================================================
        // 物品 ESP
        // =========================================================
        public static void DrawItems(Camera cam)
        {
            var items = ESPCache.CachedItems;
            if (items == null) return;
            foreach (Item item in items)
            {
                if (item == null || item.transform.parent != null) continue;
                string cleanName = (item.Stats != null) ? item.Stats.fullName : "未知物品";
                RenderUtils.DrawEspLabel(cam, item.transform.position, cleanName,
                    Settings.SelectedEspColor, Settings.EspFontSize);
            }
        }

        // =========================================================
        // 生物 ESP
        // =========================================================
        public static void DrawCreatures(Camera cam)
        {
            var entities = ESPCache.CachedEntities;
            if (entities == null) return;
            foreach (BuildingEntity entity in entities)
            {
                if (entity == null || !entity.animal || entity.health < 0.5f) continue;
                string creatureName = string.IsNullOrEmpty(entity.fullName) ? "未知生物" : entity.fullName;
                string tagStr = entity.metallic ? "[机械] " : "[异星] ";
                RenderUtils.DrawEspLabel(cam, entity.transform.position,
                    tagStr + creatureName + "\nHP: " + Mathf.RoundToInt(entity.health),
                    Settings.SelectedCreatureColor, Settings.EspFontSize);
            }
        }

        // =========================================================
        // 陷阱 ESP（所有16种陷阱/危险物）
        // =========================================================
        public static void DrawTraps(Camera cam)
        {
            Color c = Color.red;

            // 物理射线（地刺、炮塔、滴水石锥、间歇泉、跳板）
            foreach (var line in ESPCache.CachedLines)
                RenderUtils.DrawWorldLine(cam, line.start, line.end, line.color);

            // 捕熊夹
            if (ESPCache.CachedBearTraps != null)
                foreach (var t in ESPCache.CachedBearTraps)
                    if (t != null && t.caughtLimb == null)
                    {
                        RenderUtils.DrawEspLabel(cam, t.transform.position, "[ 捕熊夹 ]", c, Settings.EspFontSize);
                        RenderUtils.DrawWorldCircle(cam, t.transform.position, 1.5f, c);
                    }

            // 地雷
            if (ESPCache.CachedMines != null)
                foreach (var m in ESPCache.CachedMines)
                    if (m != null && m.build != null && m.build.health > 0.5f)
                    {
                        string name = string.IsNullOrEmpty(m.build.fullName) ? "地雷" : m.build.fullName;
                        RenderUtils.DrawEspLabel(cam, m.transform.position, "[ " + name + " ]", c, Settings.EspFontSize);
                        RenderUtils.DrawWorldCircle(cam, m.transform.position, 9f, c);
                    }

            // 致命地刺
            if (ESPCache.CachedSpikes != null)
                foreach (var s in ESPCache.CachedSpikes)
                    if (s != null) RenderUtils.DrawEspLabel(cam, s.transform.position, "[ 致命地刺 ]", c, Settings.EspFontSize);

            // 自动炮塔
            if (ESPCache.CachedTurrets != null)
                foreach (var t in ESPCache.CachedTurrets)
                {
                    if (t == null) continue;
                    BuildingEntity b = t.GetComponent<BuildingEntity>();
                    if (b != null && b.health > 0.5f)
                    {
                        string name = string.IsNullOrEmpty(b.fullName) ? "自动炮塔" : b.fullName;
                        string status = t.didBeep ? "\n<color=red>[ 锁定中! ]</color>" : "";
                        RenderUtils.DrawEspLabel(cam, t.transform.position, "[ " + name + " ]" + status, c, Settings.EspFontSize, true);
                    }
                }

            // 音波炮
            if (ESPCache.CachedCannons != null)
                foreach (var sc in ESPCache.CachedCannons)
                    if (sc != null)
                    {
                        RenderUtils.DrawEspLabel(cam, sc.transform.position, "[ 音波炮 ]", c, Settings.EspFontSize);
                        RenderUtils.DrawWorldCircle(cam, sc.transform.position, sc.maxDistance, c);
                    }

            // 滴水石锥
            if (ESPCache.CachedDroppers != null)
                foreach (var d in ESPCache.CachedDroppers)
                {
                    if (d == null) continue;
                    BuildingEntity b = d.GetComponent<BuildingEntity>();
                    if (b != null && b.health > 0.5f)
                    {
                        string name = string.IsNullOrEmpty(b.fullName) ? "坠落物" : b.fullName;
                        RenderUtils.DrawEspLabel(cam, d.transform.position, "[ " + name + " ]", c, Settings.EspFontSize);
                    }
                }

            // 间歇泉
            if (ESPCache.CachedGeysers != null)
                foreach (var g in ESPCache.CachedGeysers)
                    if (g != null) RenderUtils.DrawEspLabel(cam, g.transform.position, "[ 间歇泉 ]", c, Settings.EspFontSize);

            // 弹跳跳板
            if (ESPCache.CachedJumpPads != null)
                foreach (var p in ESPCache.CachedJumpPads)
                    if (p != null && !p.disabled) RenderUtils.DrawEspLabel(cam, p.transform.position, "[ 弹跳跳板 ]\n↑ 强制击飞危险 ↑", c, Settings.EspFontSize);

            // --- 新增7种陷阱 ---

            // 铁丝网
            if (ESPCache.CachedBarbedWires != null)
                foreach (var w in ESPCache.CachedBarbedWires)
                    if (w != null)
                    {
                        RenderUtils.DrawEspLabel(cam, w.transform.position, "[ 铁丝网 ]", c, Settings.EspFontSize);
                        RenderUtils.DrawWorldCircle(cam, w.transform.position + Vector3.up * 4f, 0.5f, c);
                    }

            // 电线圈
            if (ESPCache.CachedCoils != null)
                foreach (var coil in ESPCache.CachedCoils)
                    if (coil != null)
                    {
                        RenderUtils.DrawEspLabel(cam, coil.transform.position, "[ 危险线圈 ]", c, Settings.EspFontSize);
                        RenderUtils.DrawWorldCircle(cam, coil.transform.position, 2.5f, c);
                    }

            // 触手植物
            if (ESPCache.CachedTentacles != null)
                foreach (var ten in ESPCache.CachedTentacles)
                    if (ten != null) RenderUtils.DrawEspLabel(cam, ten.transform.position, "[ 触手植物 ]", c, Settings.EspFontSize);

            // 玻璃碎片
            if (ESPCache.CachedGlassShards != null)
                foreach (var sh in ESPCache.CachedGlassShards)
                    if (sh != null) RenderUtils.DrawEspLabel(cam, sh.transform.position, "[ 玻璃碎片 ]", c, Settings.EspFontSize);

            // 辐射危险物（衰变燃料 & 核燃料桶）
            if (ESPCache.CachedRadObjects != null)
                foreach (var rad in ESPCache.CachedRadObjects)
                    if (rad != null)
                    {
                        string objName = rad.gameObject.name.ToLower().Contains("barrel") ? "[ 核燃料桶 ]" : "[ 衰变燃料 ]";
                        RenderUtils.DrawEspLabel(cam, rad.transform.position, objName + "\n(强辐射)", c, Settings.EspFontSize);
                        float radRadius = 8f;
                        try { radRadius = Mathf.Sqrt(rad.radAtZero / 0.1f) / 0.3f; radRadius = Mathf.Clamp(radRadius, 2f, 30f); } catch { }
                        RenderUtils.DrawWorldCircle(cam, rad.transform.position, radRadius, c);
                    }

            // 未触发的蜱虫群
            if (ESPCache.CachedTickSwarms != null)
                foreach (var sw in ESPCache.CachedTickSwarms)
                    if (sw != null)
                    {
                        RenderUtils.DrawEspLabel(cam, sw.transform.position, "[ 潜伏蜱虫群 ]", c, Settings.EspFontSize);
                        RenderUtils.DrawWorldCircle(cam, sw.transform.position, 1f, c);
                    }
        }
    }
}
