using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    // ============================================================
    //  原子功能模块 —— 每个模块仅负责一个功能的开关切换
    //  已单独提取到独立文件的模块：ItemESP, CreatureESP, TrapESP,
    //    VisionExpand, CameraZoom, FullBright
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
    }

    public class IQ250 : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_iq250");
        public override ModuleCategory Category => ModuleCategory.Misc;
        public override void OnEnable()  => Settings.IsIQ250Enabled = true;
        public override void OnDisable() => Settings.IsIQ250Enabled = false;
    }
}
