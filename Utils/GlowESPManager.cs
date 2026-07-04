using System.Collections.Generic;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Utils
{
    /// <summary>
    /// GlowESPManager（外发光透视管理器）
    /// 
    /// 通过动态替换实体的 SpriteRenderer.sharedMaterial 为
    /// AssetBundle 中的 GlowMaterial（发光 Shader），实现 Glow ESP 效果。
    /// 
    /// 工作流：
    ///   ApplyGlow(gameObject, color) → 记录原材质 → 替换为 GlowMaterial 实例
    ///   RemoveGlow(gameObject)        → 恢复原材质 → 移除记录
    ///   ProcessESPEntities()          → 遍历 ESPCache 中的 Creatures/Players
    ///                                    对符合条件的实体 ApplyGlow，对失效实体 RemoveGlow
    /// </summary>
    public static class GlowESPManager
    {
        /// <summary>记录每个 SpriteRenderer 的原始材质</summary>
        private static readonly Dictionary<SpriteRenderer, Material> OriginalMaterials
            = new Dictionary<SpriteRenderer, Material>();

        /// <summary>当前已应用 Glow 的 GameObject 集合（用于失效检测）</summary>
        private static readonly HashSet<GameObject> ActiveGlowTargets = new HashSet<GameObject>();

        /// <summary>本次 ESP 扫描周期内仍有效的目标</summary>
        private static readonly HashSet<GameObject> StillValidTargets = new HashSet<GameObject>();

        /// <summary>
        /// 对指定 GameObject 的所有 SpriteRenderer 应用发光材质。
        /// 如果已应用则跳过（仅更新颜色）。
        /// </summary>
        public static void ApplyGlow(GameObject obj, Color color)
        {
            if (obj == null) return;

            Material glowMat = ResourceManager.GlowMaterial;
            if (glowMat == null) return;

            StillValidTargets.Add(obj);

            SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers)
            {
                if (sr == null) continue;

                if (OriginalMaterials.ContainsKey(sr))
                {
                    // 已应用 Glow，仅更新颜色
                    sr.material.SetColor("_GlowColor", color);
                    continue;
                }

                // 首次应用：记录原材质，替换为 GlowMaterial 实例
                OriginalMaterials[sr] = sr.sharedMaterial;
                Material instance = Object.Instantiate(glowMat);
                instance.SetColor("_GlowColor", color);
                sr.material = instance;
            }

            ActiveGlowTargets.Add(obj);
        }

        /// <summary>
        /// 移除指定 GameObject 上所有 SpriteRenderer 的发光材质，恢复原材质。
        /// </summary>
        public static void RemoveGlow(GameObject obj)
        {
            if (obj == null) return;

            SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in renderers)
            {
                if (sr == null) continue;
                if (!OriginalMaterials.TryGetValue(sr, out Material original)) continue;

                // 销毁 Glow 材质实例，恢复原材质
                if (sr.material != null && sr.material != original)
                    Object.Destroy(sr.material);
                sr.material = original;
                OriginalMaterials.Remove(sr);
            }

            ActiveGlowTargets.Remove(obj);
        }

        /// <summary>
        /// 遍历 ESPCache 中的 Creatures, Players, Items 和 Traps，应用/移除 Glow。
        /// </summary>
        public static void ProcessESPEntities()
        {
            var mat = ResourceManager.GlowMaterial;
            if (mat == null) return; // AssetBundle 未加载成功

            StillValidTargets.Clear();

            // ── 1. Creatures (生物) ──
            if (ModSettings.IsCreatureEspEnabled && ModSettings.IsGlowEspEnabled && ESPCache.CachedEntities != null)
            {
                foreach (var be in ESPCache.CachedEntities)
                {
                    if (be == null || !be) continue;
                    if (!be.animal || be.health <= 0.5f) continue;
                    if (!ESPCache.IsInRange(be.transform.position)) continue;

                    ApplyGlow(be.gameObject, ModSettings.SelectedCreatureColor);
                }
            }

            // ── 2. Players (玩家) ──
            if (ModSettings.IsPlayerEspEnabled && ModSettings.IsGlowEspEnabled && ESPCache.CachedPlayers != null)
            {
                foreach (var body in ESPCache.CachedPlayers)
                {
                    if (body == null || !body) continue;
                    if (!ESPCache.IsInRange(body.transform.position)) continue;

                    ApplyGlow(body.gameObject, ModSettings.SelectedPlayerColor);
                }
            }

            // ── 3. Items (物品) ──
            if (ModSettings.IsItemEspEnabled && ModSettings.IsGlowEspEnabled && ESPCache.CachedItems != null)
            {
                foreach (var item in ESPCache.CachedItems)
                {
                    if (item == null || !item || item.transform.parent != null) continue;
                    if (!ESPCache.IsInRange(item.transform.position)) continue;

                    ApplyGlow(item.gameObject, ModSettings.SelectedEspColor);
                }
            }

            // ── 4. Traps (陷阱与特定建筑) ──
            if (ModSettings.IsTrapEspEnabled && ModSettings.IsGlowEspEnabled)
            {
                Color trapColor = ModSettings.SelectedTrapColor;

                System.Action<MonoBehaviour[]> tryApplyTrapGlow = (traps) =>
                {
                    if (traps == null) return;
                    foreach (var t in traps)
                    {
                        if (t != null && t && ESPCache.IsInRange(t.transform.position))
                        {
                            ApplyGlow(t.gameObject, trapColor);
                        }
                    }
                };

                tryApplyTrapGlow(ESPCache.CachedBearTraps);
                tryApplyTrapGlow(ESPCache.CachedMines);
                tryApplyTrapGlow(ESPCache.CachedSpikes);
                tryApplyTrapGlow(ESPCache.CachedTurrets);
                tryApplyTrapGlow(ESPCache.CachedCannons);
                tryApplyTrapGlow(ESPCache.CachedDroppers);
                tryApplyTrapGlow(ESPCache.CachedGeysers);
                tryApplyTrapGlow(ESPCache.CachedJumpPads);
                tryApplyTrapGlow(ESPCache.CachedBarbedWires);
                tryApplyTrapGlow(ESPCache.CachedCoils);
                tryApplyTrapGlow(ESPCache.CachedTentacles);
                tryApplyTrapGlow(ESPCache.CachedGlassShards);
                tryApplyTrapGlow(ESPCache.CachedRadObjects);
                tryApplyTrapGlow(ESPCache.CachedTickSwarms);
            }

            // ── 清理不再有效的目标 ──
            var removed = new List<GameObject>();
            foreach (var obj in ActiveGlowTargets)
            {
                if (!StillValidTargets.Contains(obj))
                    removed.Add(obj);
            }
            foreach (var obj in removed)
                RemoveGlow(obj);
        }

        /// <summary>
        /// 强制移除所有 Glow 效果（模块关闭时调用）。
        /// </summary>
        public static void RemoveAllGlow()
        {
            var toRemove = new List<GameObject>(ActiveGlowTargets);
            foreach (var obj in toRemove)
                RemoveGlow(obj);

            ActiveGlowTargets.Clear();
            StillValidTargets.Clear();
            OriginalMaterials.Clear();
        }
    }
}
