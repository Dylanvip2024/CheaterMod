using FullBrightMod.Core;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules.Player
{
    public class AutoPush : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_autopush");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()  => Settings.IsAutoPushEnabled = true;
        public override void OnDisable() => Settings.IsAutoPushEnabled = false;

        private float _pushTimer;

        public override float GetSettingsHeight() => 30f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            float val = Settings.AutoPushDistance;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            GUI.Label(new Rect(x + 8, y, 70, 20),
                $"  {Utils.I18n.Get("set_autopush_distance")}: {Settings.AutoPushDistance:F1}m", labelStyle);

            Rect barRect = new Rect(x + 78, y + 5, width - 90, 14);
            Settings.AutoPushDistance = ClickGUIManager.DrawSlider(barRect, Settings.AutoPushDistance, 1f, 5f, e);

            y += 30f;
        }

        public override void OnUpdate()
        {
            _pushTimer += Time.deltaTime;
            if (_pushTimer < 0.5f) return;
            _pushTimer = 0f;

            var players = ESPCache.CachedPlayers;
            if (players == null) return;

            NetBody localNetBody = NetPlayer.LOCAL_PLAYER?.playerbody;
            if (localNetBody == null) return;

            float pushDistSqr = Settings.AutoPushDistance * Settings.AutoPushDistance;
            Vector2 myPos = localNetBody.rb.position;

            foreach (Body targetBody in players)
            {
                if (targetBody == null || !targetBody || !targetBody.conscious) continue;

                Vector2 targetPos = targetBody.rb.position;
                if ((targetPos - myPos).sqrMagnitude > pushDistSqr) continue;

                try
                {
                    NetBody targetNetBody = targetBody.GetComponent<NetBody>();
                    if (targetNetBody != null)
                        localNetBody.Push(targetNetBody);
                }
                catch { }
            }
        }
    }
}
