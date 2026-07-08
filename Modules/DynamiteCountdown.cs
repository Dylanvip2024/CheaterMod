using System.Collections.Generic;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class DynamiteCountdown : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_dynamite_countdown") ?? "炸药倒计时";
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable() => ModSettings.IsDynamiteCountdownEnabled = true;
        public override void OnDisable()
        {
            ModSettings.IsDynamiteCountdownEnabled = false;
            Tracked.Clear();
            LearnedFuseTime = 0f;
        }

        internal static readonly Dictionary<CustomItemBehaviour, DynamiteEntry> Tracked = new Dictionary<CustomItemBehaviour, DynamiteEntry>();
        internal static float DefaultFuseTime = 4f;
        internal static float LearnedFuseTime = 0f;

        private static Texture2D _whiteTex;

        private static Texture2D WhiteTex
        {
            get
            {
                if (_whiteTex == null)
                {
                    _whiteTex = new Texture2D(1, 1);
                    _whiteTex.SetPixel(0, 0, Color.white);
                    _whiteTex.Apply();
                }
                return _whiteTex;
            }
        }

        internal static void Track(CustomItemBehaviour beh)
        {
            if (beh == null || Tracked.ContainsKey(beh)) return;
            if (LearnedFuseTime > 0f)
                DefaultFuseTime = LearnedFuseTime;
            Tracked[beh] = new DynamiteEntry { Behaviour = beh, IgniteTime = Time.time };
        }

        internal static void Untrack(CustomItemBehaviour beh)
        {
            if (beh == null) return;
            DynamiteEntry entry;
            if (Tracked.TryGetValue(beh, out entry))
            {
                float elapsed = Time.time - entry.IgniteTime;
                if (elapsed > 0.5f && elapsed < 30f)
                    LearnedFuseTime = elapsed;
            }
            Tracked.Remove(beh);
        }

        public override void OnUpdate()
        {
            if (!ModSettings.IsDynamiteCountdownEnabled) return;

            var keysToRemove = new List<CustomItemBehaviour>();
            foreach (var kvp in Tracked)
            {
                if (kvp.Key == null)
                    keysToRemove.Add(kvp.Key);
                else if (Time.time - kvp.Value.IgniteTime > DefaultFuseTime + 10f)
                    keysToRemove.Add(kvp.Key);
            }
            foreach (var key in keysToRemove)
            {
                Tracked.Remove(key);
            }
        }

        public override void OnGUI()
        {
            if (!ModSettings.IsDynamiteCountdownEnabled) return;
            if (Tracked.Count == 0) return;

            float fuseTime = (LearnedFuseTime > 0f) ? LearnedFuseTime : DefaultFuseTime;
            float now = Time.time;
            float minRemaining = float.MaxValue;
            int count = 0;

            foreach (var entry in Tracked.Values)
            {
                float remaining = Mathf.Max(0f, fuseTime - (now - entry.IgniteTime));
                if (remaining < minRemaining)
                    minRemaining = remaining;
                count++;
            }

            float barW = 300f;
            float barH = 32f;
            float barX = (Screen.width - barW) / 2f;
            float barY = 12f;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(barX - 4f, barY - 4f, barW + 8f, barH + 8f), WhiteTex);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), WhiteTex);

            float t = Mathf.Clamp01(minRemaining / fuseTime);
            GUI.color = (t > 0.5f) ? Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f)
                           : Color.Lerp(Color.red, Color.yellow, t * 2f);
            GUI.DrawTexture(new Rect(barX + 2f, barY + 2f, (barW - 4f) * t, barH - 4f), WhiteTex);
            GUI.color = Color.white;

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            string text = (count > 1)
                ? string.Format("炸药  ×{0}  最快爆炸: {1:F1}s", count, minRemaining)
                : string.Format("炸药  引爆倒计时: {0:F1}s", minRemaining);
            GUI.Label(new Rect(barX, barY, barW, barH), text, textStyle);
        }
    }

    internal class DynamiteEntry
    {
        public CustomItemBehaviour Behaviour;
        public float IgniteTime;
    }
}

