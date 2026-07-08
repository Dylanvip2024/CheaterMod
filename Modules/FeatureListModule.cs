using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.UI;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class FeatureListModule : ModuleBase
    {
        public override string Name => "Feature List";
        public override string Description => "在屏幕上显示已开启的功能列表";
        public override ModuleCategory Category => ModuleCategory.Hidden;

        public override void OnEnable()  => GlobalSettings.EnableFeatureList = true;
        public override void OnDisable() => GlobalSettings.EnableFeatureList = false;

        private bool _isDragging;
        private Vector2 _dragOffset;
        private Rect _lastBoundingRect;
        private bool _isFirstFrame = true;

        private const float LogoBaseWidth = 135f;
        private const float LogoBaseHeight = 30f;

        public override void OnGUI()
        {
            if (!GlobalSettings.EnableFeatureList) return;

            if (_isFirstFrame)
            {
                if (GlobalSettings.FeatureListRect.x == 20f && GlobalSettings.FeatureListRect.y == 20f)
                    GlobalSettings.FeatureListRect = new Rect(Mathf.Max(0f, Screen.width - 220f), 20f, 200f, 200f);
                _isFirstFrame = false;
            }

            var modManager = FullBrightPlugin.Instance?.ModManager;
            if (modManager == null) return;

            List<ModuleBase> activeModules = new List<ModuleBase>();
            foreach (var mod in modManager.GetAllModules())
            {
                if (mod.Enabled && mod.Category != ModuleCategory.Hidden)
                    activeModules.Add(mod);
            }
            if (activeModules.Count == 0 && !GlobalSettings.FeatureListShowLogo) return;

            GUIStyle measureStyle = new GUIStyle(GUI.skin.label)
            { fontSize = GlobalSettings.FeatureListFontSize, fontStyle = FontStyle.Bold };

            var entries = new List<(string name, float width)>();
            foreach (var mod in activeModules)
            {
                float w = measureStyle.CalcSize(new GUIContent(mod.Name)).x;
                entries.Add((mod.Name, w));
            }
            entries.Sort((a, b) => b.width.CompareTo(a.width));

            float lineHeight = GlobalSettings.FeatureListFontSize + 6f;
            float maxWidth = 0f;
            foreach (var e in entries) { if (e.width > maxWidth) maxWidth = e.width; }

            float padX = 10f;
            float padY = 4f;
            float logoSectionHeight = 0f;
            if (GlobalSettings.FeatureListShowLogo)
            {
                float s = GlobalSettings.FeatureListFontSize / 15f;
                logoSectionHeight = LogoBaseHeight * s + 6f * s;
                maxWidth = Mathf.Max(maxWidth, LogoBaseWidth * s + 6f * s + 80f * s);
            }

            float totalWidth = maxWidth + padX * 2f;
            float totalHeight = entries.Count * lineHeight + padY * 2f + logoSectionHeight;

            Rect featureRect = GlobalSettings.FeatureListRect;
            featureRect.width = totalWidth;
            featureRect.height = totalHeight;
            if (!_isDragging)
                GlobalSettings.FeatureListRect = new Rect(featureRect.x, featureRect.y, totalWidth, totalHeight);

            Event ev = Event.current;
            if (ev != null)
            {
                if (ev.type == EventType.MouseDown && ev.button == 0 && featureRect.Contains(ev.mousePosition))
                { _isDragging = true; _dragOffset = ev.mousePosition - new Vector2(featureRect.x, featureRect.y); ev.Use(); }
                if (_isDragging && ev.type == EventType.MouseDrag && ev.button == 0)
                {
                    float newX = ev.mousePosition.x - _dragOffset.x;
                    float newY = ev.mousePosition.y - _dragOffset.y;
                    if (GlobalSettings.EnableGridSnap && GlobalSettings.GridSize > 1f)
                    { float gs = GlobalSettings.GridSize; newX = Mathf.Round(newX / gs) * gs; newY = Mathf.Round(newY / gs) * gs; }
                    GlobalSettings.FeatureListRect = new Rect(newX, newY, featureRect.width, featureRect.height);
                    ev.Use(); return;
                }
                if (ev.type == EventType.MouseUp && ev.button == 0) _isDragging = false;
            }

            _lastBoundingRect = featureRect;
            bool isRightAlign = !GlobalSettings.FeatureListAlignLeft;
            float currentY = featureRect.y + padY;

            // Logo 尺寸跟随字体等比缩放
            float logoScale = GlobalSettings.FeatureListFontSize / 15f;
            float logoWidth = LogoBaseWidth * logoScale;
            float logoHeight = LogoBaseHeight * logoScale;
            float logoGap = 6f * logoScale;
            float logoBottomPad = 6f * logoScale;
            int versionFontSize = Mathf.RoundToInt(10f * logoScale);

            // ★ CM 标志渲染（Logo + 版本号并排）
            if (GlobalSettings.FeatureListShowLogo)
            {
                Texture2D logoTex = ResourceManager.CMLogoTexture;
                float logoY = currentY;
                float logoX = isRightAlign ? (featureRect.x + featureRect.width - padX - logoWidth) : (featureRect.x + padX);

                if (logoTex != null)
                {
                    float logoAspect = (float)logoTex.width / logoTex.height;
                    float renderW = logoWidth;
                    float renderH = renderW / logoAspect;

                    Color logoTint = GlobalSettings.FeatureListRainbow
                        ? Color.HSVToRGB((Time.realtimeSinceStartup * 0.3f) % 1f, 0.6f, 1f)
                        : GlobalSettings.FeatureListColor;

                    GUI.color = logoTint;
                    GUI.DrawTexture(new Rect(logoX, logoY, renderW, renderH), logoTex, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }

                // 版本小字（Logo 右侧）
                Color versionColor = GlobalSettings.FeatureListRainbow
                    ? Color.HSVToRGB((Time.realtimeSinceStartup * 0.3f + 0.1f) % 1f, 0.5f, 1f)
                    : new Color(GlobalSettings.FeatureListColor.r, GlobalSettings.FeatureListColor.g, GlobalSettings.FeatureListColor.b, 0.5f);

                GUIStyle versionStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = versionFontSize,
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = versionColor }
                };
                float verX = logoX + logoWidth + logoGap;
                GUI.Label(new Rect(verX, logoY, 80f * logoScale, logoHeight), FullBrightPlugin.PluginVersion, versionStyle);

                currentY += logoHeight + logoBottomPad;
            }

            // 遍历绘制模块名称
            for (int i = 0; i < entries.Count; i++)
            {
                string name = entries[i].name;
                float textW = entries[i].width;
                float rowY = currentY + i * lineHeight;
                float rowWidth = textW + 12f;
                float rowX = isRightAlign ? (featureRect.x + featureRect.width - rowWidth) : featureRect.x;

                if (GlobalSettings.FeatureListBackground)
                {
                    GUI.color = new Color(0f, 0f, 0f, 0.4f);
                    GUI.DrawTexture(new Rect(rowX, rowY, rowWidth, lineHeight), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                if (GlobalSettings.FeatureListAccentLine)
                {
                    float lineX = isRightAlign ? (featureRect.x + featureRect.width - 2f) : featureRect.x;
                    Color lineColor = GlobalSettings.FeatureListRainbow
                        ? Color.HSVToRGB((Time.realtimeSinceStartup * 0.5f + i * 0.08f) % 1f, 0.7f, 1f)
                        : GlobalSettings.FeatureListColor;
                    GUI.color = lineColor;
                    GUI.DrawTexture(new Rect(lineX, rowY, 2f, lineHeight), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                Color textColor = GlobalSettings.FeatureListRainbow
                    ? Color.HSVToRGB((Time.realtimeSinceStartup * 0.5f + i * 0.08f) % 1f, 0.6f, 1f)
                    : GlobalSettings.FeatureListColor;

                GUIStyle textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = GlobalSettings.FeatureListFontSize, fontStyle = FontStyle.Bold,
                    alignment = isRightAlign ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                    normal = { textColor = textColor }
                };
                float textX = isRightAlign ? (rowX + 4f) : (rowX + 6f);
                GUI.Label(new Rect(textX, rowY, textW, lineHeight), name, textStyle);
            }
            GUI.color = Color.white;
        }
    }
}
