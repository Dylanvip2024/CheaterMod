using System.Collections.Generic;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Utils
{
    /// <summary>
    /// NotificationManager — 模块开关 Toast 通知系统。
    /// 纯 IMGUI 渲染，带滑入/滑出/堆叠动画，最多 4 条。
    /// </summary>
    public static class NotificationManager
    {
        private class Notification
        {
            public string ModuleName;
            public bool State;           // true=开启, false=关闭
            public float TimeAlive;
            public float TargetY;
            public float CurrentY;
            public float CurrentX;
        }

        private static readonly List<Notification> _notifications = new List<Notification>();
        private const float LIFETIME = 3.0f;
        private const float ANIM_DURATION = 0.3f;
        private const float ENTRY_HEIGHT = 36f;
        private const float ENTRY_GAP = 4f;
        // 使用 GlobalSettings.NotificationYPosition 动态控制起始 Y
        // 使用 GlobalSettings.NotificationSlideFromRight 控制滑入方向

        /// <summary>显示一条通知</summary>
        public static void Show(string moduleName, bool state)
        {
            if (!GlobalSettings.EnableNotifications) return;

            while (_notifications.Count >= 4)
                _notifications.RemoveAt(0);

            float startY = GlobalSettings.NotificationYPosition;
            float startX = XHidden();

            _notifications.Add(new Notification
            {
                ModuleName = moduleName,
                State = state,
                TimeAlive = 0f,
                CurrentX = startX,
                CurrentY = startY,
                TargetY = startY
            });

            RecalcTargets();
        }

        private static float XHidden()
        {
            if (GlobalSettings.NotificationSlideFromRight)
                return Screen.width + 220f;  // 隐藏在屏幕右侧外
            return -220f;                     // 隐藏在屏幕左侧外
        }

        private static float XVisible()
        {
            if (GlobalSettings.NotificationSlideFromRight)
                return Screen.width - 200f;  // 右侧可见位置
            return 20f;                       // 左侧可见位置
        }

        private static void RecalcTargets()
        {
            float baseY = GlobalSettings.NotificationYPosition;
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                _notifications[i].TargetY = baseY;
                baseY += ENTRY_HEIGHT + ENTRY_GAP;
            }
        }

        /// <summary>每帧调用（放在 Plugin.OnGUI 中）</summary>
        public static void Draw()
        {
            if (!GlobalSettings.EnableNotifications) return;
            if (_notifications.Count == 0) return;

            float deltaTime = Time.deltaTime;

            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                var n = _notifications[i];
                n.TimeAlive += deltaTime;

                // 移除过期通知
                if (n.TimeAlive >= LIFETIME)
                {
                    _notifications.RemoveAt(i);
                    continue;
                }

                // ── X 轴动画 ──
                float fadeInEnd = ANIM_DURATION;
                float fadeOutStart = LIFETIME - ANIM_DURATION;

                if (n.TimeAlive < fadeInEnd)
                {
                    float t = n.TimeAlive / fadeInEnd;
                    n.CurrentX = Mathf.Lerp(XHidden(), XVisible(), t);
                }
                else if (n.TimeAlive > fadeOutStart)
                {
                    float t = (n.TimeAlive - fadeOutStart) / ANIM_DURATION;
                    n.CurrentX = Mathf.Lerp(XVisible(), XHidden(), t);
                }
                else
                {
                    n.CurrentX = XVisible();
                }

                // ── Y 轴动画 ──
                n.CurrentY = Mathf.Lerp(n.CurrentY, n.TargetY, deltaTime * 8f);

                // ── 透明度 ──
                float alpha;
                if (n.TimeAlive < fadeInEnd)
                    alpha = n.TimeAlive / fadeInEnd;
                else if (n.TimeAlive > fadeOutStart)
                    alpha = 1f - (n.TimeAlive - fadeOutStart) / ANIM_DURATION;
                else
                    alpha = 1f;

                // ── 绘制 ──
                Color accentColor = n.State
                    ? new Color(0.2f, 0.7f, 0.3f)  // 绿色：开启
                    : new Color(0.9f, 0.3f, 0.25f); // 红色：关闭

                Color bgColor = new Color(0f, 0f, 0f, 0.55f * alpha);
                Color lineColor = new Color(accentColor.r, accentColor.g, accentColor.b, alpha);
                Color textColor = new Color(1f, 1f, 1f, alpha);

                float entryW = 180f;
                float entryH = ENTRY_HEIGHT;
                Rect entryRect = new Rect(n.CurrentX, n.CurrentY, entryW, entryH);

                // 背景框
                GUI.color = bgColor;
                GUI.DrawTexture(entryRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // 左侧强调线
                Rect lineRect = new Rect(entryRect.x, entryRect.y, 3f, entryRect.height);
                GUI.color = lineColor;
                GUI.DrawTexture(lineRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // 文字
                string label = n.State ? "[ON]  " : "[OFF] ";
                label += n.ModuleName;

                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = textColor },
                    padding = new RectOffset(10, 6, 0, 0)
                };
                GUI.Label(entryRect, label, style);
            }
        }
    }
}
