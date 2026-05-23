using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class LanguageModule : ModuleBase
    {
        // 修复隐藏问题：根据你的要求，永远显示为英文 "Language"，不需要走 I18n 字典
        public override string Name => Utils.I18n.Get("mod_language");
        
        public override ModuleCategory Category => ModuleCategory.Misc;

        public override void OnEnable() { }
        public override void OnDisable() { }

        public override float GetSettingsHeight() => 35f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            float btnWidth = (width - 30) / 2f;
            Rect zhBtnRect = new Rect(x + 10, y + 5, btnWidth, 22);
            Rect enBtnRect = new Rect(x + 15 + btnWidth, y + 5, btnWidth, 22);

            // --- 状态检测 ---
            bool zhHover = zhBtnRect.Contains(e.mousePosition);
            bool enHover = enBtnRect.Contains(e.mousePosition);

            // ==========================================
            // 绘制“中文”按钮背景
            // ==========================================
            // 选中的语言使用主题高亮蓝色，未选中时检测是否悬停
            GUI.color = Settings.CurrentLanguage == AppLanguage.Chinese 
                ? new Color(0.2f, 0.6f, 1.0f, 0.85f) 
                : (zhHover ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(zhBtnRect, ClickGUIManager.WhiteTexture);

            // ==========================================
            // 绘制“English”按钮背景
            // ==========================================
            GUI.color = Settings.CurrentLanguage == AppLanguage.English 
                ? new Color(0.2f, 0.6f, 1.0f, 0.85f) 
                : (enHover ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(enBtnRect, ClickGUIManager.WhiteTexture);

            // ⚠️ 极其重要：画完背景必须重置画笔颜色为纯白，防止污染文字！
            GUI.color = Color.white;

            // ==========================================
            // 绘制按钮文字
            // ==========================================
            GUIStyle btnStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, // 居中对齐
                fontSize = 12,
                normal = { textColor = Color.white }
            };
            GUI.Label(zhBtnRect, "中文 (ZH)", btnStyle);
            GUI.Label(enBtnRect, "English (EN)", btnStyle);

            // ==========================================
            // 处理点击事件 (纯手动接管)
            // ==========================================
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (zhHover)
                {
                    Settings.CurrentLanguage = AppLanguage.Chinese;
                    e.Use(); // 吞掉事件，防止点穿 GUI 触发游戏内的开枪等操作
                }
                else if (enHover)
                {
                    Settings.CurrentLanguage = AppLanguage.English;
                    e.Use(); 
                }
            }

            y += 35f;
        }
    }
}
