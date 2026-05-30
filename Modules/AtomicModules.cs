using FullBrightMod.Core;
using FullBrightMod.UI;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules
{
    // ============================================================
    //  原子功能模块 —— 每个模块仅负责一个功能的开关切换
    //  已单独提取到独立文件的模块：ItemESP, CreatureESP, TrapESP,
    //    VisionExpand, CameraZoom, FullBright, PlayerESP
    // ============================================================

    public class FullBright : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_fullbright");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => Settings.IsFullBrightEnabled = true;
        public override void OnDisable() => Settings.IsFullBrightEnabled = false;

        // ==== 自定义设置：光照强度滑动条 ====
        public override float GetSettingsHeight() => 24f; // 一行滑块高度

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // 滑动条矩形区域
            Rect sliderRect = new Rect(x + 8, y + 2, width - 16, 20);

            // 左侧标签 "强度"
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            Rect labelRect = new Rect(x + 8, y, 50, 20);
            GUI.Label(labelRect, $"  {Utils.I18n.Get("set_intensity")}: {Settings.BrightenIntensity:F1}", labelStyle);

            // 滑块本体（纯代码手绘，由 ClickGUIManager.DrawSlider 实现）
            Rect barRect = new Rect(x + 55, y + 5, width - 70, 14);
            Settings.BrightenIntensity = ClickGUIManager.DrawSlider(barRect, Settings.BrightenIntensity, 0f, 5f, e);

            y += 24f;
        }
    }

    public class Freecam : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_freecam");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()
        {
            Settings.IsFreecamEnabled = true;
            // 【修复】开启瞬间捕获当前摄像机坐标，防止视角飞到原点
            if (Camera.main != null)
                Settings.FreecamPosition = Camera.main.transform.position;
            else if (PlayerCamera.main != null)
                Settings.FreecamPosition = PlayerCamera.main.transform.position;
        }
        public override void OnDisable() => Settings.IsFreecamEnabled = false;
    }

    public class LongHands : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_longhands");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()  => Settings.IsLongHandsEnabled = true;
        public override void OnDisable() => Settings.IsLongHandsEnabled = false;

        public override float GetSettingsHeight() => 60f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                padding = new RectOffset(0, 0, 0, 0)
            };
            GUI.Label(new Rect(x + 8, y, 70, 20),
                $"  {Utils.I18n.Get("set_interact_distance") ?? "交互距离"}: {Settings.CustomPickupRange:F1}m", labelStyle);

            Rect barRect = new Rect(x + 78, y + 5, width - 90, 14);
            Settings.CustomPickupRange = ClickGUIManager.DrawSlider(barRect, Settings.CustomPickupRange, 3f, 30f, e);
            y += 30f;

            // TP 模式开关 (模仿 ItemESP.DrawToggle 样式)
            ItemESP.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_longhands_tp"),
                ref Settings.IsLongHandsTPModeEnabled);
        }
    }

    public class ThroughWall : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_throughwall");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()  => Settings.IsThroughWallEnabled = true;
        public override void OnDisable() => Settings.IsThroughWallEnabled = false;
    }

    public class Flight : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_flight");
        public override ModuleCategory Category => ModuleCategory.Movement;
        public override void OnEnable()  => Settings.IsFlightEnabled = true;
        public override void OnDisable() => Settings.IsFlightEnabled = false;
    }

    public class AutoUnlock : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_autounlock");
        public override ModuleCategory Category => ModuleCategory.World;
        public override void OnEnable()  => Settings.IsAutoUnlockEnabled = true;
        public override void OnDisable() => Settings.IsAutoUnlockEnabled = false;
    }

    public class AutoTranslate : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_autotranslate");
        public override ModuleCategory Category => ModuleCategory.Misc;
        public override void OnEnable()  => Settings.IsAutoTranslateEnabled = true;
        public override void OnDisable() => Settings.IsAutoTranslateEnabled = false;

        // 对应 Settings 中 TranslateLangCodes 的前端显示名称
        private static readonly string[] LangNames = { 
            "自动 (Auto)", "中文 (ZH)", "英文 (EN)", "俄文 (RU)", "日文 (JA)", 
            "韩文 (KO)", "西班牙文 (ES)", "法文 (FR)", "德文 (DE)" 
        };

        public override float GetSettingsHeight()
        {
            float h = 120f;
            if (Settings.CurrentTranslationEngine == FullBrightMod.Core.TranslationEngine.OpenAI)
                h += 130f; // BaseUrl, ApiKey, Model + 上下文开关
            return h;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // 完美白嫖音响模块的 UI 样式！
            GUIStyle labelStyle = BoomboxStyles.GeekLabelStyle;
            GUIStyle buttonStyle = BoomboxStyles.CustomButtonStyle;

            // =====================================
            // 第一行：源语言 (Source Language)
            // =====================================
            Rect sourceLabelRect = new Rect(x + 8, y, 70, 22);
            GUI.Label(sourceLabelRect, $"  {Utils.I18n.Get("set_trans_source") ?? "源语言"}", labelStyle);

            if (GUI.Button(new Rect(x + 78, y + 2, 22, 18), "‹", buttonStyle))
            {
                Settings.TranslateSourceIndex--;
                if (Settings.TranslateSourceIndex < 0) Settings.TranslateSourceIndex = LangNames.Length - 1;
            }

            GUIStyle centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            GUI.Label(new Rect(x + 100, y, width - 130, 22), LangNames[Settings.TranslateSourceIndex], centerStyle);

            if (GUI.Button(new Rect(x + width - 28, y + 2, 22, 18), "›", buttonStyle))
            {
                Settings.TranslateSourceIndex++;
                if (Settings.TranslateSourceIndex >= LangNames.Length) Settings.TranslateSourceIndex = 0;
            }
            y += 30f;

            // =====================================
            // 第二行：目标语言 (Target Language)
            // =====================================
            Rect targetLabelRect = new Rect(x + 8, y, 70, 22);
            GUI.Label(targetLabelRect, $"  {Utils.I18n.Get("set_trans_target") ?? "目标语言"}", labelStyle);

            if (GUI.Button(new Rect(x + 78, y + 2, 22, 18), "‹", buttonStyle))
            {
                Settings.TranslateTargetIndex--;
                // 目标语言不能是"自动(0)"，所以下限是 1
                if (Settings.TranslateTargetIndex < 1) Settings.TranslateTargetIndex = LangNames.Length - 1; 
            }

            GUI.Label(new Rect(x + 100, y, width - 130, 22), LangNames[Settings.TranslateTargetIndex], centerStyle);

            if (GUI.Button(new Rect(x + width - 28, y + 2, 22, 18), "›", buttonStyle))
            {
                Settings.TranslateTargetIndex++;
                // 跳过索引 0
                if (Settings.TranslateTargetIndex >= LangNames.Length) Settings.TranslateTargetIndex = 1; 
            }
            y += 30f;

            // =====================================
            // 引擎切换按钮
            // =====================================
            Rect engineRect = new Rect(x + 8, y + 4, width - 16, 22);
            bool isOpenAI = Settings.CurrentTranslationEngine == FullBrightMod.Core.TranslationEngine.OpenAI;
            Color engineColor = isOpenAI ? new Color(0.2f, 0.8f, 0.4f, 0.85f) : new Color(0.2f, 0.6f, 1.0f, 0.85f);
            GUI.color = engineColor;
            GUI.DrawTexture(engineRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;
            GUIStyle engineLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }
            };
            GUI.Label(engineRect, isOpenAI ? "Engine: OpenAI" : "Engine: Google Translate", engineLabelStyle);
            if (e.type == EventType.MouseDown && e.button == 0 && engineRect.Contains(e.mousePosition))
            {
                Settings.CurrentTranslationEngine = isOpenAI
                    ? FullBrightMod.Core.TranslationEngine.Google
                    : FullBrightMod.Core.TranslationEngine.OpenAI;
                e.Use();
            }
            y += 32f;

            // =====================================
            // OpenAI 配置输入
            // =====================================
            if (isOpenAI)
            {
                GUIStyle inputStyle = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 10, normal = { textColor = Color.white, background = ClickGUIManager.WhiteTexture },
                    padding = new RectOffset(4, 4, 2, 2)
                };
                GUIStyle fieldLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    padding = new RectOffset(14, 0, 2, 0)
                };

                GUI.Label(new Rect(x, y, width, 16f), "Base URL", fieldLabelStyle);
                Settings.OpenAIBaseUrl = GUI.TextField(
                    new Rect(x + 14, y + 16f, width - 28, 20f), Settings.OpenAIBaseUrl, inputStyle);
                y += 40f;

                GUI.Label(new Rect(x, y, width, 16f), "API Key", fieldLabelStyle);
                string key = GUI.PasswordField(
                    new Rect(x + 14, y + 16f, width - 28, 20f), Settings.OpenAIApiKey, '*', inputStyle);
                if (key != Settings.OpenAIApiKey) Settings.OpenAIApiKey = key;
                y += 40f;

                GUI.Label(new Rect(x, y, width, 16f), "Model", fieldLabelStyle);
                Settings.OpenAIModel = GUI.TextField(
                    new Rect(x + 14, y + 16f, width - 28, 20f), Settings.OpenAIModel, inputStyle);
                y += 38f;
            }

            // =====================================
            // 第三行：双向外发翻译开关
            // =====================================
            GUIStyle toggleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                padding = new RectOffset(14, 0, 4, 0)
            };
            Rect toggleRect = new Rect(x, y, width, 24f);

            GUI.color = Settings.IsTwoWayTranslationEnabled ? new Color(0.2f, 0.6f, 1.0f, 0.85f)
                : (toggleRect.Contains(e.mousePosition) ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
            GUI.DrawTexture(toggleRect, ClickGUIManager.WhiteTexture);
            GUI.color = Color.white;

            string display = (Settings.IsTwoWayTranslationEnabled ? "[ON]  " : "[OFF] ") + Utils.I18n.Get("set_two_way_trans");
            GUI.Label(toggleRect, display, toggleStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && toggleRect.Contains(e.mousePosition))
            {
                Settings.IsTwoWayTranslationEnabled = !Settings.IsTwoWayTranslationEnabled;
                e.Use();
            }
            
            y += 30f;

            // AI Context Memory toggle (仅 OpenAI 模式)
            if (Settings.CurrentTranslationEngine == FullBrightMod.Core.TranslationEngine.OpenAI)
            {
                Rect contextRect = new Rect(x, y, width, 24f);
                GUI.color = Settings.IsTranslationContextEnabled ? new Color(0.2f, 0.6f, 1.0f, 0.85f)
                    : (contextRect.Contains(e.mousePosition) ? new Color(0.22f, 0.22f, 0.25f, 0.90f) : new Color(0.16f, 0.16f, 0.18f, 0.90f));
                GUI.DrawTexture(contextRect, ClickGUIManager.WhiteTexture);
                GUI.color = Color.white;
                GUI.Label(contextRect, (Settings.IsTranslationContextEnabled ? "[ON]  " : "[OFF] ") + "AI Context Memory", toggleStyle);
                if (e.type == EventType.MouseDown && e.button == 0 && contextRect.Contains(e.mousePosition))
                {
                    Settings.IsTranslationContextEnabled = !Settings.IsTranslationContextEnabled;
                    e.Use();
                }
                y += 30f;
            }
        }
    }

    public class IQ250 : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_iq250");
        public override ModuleCategory Category => ModuleCategory.Misc;
        public override void OnEnable()  => Settings.IsIQ250Enabled = true;
        public override void OnDisable() => Settings.IsIQ250Enabled = false;
    }

    // ============================================================
    //  玩家透视 (Player ESP)
    // ============================================================
    public class PlayerESP : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_playeresp");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => Settings.IsPlayerEspEnabled = true;
        public override void OnDisable() => Settings.IsPlayerEspEnabled = false;

        public override float GetSettingsHeight() => 60f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            ItemESP.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_esp_wireframe"),
                ref Settings.IsPlayerEspWireframeEnabled);

            ItemESP.DrawColorPicker(x, ref y, width, e,
                Utils.I18n.Get("set_player_color") + ":",
                ref Settings.SelectedPlayerColor);
        }
    }

    // ============================================================
    //  相机传送 (Camera Teleport) — 关模块时传送到相机位置
    // ============================================================
    public class CameraTeleport : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_camerateleport");
        public override ModuleCategory Category => ModuleCategory.Player;

        public override void OnEnable()  => Settings.IsCameraTeleportEnabled = true;
        public override void OnDisable()
        {
            Settings.IsCameraTeleportEnabled = false;

            Body myBody = PlayerCamera.main?.body;
            Camera cam = Camera.main;
            if (myBody == null || cam == null) return;

            try
            {
                Vector3 safePos = Patches.TPHelper.GetSafeTeleportPosition(
                    myBody.transform.position, cam.transform.position, 0.5f);
                myBody.transform.position = safePos;
                ClientMain.Client_SendCharacterSyncPacket();
            }
            catch { }
        }
    }
}
