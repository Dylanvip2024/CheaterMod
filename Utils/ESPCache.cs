using System.Collections.Generic;
using UnityEngine;

namespace FullBrightMod.Utils
{
    /// <summary>
    /// 实体缓存管理器 —— 统一管理所有 ESP 需要的实体缓存数组。
    /// 每 1 秒通过 FindObjectsOfType 刷新一次；物理射线在刷新时计算并存入 _cachedLines。
    /// 【绝不破坏现有的"物理射线与 UI 渲染解耦"逻辑】
    /// </summary>
    public static class ESPCache
    {
        public struct LineData
        {
            public Vector3 start;
            public Vector3 end;
            public Color color;
        }

        // ---- 缓存的实体数组 ----
        public static Item[]              CachedItems;
        public static BuildingEntity[]    CachedEntities;
        public static BearTrap[]          CachedBearTraps;
        public static MineScript[]        CachedMines;
        public static SpikeStabberScript[]CachedSpikes;
        public static TurretScript[]      CachedTurrets;
        public static SoundCannon[]       CachedCannons;
        public static StalactiteDropper[] CachedDroppers;
        public static GeyserScript[]      CachedGeysers;
        public static JumpPadScript[]     CachedJumpPads;
        public static BarbedFence[]       CachedBarbedWires;
        public static CoilScript[]        CachedCoils;
        public static GrabberPlant[]      CachedTentacles;
        public static GroundGlass[]       CachedGlassShards;
        public static RadioactiveObject[] CachedRadObjects;
        public static CaveTickSpawner[]   CachedTickSwarms;

        // ---- 物理射线缓存（由 Refresh 计算，由 ESP 模块渲染） ----
        public static readonly List<LineData> CachedLines = new List<LineData>();

        private static float _scanTimer = 1.0f;
        private static int   _groundMask = -1;

        static ESPCache()
        {
            _groundMask = LayerMask.GetMask("Ground");
        }

        /// <summary>
        /// 由主插件 Update 每帧调用。内部仅在计时器到期时执行高频扫描。
        /// </summary>
        public static void Tick(float deltaTime)
        {
            _scanTimer += deltaTime;
            if (_scanTimer < 1.0f) return;
            _scanTimer = 0f;

            // 清空上一秒的射线缓存
            CachedLines.Clear();

            // 扫描物品
            CachedItems   = GameObject.FindObjectsOfType<Item>();
            // 扫描生物
            CachedEntities = GameObject.FindObjectsOfType<BuildingEntity>();

            // 扫描所有陷阱
            RefreshTraps();
        }

        private static void RefreshTraps()
        {
            CachedBearTraps   = GameObject.FindObjectsOfType<BearTrap>();
            CachedMines       = GameObject.FindObjectsOfType<MineScript>();
            CachedSpikes      = GameObject.FindObjectsOfType<SpikeStabberScript>();
            CachedTurrets     = GameObject.FindObjectsOfType<TurretScript>();
            CachedCannons     = GameObject.FindObjectsOfType<SoundCannon>();
            CachedDroppers    = GameObject.FindObjectsOfType<StalactiteDropper>();
            CachedGeysers     = GameObject.FindObjectsOfType<GeyserScript>();
            CachedJumpPads    = GameObject.FindObjectsOfType<JumpPadScript>();
            CachedBarbedWires = GameObject.FindObjectsOfType<BarbedFence>();
            CachedCoils       = GameObject.FindObjectsOfType<CoilScript>();
            CachedTentacles   = GameObject.FindObjectsOfType<GrabberPlant>();
            CachedGlassShards = GameObject.FindObjectsOfType<GroundGlass>();
            CachedRadObjects  = GameObject.FindObjectsOfType<RadioactiveObject>();
            CachedTickSwarms  = GameObject.FindObjectsOfType<CaveTickSpawner>();

            // ---- 物理射线集中计算（仅在有物理需求的陷阱上） ----

            // 致命地刺 — 向上射线
            if (CachedSpikes != null)
            {
                foreach (var spike in CachedSpikes)
                {
                    if (spike == null) continue;
                    Vector3 startPos = spike.transform.position;
                    Vector3 dir = spike.transform.up;
                    float maxDist = 6f;
                    RaycastHit2D hit = Physics2D.Raycast(startPos, dir, maxDist, _groundMask);
                    Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + dir * maxDist;
                    CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                }
            }

            // 自动炮塔 — 射击方向射线
            if (CachedTurrets != null)
            {
                foreach (var turret in CachedTurrets)
                {
                    if (turret == null) continue;
                    BuildingEntity build = turret.GetComponent<BuildingEntity>();
                    if (build != null && build.health > 0.5f)
                    {
                        Vector3 fireDir = (turret.transform.right * Mathf.Sign(turret.transform.localScale.x)).normalized;
                        Vector3 startPos = turret.transform.position + fireDir * 0.6f;
                        float maxDist = 40f;
                        RaycastHit2D hit = Physics2D.Raycast(startPos, fireDir, maxDist, _groundMask);
                        Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + fireDir * maxDist;
                        CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                    }
                }
            }

            // 滴水石锥 — 向下射线
            if (CachedDroppers != null)
            {
                foreach (var dropper in CachedDroppers)
                {
                    if (dropper == null) continue;
                    BuildingEntity build = dropper.GetComponent<BuildingEntity>();
                    if (build != null && build.health > 0.5f)
                    {
                        Vector3 startPos = dropper.transform.position + Vector3.down * 0.6f;
                        float maxDist = 40f;
                        RaycastHit2D hit = Physics2D.Raycast(startPos, Vector3.down, maxDist, _groundMask);
                        Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + Vector3.down * maxDist;
                        CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                    }
                }
            }

            // 间歇泉 — 向上射线
            if (CachedGeysers != null)
            {
                foreach (var geyser in CachedGeysers)
                {
                    if (geyser == null) continue;
                    Vector3 startPos = geyser.transform.position;
                    float maxDist = 8f;
                    RaycastHit2D hit = Physics2D.Raycast(startPos, Vector3.up, maxDist, _groundMask);
                    Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + Vector3.up * maxDist;
                    CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                }
            }

            // 弹跳跳板 — 向上射线
            if (CachedJumpPads != null)
            {
                foreach (var pad in CachedJumpPads)
                {
                    if (pad != null && !pad.disabled)
                    {
                        Vector3 startPos = pad.transform.position;
                        float maxDist = 12f;
                        RaycastHit2D hit = Physics2D.Raycast(startPos, Vector3.up, maxDist, _groundMask);
                        Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + Vector3.up * maxDist;
                        CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                    }
                }
            }
        }
    }
}
