using FullBrightMod.Core;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class AntiHeadshot : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_anti_headshot") ?? "反爆头";
        public override ModuleCategory Category => ModuleCategory.Combat;
        public override void OnEnable() => ModSettings.IsAntiHeadshotEnabled = true;
        public override void OnDisable() => ModSettings.IsAntiHeadshotEnabled = false;

        private const float ScanRadius = 20f;
        private const float LookAwayDistance = 10f;

        public override void OnUpdate()
        {
            if (!ModSettings.IsAntiHeadshotEnabled) return;
            Body myBody = PlayerCamera.main?.body;
            if (myBody == null || !myBody.conscious) return;

            Body nearestEnemy = FindNearestEnemy(myBody);
            if (nearestEnemy == null) return;

            Vector2 myPos = myBody.transform.position;
            Vector2 dirAway = (myPos - (Vector2)nearestEnemy.transform.position).normalized;
            myBody.targetLookPos = myPos + dirAway * LookAwayDistance + Vector2.down * 3f;
        }

        private static Body FindNearestEnemy(Body myBody)
        {
            Body nearest = null;
            float nearestDist = ScanRadius;
            foreach (Body b in ESPCache.CachedPlayers)
            {
                if (b == myBody || b == null || !b.alive) continue;
                float d = Vector2.Distance(myBody.transform.position, b.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = b;
                }
            }
            return nearest;
        }
    }
}
