using UnityEngine;
using FullBrightMod.Utils;

namespace FullBrightMod.UI
{
    public static class CustomGUI
    {
        private static readonly Color ColorOn     = new Color(0.2f, 0.6f, 1.0f, 0.85f);
        private static readonly Color ColorOff    = new Color(0.16f, 0.16f, 0.18f, 0.90f);
        private static readonly Color ColorHover  = new Color(0.22f, 0.22f, 0.25f, 0.90f);
        private static readonly Color ColorLabel  = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color ColorWhite  = Color.white;
        private static readonly Color ColorOnText = Color.white;
        private static readonly Color ColorOffText = new Color(0.7f, 0.7f, 0.7f);

        private static Texture2D _whiteTex;
        internal static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTex == null)
                {
                    _whiteTex = new Texture2D(1, 1);
                    _whiteTex.SetPixel(0, 0, Color.white);
                    _whiteTex.Apply();
                    _whiteTex.hideFlags = HideFlags.HideAndDontSave;
                }
                return _whiteTex;
            }
        }

        // ============================================================
        //  悬浮调色板状�?        // ============================================================
        public static string ActiveColorPickerId = null;
        public static Color ActiveColorBuffer = Color.white;
        public static Rect ColorPickerRect = new Rect(0, 0, 260, 310);

        private static string _hexInputBuffer = "";
        private static float _pickerHue, _pickerSat, _pickerVal, _pickerAlpha;
        private static Texture2D _hueTex, _svTex;
        private static Color _pickerOriginalColor; // 取消时恢�?
        // ============================================================
        //  Toggle / Slider �?保持不变
        // ============================================================
        public static void DrawToggle(float x, ref float y, float width, Event e, string label, ref bool value)
        {
            Rect rowRect = new Rect(x, y, width, 24f);
            bool hover = rowRect.Contains(e.mousePosition);
            GUI.color = value ? ColorOn : (hover ? ColorHover : ColorOff);
            GUI.DrawTexture(rowRect, WhiteTexture);
            GUI.color = ColorWhite;

            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11, fontStyle = FontStyle.Bold,
                normal = { textColor = value ? ColorOnText : ColorOffText },
                padding = new RectOffset(14, 0, 4, 0)
            };
            GUI.Label(rowRect, (value ? "[ON]  " : "[OFF] ") + label, s);
            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            { value = !value; e.Use(); }
            y += 28f;
        }

        public static void DrawSlider(float x, ref float y, float width, Event e, string label, ref float value, float min, float max)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 10, normal = { textColor = ColorLabel }, padding = new RectOffset(14, 0, 2, 0) };
            GUI.Label(new Rect(x, y, width, 18f), label, labelStyle);

            Rect sliderRect = new Rect(x + 14f, y + 18f, width - 28f, 8f);
            float newVal = ClickGUIManager.DrawSlider(sliderRect, value, min, max, e);
            if (Mathf.Abs(newVal - value) > 0.001f) value = newVal;
            y += 28f;
        }

        // ============================================================
        //  DrawColorPicker �?预览�?+ [调色板] 按钮
        // ============================================================
        public static void DrawColorPicker(float x, ref float y, float width, Event e, string label, ref Color currentColor)
        {
            string pickerId = label.GetHashCode().ToString();
            if (ActiveColorPickerId == pickerId)
                currentColor = ActiveColorBuffer;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.MiddleLeft, fontSize = 10, normal = { textColor = ColorLabel }, padding = new RectOffset(0, 0, 0, 0) };
            GUI.Label(new Rect(x + 8, y, 65, 22), label, labelStyle);

            // 颜色预览
            Rect previewRect = new Rect(x + 75, y + 3, 22, 18);
            GUI.color = currentColor;
            GUI.DrawTexture(previewRect, WhiteTexture);
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            float t = 1f;
            GUI.DrawTexture(new Rect(previewRect.x - t, previewRect.y - t, previewRect.width + 2, t), WhiteTexture);
            GUI.DrawTexture(new Rect(previewRect.x - t, previewRect.y + previewRect.height, previewRect.width + 2, t), WhiteTexture);
            GUI.DrawTexture(new Rect(previewRect.x - t, previewRect.y, t, previewRect.height), WhiteTexture);
            GUI.DrawTexture(new Rect(previewRect.x + previewRect.width, previewRect.y, t, previewRect.height), WhiteTexture);
            GUI.color = ColorWhite;

            // [调色板] 按钮
            Rect btnRect = new Rect(x + 100, y + 2, 55, 20);
            bool btnHover = btnRect.Contains(e.mousePosition);
            GUI.color = ActiveColorPickerId == pickerId ? ColorOn : (btnHover ? ColorHover : new Color(0.25f, 0.25f, 0.3f, 0.85f));
            GUI.DrawTexture(btnRect, WhiteTexture);
            GUI.color = ColorWhite;
            GUIStyle btnStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = ColorWhite } };
            GUI.Label(btnRect, Utils.I18n.Get("ui_color_palette"), btnStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                if (ActiveColorPickerId == pickerId)
                {
                    ActiveColorPickerId = null;
                }
                else
                {
                    ActiveColorPickerId = pickerId;
                    ActiveColorBuffer = currentColor;
                    _pickerOriginalColor = currentColor;
                    Color.RGBToHSV(currentColor, out _pickerHue, out _pickerSat, out _pickerVal);
                    _pickerAlpha = currentColor.a;
                    ColorPickerRect.x = btnRect.x;
                    ColorPickerRect.y = btnRect.y + 24;
                    InitPaletteTextures();
                    UpdateSVTexture();
                    UpdateColorFromHSV();
                }
                e.Use();
            }
            y += 28f;
        }

        // ============================================================
        //  贴图初始�?        // ============================================================
        private static void InitPaletteTextures()
        {
            if (_hueTex == null)
            {
                _hueTex = new Texture2D(1, 7, TextureFormat.RGB24, false)
                { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                for (int i = 0; i <= 6; i++)
                    _hueTex.SetPixel(0, i, Color.HSVToRGB(i / 6f, 1f, 1f));
                _hueTex.Apply();
            }
            if (_svTex == null)
            {
                _svTex = new Texture2D(2, 2, TextureFormat.RGB24, false)
                { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            }
        }

        private static void UpdateSVTexture()
        {
            if (_svTex == null) return;
            Color hueColor = Color.HSVToRGB(_pickerHue, 1f, 1f);
            _svTex.SetPixel(0, 1, Color.white);
            _svTex.SetPixel(1, 1, hueColor);
            _svTex.SetPixel(0, 0, Color.black);
            _svTex.SetPixel(1, 0, Color.black);
            _svTex.Apply();
        }

        private static void UpdateColorFromHSV()
        {
            ActiveColorBuffer = Color.HSVToRGB(_pickerHue, _pickerSat, _pickerVal);
            ActiveColorBuffer.a = _pickerAlpha;
            _hexInputBuffer = ColorUtility.ToHtmlStringRGBA(ActiveColorBuffer);
        }

        // ============================================================
        //  DrawFloatingColorPicker �?悬浮窗渲�?        // ============================================================
        public static void DrawFloatingColorPicker(Event e)
        {
            if (ActiveColorPickerId == null) return;

            Rect r = ColorPickerRect;
            float px = r.x, py = r.y, pw = r.width;
            float ph = 305;

            // ── 背景 ──
            var bg = new Rect(px - 3, py - 3, pw + 6, ph);
            GUI.color = new Color(0.11f, 0.11f, 0.13f, 0.97f);
            GUI.DrawTexture(bg, WhiteTexture);
            GUI.color = ColorOn;
            GUI.DrawTexture(new Rect(px - 3, py - 3, pw + 6, 2), WhiteTexture);
            GUI.color = ColorWhite;

            GUIStyle small = new GUIStyle(GUI.skin.label) { fontSize = 9, normal = { textColor = ColorLabel }, padding = new RectOffset(0, 0, 0, 0) };
            GUIStyle boldSmall = new GUIStyle(GUI.skin.label) { fontSize = 9, fontStyle = FontStyle.Bold, normal = { textColor = ColorLabel }, padding = new RectOffset(0, 0, 0, 0) };
            GUIStyle hexStyle = new GUIStyle(GUI.skin.textField) { fontSize = 10, normal = { textColor = ColorWhite, background = WhiteTexture }, padding = new RectOffset(3, 3, 1, 1) };
            GUIStyle btnLbl = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = ColorWhite } };
            GUIStyle centerSmall = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter, normal = { textColor = ColorLabel } };

            float y = py + 6;

            // ── 标题�?──
            GUI.Label(new Rect(px + 8, y, 60, 16), Utils.I18n.Get("ui_color_edit"), boldSmall);
            // 右上角预览方块
            Rect topPreviewRect = new Rect(px + pw - 32, y - 2, 24, 24);
            GUI.color = ActiveColorBuffer;
            GUI.DrawTexture(topPreviewRect, WhiteTexture);
            GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
            float tb = 1f;
            GUI.DrawTexture(new Rect(topPreviewRect.x - tb, topPreviewRect.y - tb, topPreviewRect.width + 2, tb), WhiteTexture);
            GUI.DrawTexture(new Rect(topPreviewRect.x - tb, topPreviewRect.y + topPreviewRect.height, topPreviewRect.width + 2, tb), WhiteTexture);
            GUI.DrawTexture(new Rect(topPreviewRect.x - tb, topPreviewRect.y, tb, topPreviewRect.height), WhiteTexture);
            GUI.DrawTexture(new Rect(topPreviewRect.x + topPreviewRect.width, topPreviewRect.y, tb, topPreviewRect.height), WhiteTexture);
            GUI.color = ColorWhite;
            y += 28;

            // ── 二维 SV 调色�?+ Hue 彩虹�?──
            float svSize = 130f;
            float svX = px + 8;
            float svY = y;
            Rect svRect = new Rect(svX, svY, svSize, svSize);

            // 绘制 SV 渐变
            InitPaletteTextures();
            GUI.DrawTexture(svRect, _svTex, ScaleMode.StretchToFill);
            // 边框
            DrawBoxBorder(svRect, ColorWhite);

            // ── 准星（S-V 当前位置�?──
            float crossX = svX + _pickerSat * svSize;
            float crossY = svY + (1f - _pickerVal) * svSize;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(crossX - 3, crossY - 1, 7, 1), WhiteTexture);
            GUI.DrawTexture(new Rect(crossX - 1, crossY - 3, 1, 7), WhiteTexture);
            GUI.color = ColorWhite;
            GUI.DrawTexture(new Rect(crossX - 2, crossY - 1, 5, 1), WhiteTexture);
            GUI.DrawTexture(new Rect(crossX - 1, crossY - 2, 1, 5), WhiteTexture);

            // 监听 SV 区鼠标
            if (svRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    _pickerSat = Mathf.Clamp01((e.mousePosition.x - svX) / svSize);
                    _pickerVal = Mathf.Clamp01(1f - (e.mousePosition.y - svY) / svSize);
                    UpdateColorFromHSV();
                    e.Use();
                }
            }

            // ── Hue 彩虹�?──
            float hueBarW = 20f;
            float hueBarX = svX + svSize + 10;
            Rect hueRect = new Rect(hueBarX, svY, hueBarW, svSize);
            GUI.DrawTexture(hueRect, _hueTex, ScaleMode.StretchToFill);
            DrawBoxBorder(hueRect, ColorWhite);

            // Hue 游标
            float hueCursorY = svY + svSize - (_pickerHue * svSize);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(hueBarX - 2, hueCursorY - 1, hueBarW + 4, 3), WhiteTexture);
            GUI.color = ColorWhite;
            GUI.DrawTexture(new Rect(hueBarX - 1, hueCursorY, hueBarW + 2, 1), WhiteTexture);

            // 监听 Hue 条鼠标
            if (hueRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    _pickerHue = Mathf.Clamp01(1f - (e.mousePosition.y - svY) / svSize);
                    UpdateSVTexture();
                    UpdateColorFromHSV();
                    e.Use();
                }
            }

            // ── RGB 数值显�?──
            float infoX = hueBarX + hueBarW + 8;
            GUI.Label(new Rect(infoX, svY, 40, 14), "R", small);
            GUI.Label(new Rect(infoX, svY + 16, 40, 14), "G", small);
            GUI.Label(new Rect(infoX, svY + 32, 40, 14), "B", small);
            GUI.Label(new Rect(infoX, svY + 50, 40, 14), "A", small);
            Color bc = ActiveColorBuffer;
            GUIStyle valStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleRight, normal = { textColor = ColorWhite } };
            GUI.Label(new Rect(infoX + 14, svY, 40, 14), ((int)(bc.r * 255)).ToString(), valStyle);
            GUI.Label(new Rect(infoX + 14, svY + 16, 40, 14), ((int)(bc.g * 255)).ToString(), valStyle);
            GUI.Label(new Rect(infoX + 14, svY + 32, 40, 14), ((int)(bc.b * 255)).ToString(), valStyle);
            GUI.Label(new Rect(infoX + 14, svY + 50, 40, 14), ((int)(bc.a * 255)).ToString(), valStyle);

            // ── Alpha 滑块 ──
            y = svY + svSize + 14;
            GUI.Label(new Rect(px + 8, y, 30, 14), "A", small);
            float alphaVal = _pickerAlpha;
            float newAlpha = GUI.HorizontalSlider(new Rect(px + 32, y + 2, pw - 112, 12), alphaVal, 0f, 1f);
            if (Mathf.Abs(newAlpha - alphaVal) > 0.0001f) { _pickerAlpha = newAlpha; UpdateColorFromHSV(); }
            GUIStyle av = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleRight, normal = { textColor = ColorLabel } };
            GUI.Label(new Rect(px + pw - 84, y, 30, 14), ((int)(alphaVal * 255)).ToString(), av);
            // Alpha 预览条
            Rect alphaBar = new Rect(px + pw - 50, y + 1, 42, 14);
            GUI.color = new Color(bc.r, bc.g, bc.b, 1f);
            GUI.DrawTexture(alphaBar, WhiteTexture);
            // checkerboard
            GUI.color = ColorWhite;
            DrawBoxBorder(alphaBar, new Color(0.4f, 0.4f, 0.4f, 0.6f));
            y += 26;

            // ── Hex 输入�?──
            GUI.Label(new Rect(px + 8, y, 30, 16), "Hex", small);
            GUI.SetNextControlName("ColorHexInput");
            string newHex = GUI.TextField(new Rect(px + 36, y, pw - 76, 18), _hexInputBuffer, hexStyle);
            if (newHex != _hexInputBuffer)
            {
                _hexInputBuffer = newHex;
                Color parsed;
                if (ColorUtility.TryParseHtmlString(newHex.Length == 6 || newHex.Length == 8
                    ? "#" + _hexInputBuffer : _hexInputBuffer, out parsed))
                {
                    ActiveColorBuffer = parsed;
                    _pickerAlpha = parsed.a;
                    // 避免纯黑�?Hue 丢失 �?只在非纯黑时更新 Hue
                    float h, s, v;
                    Color.RGBToHSV(parsed, out h, out s, out v);
                    _pickerSat = s;
                    _pickerVal = v;
                    if (s > 0.001f) _pickerHue = h;
                    UpdateSVTexture();
                }
            }
            y += 26;

            // ── 预设色块 ──
            Color[] presets = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta, Color.white, Color.black };
            float blockSize = 14f;
            float blockY = y;
            for (int i = 0; i < presets.Length; i++)
            {
                Rect br = new Rect(px + 8 + i * (blockSize + 4), blockY, blockSize, blockSize);
                GUI.color = presets[i];
                GUI.DrawTexture(br, WhiteTexture);
                if (presets[i] == ActiveColorBuffer)
                {
                    GUI.color = ColorWhite;
                    float bf = 1f;
                    GUI.DrawTexture(new Rect(br.x - bf, br.y - bf, br.width + bf * 2, bf), WhiteTexture);
                    GUI.DrawTexture(new Rect(br.x - bf, br.y + br.height, br.width + bf * 2, bf), WhiteTexture);
                    GUI.DrawTexture(new Rect(br.x - bf, br.y, bf, br.height), WhiteTexture);
                    GUI.DrawTexture(new Rect(br.x + br.width, br.y, bf, br.height), WhiteTexture);
                }
                if (e.type == EventType.MouseDown && e.button == 0 && br.Contains(e.mousePosition))
                {
                    ActiveColorBuffer = presets[i];
                    Color.RGBToHSV(presets[i], out _pickerHue, out _pickerSat, out _pickerVal);
                    _pickerAlpha = presets[i].a;
                    _hexInputBuffer = ColorUtility.ToHtmlStringRGBA(presets[i]);
                    if (_pickerSat > 0.001f) UpdateSVTexture();
                    e.Use();
                }
            }
            GUI.color = ColorWhite;
            y += 24;

            // ── [取消] / [确定] 按钮 ──
            Rect cancelRect = new Rect(px + 8, y, 56, 20);
            GUI.color = new Color(0.35f, 0.35f, 0.35f, 0.8f);
            GUI.DrawTexture(cancelRect, WhiteTexture);
            GUI.color = ColorWhite;
            GUI.Label(cancelRect, Utils.I18n.Get("ui_cancel"), btnLbl);
            if (e.type == EventType.MouseDown && e.button == 0 && cancelRect.Contains(e.mousePosition))
            {
                // 恢复编辑前的原始颜色
                ActiveColorBuffer = _pickerOriginalColor;
                Color.RGBToHSV(_pickerOriginalColor, out _pickerHue, out _pickerSat, out _pickerVal);
                _pickerAlpha = _pickerOriginalColor.a;
                UpdateSVTexture();
                _hexInputBuffer = ColorUtility.ToHtmlStringRGBA(_pickerOriginalColor);
                ActiveColorPickerId = null;
                e.Use();
            }

            Rect applyRect = new Rect(px + pw - 72, y, 56, 20);
            GUI.color = ColorOn;
            GUI.DrawTexture(applyRect, WhiteTexture);
            GUI.color = ColorWhite;
            GUI.Label(applyRect, Utils.I18n.Get("ui_confirm"), btnLbl);
            if (e.type == EventType.MouseDown && e.button == 0 && applyRect.Contains(e.mousePosition))
            { ActiveColorPickerId = null; e.Use(); }

            // 点击窗口外 — 取消并恢复原色
            if (e.type == EventType.MouseDown && !bg.Contains(e.mousePosition))
            {
                ActiveColorBuffer = _pickerOriginalColor;
                Color.RGBToHSV(_pickerOriginalColor, out _pickerHue, out _pickerSat, out _pickerVal);
                _pickerAlpha = _pickerOriginalColor.a;
                UpdateSVTexture();
                _hexInputBuffer = ColorUtility.ToHtmlStringRGBA(_pickerOriginalColor);
                ActiveColorPickerId = null;
            }
        }

        // ============================================================
        //  工具
        // ============================================================
        private static void DrawBoxBorder(Rect r, Color c)
        {
            float t = 1f;
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), WhiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height, r.width, t), WhiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), WhiteTexture);
            GUI.DrawTexture(new Rect(r.x + r.width, r.y, t, r.height), WhiteTexture);
            GUI.color = ColorWhite;
        }

        // ============================================================
        //  TextField / Selector / ActionButton �?保持不变
        // ============================================================
        public static void DrawTextField(float x, ref float y, float width, string label, ref string value, bool isPassword = false)
        {
            GUI.color = ColorWhite;
            GUIStyle fieldLabelStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 10, normal = { textColor = ColorLabel }, padding = new RectOffset(14, 0, 2, 0) };
            GUI.Label(new Rect(x, y, width, 16f), label, fieldLabelStyle);

            GUIStyle inputStyle = new GUIStyle(GUI.skin.textField)
            { fontSize = 10, normal = { textColor = ColorWhite, background = WhiteTexture }, padding = new RectOffset(4, 4, 2, 2) };
            Rect fieldRect = new Rect(x + 14, y + 16f, width - 28, 20f);

            if (isPassword)
            {
                string newVal = GUI.PasswordField(fieldRect, value, '*', inputStyle);
                if (newVal != value) value = newVal;
            }
            else
            {
                string newVal = GUI.TextField(fieldRect, value, inputStyle);
                if (newVal != value) value = newVal;
            }
            y += 40f;
        }

        public static void DrawSelector(float x, ref float y, float width, Event e, string label, string[] options, ref int currentIndex)
        {
            GUI.color = ColorWhite;
            if (options == null || options.Length == 0) return;
            currentIndex = Mathf.Clamp(currentIndex, 0, options.Length - 1);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.MiddleLeft, fontSize = 10, normal = { textColor = ColorLabel }, padding = new RectOffset(0, 0, 0, 0) };
            GUI.Label(new Rect(x + 8, y, 70, 22), $"  {label}", labelStyle);

            Rect leftBtnRect = new Rect(x + 78, y + 2, 22, 18);
            if (GUI.Button(leftBtnRect, "\u25c0", SelectorBtnStyle))
            {
                currentIndex--;
                if (currentIndex < 0) currentIndex = options.Length - 1;
            }

            GUIStyle centerStyle = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = ColorWhite } };
            GUI.Label(new Rect(x + 100, y, width - 130, 22), options[currentIndex], centerStyle);

            Rect rightBtnRect = new Rect(x + width - 28, y + 2, 22, 18);
            if (GUI.Button(rightBtnRect, "\u25b6", SelectorBtnStyle))
            {
                currentIndex++;
                if (currentIndex >= options.Length) currentIndex = 0;
            }
            y += 30f;
        }

        public static void DrawActionButton(float x, ref float y, float width, Event e, string label, Color activeColor, System.Action onClick)
        {
            Rect btnRect = new Rect(x + 8, y + 4, width - 16, 22);
            bool hover = btnRect.Contains(e.mousePosition);
            GUI.color = hover ? activeColor * 1.2f : activeColor;
            GUI.DrawTexture(btnRect, WhiteTexture);
            GUI.color = ColorWhite;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.label)
            { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = ColorWhite } };
            GUI.Label(btnRect, label, btnStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            { onClick?.Invoke(); e.Use(); }
            y += 30f;
        }

        private static GUIStyle _selectorBtnStyle;
        private static GUIStyle SelectorBtnStyle
        {
            get
            {
                if (_selectorBtnStyle == null)
                {
                    _selectorBtnStyle = new GUIStyle(GUI.skin.button)
                    { fontSize = 12, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 0, 0), border = new RectOffset(0, 0, 0, 0) };
                    _selectorBtnStyle.normal.background = MakeColorTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 0.9f));
                    _selectorBtnStyle.hover.background = MakeColorTex(2, 2, new Color(0.35f, 0.35f, 0.35f, 1f));
                    _selectorBtnStyle.active.background = MakeColorTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f));
                    _selectorBtnStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                    _selectorBtnStyle.hover.textColor = ColorWhite;
                    _selectorBtnStyle.active.textColor = new Color(0.5f, 0.5f, 0.5f);
                }
                return _selectorBtnStyle;
            }
        }

        private static Texture2D MakeColorTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }
    }
}
