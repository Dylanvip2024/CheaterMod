using System.Collections.Generic;
using UnityEngine;

namespace FullBrightMod.Utils
{
    public static class ESPCache
    {
        public struct LineData
        {
            public Vector3 start;
            public Vector3 end;
            public Color color;
        }

        public static Item[]              CachedItems;
        public static BuildingEntity[]    CachedEntities;
        public static Body[]              CachedPlayers;
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

        public static readonly List<LineData> CachedLines = new List<LineData>();

        private static float _scanTimer = 1.0f;
        private static int   _groundMask = -1;
        private const float MAX_SCAN_DISTANCE = 150f;
        static ESPCache()
        {
            _groundMask = LayerMask.GetMask("Ground");
        }

        // 帮助函数：检查物体是否在范围内
        public static bool IsInRange(Vector3 targetPos)
        {
            if (PlayerCamera.main == null || PlayerCamera.main.body == null) return false;
            float distSqr = (PlayerCamera.main.body.transform.position - targetPos).sqrMagnitude;
            return distSqr <= (MAX_SCAN_DISTANCE * MAX_SCAN_DISTANCE);
        }

        public static void Tick(float deltaTime)
        {
            _scanTimer += deltaTime;
            if (_scanTimer < 1.0f) return;
            _scanTimer = 0f;

            CachedLines.Clear();

            CachedItems    = GameObject.FindObjectsOfType<Item>();
            CachedEntities = GameObject.FindObjectsOfType<BuildingEntity>();
            RefreshPlayers();
            RefreshTraps();
        }

        private static void RefreshPlayers()
        {
            var allBodies = GameObject.FindObjectsOfType<Body>();
            if (allBodies == null || allBodies.Length == 0)
            {
                CachedPlayers = null;
                return;
            }

            var localBody = PlayerCamera.main?.body;
            var list = new System.Collections.Generic.List<Body>(allBodies.Length);
            foreach (var body in allBodies)
            {
                if (body == null || !body || !body.alive) continue;
                if (localBody != null && body == localBody) continue;
                if (!IsInRange(body.transform.position)) continue;
                list.Add(body);
            }
            CachedPlayers = list.Count > 0 ? list.ToArray() : null;
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

            // ---- 物理射线集中计算（增加 IsInRange 距离判断） ----

            if (CachedSpikes != null)
            {
                foreach (var spike in CachedSpikes)
                {
                    if (spike == null || !IsInRange(spike.transform.position)) continue; // 距离检测
                    Vector3 startPos = spike.transform.position;
                    Vector3 dir = spike.transform.up;
                    float maxDist = 6f;
                    RaycastHit2D hit = Physics2D.Raycast(startPos, dir, maxDist, _groundMask);
                    Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + dir * maxDist;
                    CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                }
            }

            if (CachedTurrets != null)
            {
                foreach (var turret in CachedTurrets)
                {
                    if (turret == null || !IsInRange(turret.transform.position)) continue; // 距离检测
                    BuildingEntity build = turret.GetComponent<BuildingEntity>();
                    if (build != null && build.health > 0.5f)
                    {
                        Vector3 fireDir = (turret.transform.right * Mathf.Sign(turret.transform.localScale.x)).normalized;
                        Vector3 startPos = turret.transform.position + fireDir * 2f;
                        float maxDist = 40f;
                        RaycastHit2D hit = Physics2D.Raycast(startPos, fireDir, maxDist, _groundMask);
                        Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + fireDir * maxDist;
                        CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                    }
                }
            }

            if (CachedDroppers != null)
            {
                foreach (var dropper in CachedDroppers)
                {
                    if (dropper == null || !IsInRange(dropper.transform.position)) continue; // 距离检测
                    BuildingEntity build = dropper.GetComponent<BuildingEntity>();
                    if (build != null && build.health > 0.5f)
                    {
                        Vector3 startPos = dropper.transform.position + Vector3.down * 2f;
                        float maxDist = 40f;
                        RaycastHit2D hit = Physics2D.Raycast(startPos, Vector3.down, maxDist, _groundMask);
                        Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + Vector3.down * maxDist;
                        CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                    }
                }
            }

            if (CachedGeysers != null)
            {
                foreach (var geyser in CachedGeysers)
                {
                    if (geyser == null || !IsInRange(geyser.transform.position)) continue; // 距离检测
                    Vector3 startPos = geyser.transform.position;
                    float maxDist = 8f;
                    RaycastHit2D hit = Physics2D.Raycast(startPos, Vector3.up, maxDist, _groundMask);
                    Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + Vector3.up * maxDist;
                    CachedLines.Add(new LineData { start = startPos, end = endPos, color = Color.red });
                }
            }

            if (CachedJumpPads != null)
            {
                foreach (var pad in CachedJumpPads)
                {
                    if (pad != null && !pad.disabled && IsInRange(pad.transform.position)) // 距离检测
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