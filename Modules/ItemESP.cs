using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class ItemESP : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_itemesp");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => Settings.IsItemEspEnabled = true;
        public override void OnDisable() => Settings.IsItemEspEnabled = false;

        public override float GetSettingsHeight() => 60f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_esp_wireframe"),
                ref Settings.IsItemEspWireframeEnabled);

            DrawColorPicker(x, ref y, width, e,
                Utils.I18n.Get("set_item_color") + ":",
                ref Settings.SelectedEspColor);
        }

        /// <summary>线框模式开关（模仿 KillAura.DrawCustomToggle 样式）</summary>
        internal static void DrawToggle(float x, ref float y, float width, Event e,
            string label, ref bool value)
        {
            Rect rowRect = new Rect(x, y, width, 24f);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? new Color(0.2f, 0.6f, 1.0f, 0.85f)
                : (hover ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(rowRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;

            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal = { textColor = value ? Color.white : new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(14, 0, 4, 0)
            };
            GUI.Label(rowRect, (value ? "[ON]  " : "[OFF] ") + label, s);

            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                value = !value;
                e.Use();
            }
            y += 30f;
        }

        /// <summary>纯手绘颜色选择器：横向排列色块，点击选中，当前选中加白色边框</summary>
        internal static void DrawColorPicker(float x, ref float y, float width, Event e,
            string label, ref Color currentColor)
        {
            // 提示文字
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            Rect labelRect = new Rect(x + 8, y, 65, 22);
            GUI.Label(labelRect, label, labelStyle);

            // 常用颜色数组
            Color[] colors = { Color.green, Color.red, Color.yellow, Color.cyan, Color.magenta, Color.white, Color.black };

            float blockSize = 16f;
            float startX = x + 75f;
            float blockY = y + 3f;

            for (int i = 0; i < colors.Length; i++)
            {
                Rect blockRect = new Rect(startX + i * (blockSize + 4), blockY, blockSize, blockSize);

                // 色块本体
                GUI.color = colors[i];
                GUI.DrawTexture(blockRect, ClickGUIManager.WhiteTexture);

                // 当前选中高亮：白色外框
                if (colors[i] == currentColor)
                {
                    GUI.color = Color.white;
                    float b = 1f; // 边框宽度
                    GUI.DrawTexture(new Rect(blockRect.x - b, blockRect.y - b, blockRect.width + b * 2, b), ClickGUIManager.WhiteTexture);       // 上
                    GUI.DrawTexture(new Rect(blockRect.x - b, blockRect.y + blockRect.height, blockRect.width + b * 2, b), ClickGUIManager.WhiteTexture); // 下
                    GUI.DrawTexture(new Rect(blockRect.x - b, blockRect.y, b, blockRect.height), ClickGUIManager.WhiteTexture);                  // 左
                    GUI.DrawTexture(new Rect(blockRect.x + blockRect.width, blockRect.y, b, blockRect.height), ClickGUIManager.WhiteTexture);    // 右
                }

                // 点击选中
                if (e.type == EventType.MouseDown && e.button == 0 && blockRect.Contains(e.mousePosition))
                {
                    currentColor = colors[i];
                    e.Use();
                }
            }

            GUI.color = Color.white;
            y += 28f;
        }
    }
}
