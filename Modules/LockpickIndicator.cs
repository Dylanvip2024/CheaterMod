using FullBrightMod.Core;
using HarmonyLib;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class LockpickIndicator : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_lockpick_indicator") ?? "开锁指示器";
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable() => ModSettings.IsLockpickIndicatorEnabled = true;
        public override void OnDisable() => ModSettings.IsLockpickIndicatorEnabled = false;

        internal static LockpingMinigame ActiveMinigame;

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

        public override void OnGUI()
        {
            if (!ModSettings.IsLockpickIndicatorEnabled) return;
            if (ActiveMinigame == null) return;

            try
            {
                Traverse traverse = Traverse.Create(ActiveMinigame);
                RectTransform lockRect = traverse.Field("lockRect").GetValue<RectTransform>();
                if (lockRect == null) return;

                float correctAngle = traverse.Field("correctAngle").GetValue<float>();
                float anglePrecision = traverse.Field("anglePrecision").GetValue<float>();
                float lockProgress = traverse.Field("lockProgress").GetValue<float>();

                float handAngle = 0f;
                bool hasHandAngle = false;
                try
                {
                    handAngle = traverse.Property("HandAngle", null).GetValue<float>();
                    hasHandAngle = true;
                }
                catch
                {
                    try
                    {
                        handAngle = traverse.Field("HandAngle").GetValue<float>();
                        hasHandAngle = true;
                    }
                    catch { }
                }

                Vector3[] corners = new Vector3[4];
                lockRect.GetWorldCorners(corners);
                Vector2 center = (corners[0] + corners[2]) / 2f;
                center.y = Screen.height - center.y;

                float innerR = 195f;
                float outerR = 247f;
                float rimR = 221f;

                float radCorrect = correctAngle * Mathf.Deg2Rad;
                Vector2 dirCorrect = new Vector2(Mathf.Cos(radCorrect), -Mathf.Sin(radCorrect));
                DrawLine(center + dirCorrect * innerR, center + dirCorrect * outerR, Color.green, 4f);

                if (anglePrecision > 0.5f)
                {
                    float radMin = (correctAngle - anglePrecision) * Mathf.Deg2Rad;
                    float radMax = (correctAngle + anglePrecision) * Mathf.Deg2Rad;
                    DrawArc(center, innerR, outerR, radMin, radMax, new Color(0f, 1f, 0f, 0.25f));
                    DrawArc(center, rimR - 3f, rimR + 3f, radMin, radMax, Color.green);
                }

                GUI.color = Color.green;
                GUI.DrawTexture(new Rect(center.x - 4f, center.y - 4f, 8f, 8f), WhiteTex);

                if (hasHandAngle)
                {
                    float radHand = handAngle * Mathf.Deg2Rad;
                    Vector2 dirHand = new Vector2(Mathf.Cos(radHand), -Mathf.Sin(radHand));
                    DrawLine(center + dirHand * innerR, center + dirHand * outerR, Color.yellow, 2f);
                }

                float radProgress = lockProgress * 180f * Mathf.Deg2Rad;
                DrawArc(center, rimR - 2f, rimR + 2f, 0f, radProgress, Color.cyan);

                GUI.color = Color.white;

                GUIStyle textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
                string text = string.Format("<color=#00FF00>Target: {0:F1}°</color>  Precision: ±{1:F1}°  ",
                    correctAngle, anglePrecision)
                    + (hasHandAngle ? string.Format("<color=#FFFF00>Hand: {0:F1}°</color>  ", handAngle) : "")
                    + string.Format("<color=#00FFFF>Progress: {0:F0}%</color>", lockProgress * 100f);
                GUI.Label(new Rect(15f, 15f, 600f, 28f), text, textStyle);
            }
            catch
            {
                ActiveMinigame = null;
            }
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            if (delta.magnitude < 0.01f) return;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width / 2f, length, width), WhiteTex);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private static void DrawArc(Vector2 center, float innerR, float outerR, float radMin, float radMax, Color color)
        {
            int segments = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(radMax - radMin) * 10f));
            float step = (radMax - radMin) / segments;
            for (int i = 0; i <= segments; i++)
            {
                float rad = radMin + i * step;
                Vector2 dir = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
                DrawLine(center + dir * innerR, center + dir * outerR, color, 2f);
            }
        }
    }
}

