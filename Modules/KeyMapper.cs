using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class KeyMapper : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_keymapper");
        public override ModuleCategory Category => ModuleCategory.Misc;
        public override void OnEnable()  => ModSettings.IsKeyMapperEnabled = true;
        public override void OnDisable() => ModSettings.IsKeyMapperEnabled = false;

        private int _bindingIndex = -1;
        private bool _bindingIsSource;

        public override float GetSettingsHeight()
        {
            return 30f + ModSettings.KeyMappings.Count * 64f;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            float halfW = (width - 20f) * 0.47f;
            float gap   = (width - 20f) * 0.06f;

            // ── 绑定中的按键捕获 ──
            if (_bindingIndex >= 0 && _bindingIndex < ModSettings.KeyMappings.Count && e.type == EventType.KeyDown)
            {
                var entry = ModSettings.KeyMappings[_bindingIndex];
                KeyCode pressed = e.keyCode;
                if (pressed == KeyCode.Escape)
                {
                    if (_bindingIsSource) entry.Source = KeyCode.None;
                    else                  entry.Target = KeyCode.None;
                }
                else if (pressed != KeyCode.None)
                {
                    if (_bindingIsSource) entry.Source = pressed;
                    else                  entry.Target = pressed;
                }
                _bindingIndex = -1;
                e.Use();
            }

            // ── 逐项绘制 ──
            for (int i = 0; i < ModSettings.KeyMappings.Count; i++)
            {
                var entry = ModSettings.KeyMappings[i];
                float row1Y = y;

                string srcLabel = (_bindingIndex == i && _bindingIsSource)
                    ? Utils.I18n.Get("km_press_key")
                    : Utils.I18n.Get("km_source_key") + ": " + entry.Source.ToString();

                string tgtLabel = (_bindingIndex == i && !_bindingIsSource)
                    ? Utils.I18n.Get("km_press_key")
                    : Utils.I18n.Get("km_target_key") + ": " + entry.Target.ToString();

                DrawBindBtn(x + 8f, ref y, halfW, e, i, true,
                    (_bindingIndex == i && _bindingIsSource), srcLabel);

                DrawBindBtn(x + 8f + halfW + gap, ref y, halfW, e, i, false,
                    (_bindingIndex == i && !_bindingIsSource), tgtLabel);

                y = row1Y + 28f;

                float row2Y = y;
                bool bidi = entry.Bidirectional;
                CustomGUI.DrawToggle(x, ref y, width - 80f, e, Utils.I18n.Get("km_bidirectional"), ref bidi);
                entry.Bidirectional = bidi;

                Rect delR = new Rect(x + width - 72f, row2Y + 2f, 64f, 20f);
                bool delH = delR.Contains(e.mousePosition);
                GUI.color = delH ? new Color(0.85f, 0.2f, 0.2f, 0.9f) : new Color(0.5f, 0.1f, 0.1f, 0.8f);
                GUI.DrawTexture(delR, CustomGUI.WhiteTexture);
                GUI.color = Color.white;
                var ds = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                GUI.Label(delR, Utils.I18n.Get("km_delete"), ds);

                if (e.type == EventType.MouseDown && e.button == 0 && delR.Contains(e.mousePosition))
                {
                    if (_bindingIndex == i) _bindingIndex = -1;
                    ModSettings.KeyMappings.RemoveAt(i);
                    e.Use();
                    break;
                }

                y = row2Y + 30f;

                GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.5f);
                GUI.DrawTexture(new Rect(x + 8f, y, width - 16f, 1f), CustomGUI.WhiteTexture);
                GUI.color = Color.white;
                y += 3f;
            }

            CustomGUI.DrawActionButton(x, ref y, width, e,
                Utils.I18n.Get("km_add_mapping"),
                new Color(0.2f, 0.6f, 1f, 0.85f),
                () => ModSettings.KeyMappings.Add(new ModSettings.KeyMapEntry()));
        }

        private void DrawBindBtn(float x, ref float y, float w, Event e,
            int entryIdx, bool isSource, bool isActive, string label)
        {
            Rect r = new Rect(x, y, w, 22f);
            bool hover = r.Contains(e.mousePosition);

            GUI.color = isActive ? new Color(0.8f, 0.3f, 0.2f, 0.85f)
                : (hover ? new Color(0.28f, 0.28f, 0.33f, 0.9f)
                         : new Color(0.18f, 0.18f, 0.22f, 0.85f));
            GUI.DrawTexture(r, CustomGUI.WhiteTexture);
            GUI.color = Color.white;

            var s = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = isActive ? Color.white : new Color(0.78f, 0.78f, 0.78f) } };
            GUI.Label(r, label, s);

            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                if (isActive) _bindingIndex = -1;
                else { _bindingIndex = entryIdx; _bindingIsSource = isSource; }
                e.Use();
            }
        }
    }
}
