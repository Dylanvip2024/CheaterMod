using System.Collections.Generic;
using UnityEngine;

namespace FullBrightMod.Utils
{
    /// <summary>
    /// 底层几何绘制工具类 —— 所有 ESP 文字标签、世界线段、世界圆圈的绘制均收敛至此。
    /// 保留 GUIStyle 缓存防 GC 优化。
    /// </summary>
    public static class RenderUtils
    {
        private static GUIStyle _cachedEspStyle;
        private static Texture2D _whiteTexture;

        private static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                return _whiteTexture;
            }
        }

        /// <summary>
        /// 在世界坐标位置绘制 ESP 文字标签（含距离显示）
        /// </summary>
        public static void DrawEspLabel(Camera cam, Vector3 worldPos, string text, Color color, 
            int fontSize = 14, bool useRichText = false)
        {
            if (cam == null) return;
            float distance = Vector3.Distance(cam.transform.position, worldPos);
            if (distance > 150f) return;

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0) return;

            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y;

            // 【GC 优化】：强转 int 替代 ToString("F1")，消灭字符串拼接垃圾
            int distMeters = (int)(distance * 0.3f);
            string espText = text + "\n[" + distMeters + "m]";

            if (_cachedEspStyle == null)
            {
                _cachedEspStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
            _cachedEspStyle.fontSize = fontSize;
            _cachedEspStyle.richText = useRichText;
            _cachedEspStyle.normal.textColor = color;

            Rect labelRect = new Rect(guiX - 100, guiY - 35, 200, 70);
            GUI.Label(labelRect, espText, _cachedEspStyle);
        }

        /// <summary>屏幕空间绘制线段（GUI.DrawTexture 方式）</summary>
        private static void DrawScreenLine(Vector2 start, Vector2 end, Color color, float width = 2f)
        {
            Vector2 d = end - start;
            float angle = Mathf.Rad2Deg * Mathf.Atan2(d.y, d.x);
            float length = d.magnitude;

            Matrix4x4 matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);
            Color colorBackup = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(start.x, start.y - width / 2, length, width), WhiteTexture);
            GUI.color = colorBackup;
            GUI.matrix = matrixBackup;
        }

        /// <summary>世界空间线段 → 屏幕空间绘制</summary>
        public static void DrawWorldLine(Camera cam, Vector3 start, Vector3 end, Color color)
        {
            if (cam == null) return;
            Vector3 startScreen = cam.WorldToScreenPoint(start);
            Vector3 endScreen = cam.WorldToScreenPoint(end);

            if (startScreen.z > 0 && endScreen.z > 0)
            {
                DrawScreenLine(
                    new Vector2(startScreen.x, Screen.height - startScreen.y),
                    new Vector2(endScreen.x, Screen.height - endScreen.y),
                    color, 2f
                );
            }
        }

        /// <summary>世界空间圆 → 屏幕空间多边形线段绘制</summary>
        public static void DrawWorldCircle(Camera cam, Vector3 center, float radius, Color color, int segments = 36)
        {
            if (cam == null) return;
            float angleStep = (Mathf.PI * 2f) / segments;
            Vector3 prevWorld = center + new Vector3(Mathf.Cos(0) * radius, Mathf.Sin(0) * radius, 0);
            Vector3 prevScreen = cam.WorldToScreenPoint(prevWorld);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep;
                Vector3 nextWorld = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Vector3 nextScreen = cam.WorldToScreenPoint(nextWorld);

                if (prevScreen.z > 0 && nextScreen.z > 0)
                {
                    DrawScreenLine(
                        new Vector2(prevScreen.x, Screen.height - prevScreen.y),
                        new Vector2(nextScreen.x, Screen.height - nextScreen.y),
                        color, 2f
                    );
                }
                prevScreen = nextScreen;
            }
        }
    }
}
