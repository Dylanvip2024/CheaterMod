using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// FeatureListModule（功能列表 / ArrayList）
    /// 在屏幕上渲染当前已开启的所有模块名称。
    /// 支持拖拽、左/右对齐、彩虹模式、背景框、强调线。
    /// 不在普通 Modules 面板中显示（Category=999）。
    /// </summary>
    public class FeatureListModule : ModuleBase
    {
        public override string Name => "Feature List";
        public override string Description => "在屏幕上显示已开启的功能列表";
        public override ModuleCategory Category => (ModuleCategory)999;

        public override void OnEnable()  => GlobalSettings.EnableFeatureList = true;
        public override void OnDisable() => GlobalSettings.EnableFeatureList = false;

        // 拖拽状态
        private bool _isDragging;
        private Vector2 _dragOffset;
        private Rect _lastBoundingRect;

        /// <summary>首帧标记：用于将默认位置移动到屏幕右上角</summary>
        private bool _isFirstFrame = true;

        public override void OnGUI()
        {
            if (!GlobalSettings.EnableFeatureList) return;

            // ★ 首帧初始化：默认位置移动到屏幕右上角
            if (_isFirstFrame)
            {
                if (GlobalSettings.FeatureListRect.x == 20f && GlobalSettings.FeatureListRect.y == 20f)
                {
                    GlobalSettings.FeatureListRect = new Rect(
                        Mathf.Max(0f, Screen.width - 220f), 20f, 200f, 200f);
                }
                _isFirstFrame = false;
            }

            var modManager = FullBrightPlugin.Instance?.ModManager;
            if (modManager == null) return;

            // 1. 收集所有已开启且非隐藏的模块
            List<ModuleBase> activeModules = new List<ModuleBase>();
            foreach (var mod in modManager.GetAllModules())
            {
                if (mod.Enabled && mod.Category != (ModuleCategory)999)
                    activeModules.Add(mod);
            }
            if (activeModules.Count == 0) return;

            // 2. 创建临时样式计算宽度
            GUIStyle measureStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = GlobalSettings.FeatureListFontSize,
                fontStyle = FontStyle.Bold
            };

            // 3. 计算每个名称的渲染宽度并按宽度降序排序
            var entries = new List<(string name, float width)>();
            foreach (var mod in activeModules)
            {
                float w = measureStyle.CalcSize(new GUIContent(mod.Name)).x;
                entries.Add((mod.Name, w));
            }
            entries.Sort((a, b) => b.width.CompareTo(a.width));

            // 4. 计算总 bounding rect
            float lineHeight = GlobalSettings.FeatureListFontSize + 6f;
            float maxWidth = 0f;
            foreach (var e in entries)
            {
                if (e.width > maxWidth) maxWidth = e.width;
            }
            float padX = 10f;
            float padY = 4f;
            float totalWidth = maxWidth + padX * 2f;
            float totalHeight = entries.Count * lineHeight + padY * 2f;

            Rect featureRect = GlobalSettings.FeatureListRect;
            featureRect.width = totalWidth;
            featureRect.height = totalHeight;

            // 5. 拖拽逻辑
            Event ev = Event.current;
            if (ev != null)
            {
                if (ev.type == EventType.MouseDown && ev.button == 0 && featureRect.Contains(ev.mousePosition))
                {
                    _isDragging = true;
                    _dragOffset = ev.mousePosition - new Vector2(featureRect.x, featureRect.y);
                    ev.Use();
                }
                if (_isDragging && ev.type == EventType.MouseDrag && ev.button == 0)
                {
                    float newX = ev.mousePosition.x - _dragOffset.x;
                    float newY = ev.mousePosition.y - _dragOffset.y;

                    if (GlobalSettings.EnableGridSnap && GlobalSettings.GridSize > 1f)
                    {
                        float gs = GlobalSettings.GridSize;
                        newX = Mathf.Round(newX / gs) * gs;
                        newY = Mathf.Round(newY / gs) * gs;
                    }

                    GlobalSettings.FeatureListRect = new Rect(newX, newY, featureRect.width, featureRect.height);
                    ev.Use();
                    return;
                }
                if (ev.type == EventType.MouseUp && ev.button == 0)
                    _isDragging = false;
            }

            _lastBoundingRect = featureRect;

            bool isRightAlign = !GlobalSettings.FeatureListAlignLeft;

            // 6. 遍历绘制
            for (int i = 0; i < entries.Count; i++)
            {
                string name = entries[i].name;
                float textW = entries[i].width;
                float currentY = featureRect.y + padY + i * lineHeight;

                float rowWidth = textW + 12f;

                float rowX = isRightAlign
                    ? (featureRect.x + featureRect.width - rowWidth)
                    : featureRect.x;

                if (GlobalSettings.FeatureListBackground)
                {
                    Rect bgRect = new Rect(rowX, currentY, rowWidth, lineHeight);
                    GUI.color = new Color(0f, 0f, 0f, 0.4f);
                    GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                if (GlobalSettings.FeatureListAccentLine)
                {
                    float lineX = isRightAlign
                        ? (featureRect.x + featureRect.width - 2f)
                        : featureRect.x;
                    Rect lineRect = new Rect(lineX, currentY, 2f, lineHeight);

                    Color lineColor = GlobalSettings.FeatureListRainbow
                        ? Color.HSVToRGB((Time.realtimeSinceStartup * 0.5f + i * 0.08f) % 1f, 0.7f, 1f)
                        : GlobalSettings.FeatureListColor;

                    GUI.color = lineColor;
                    GUI.DrawTexture(lineRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                Color textColor = GlobalSettings.FeatureListRainbow
                    ? Color.HSVToRGB((Time.realtimeSinceStartup * 0.5f + i * 0.08f) % 1f, 0.6f, 1f)
                    : GlobalSettings.FeatureListColor;

                GUIStyle textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = GlobalSettings.FeatureListFontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = isRightAlign ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                    normal = { textColor = textColor }
                };

                float textX = isRightAlign ? (rowX + 4f) : (rowX + 6f);
                Rect textRect = new Rect(textX, currentY, textW, lineHeight);
                GUI.Label(textRect, name, textStyle);
            }
            GUI.color = Color.white;
        }
    }
}
