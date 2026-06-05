using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class Chameleon : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_chameleon");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => ModSettings.IsChameleonEnabled = true;
        public override void OnDisable() => ModSettings.IsChameleonEnabled = false;

        private static readonly string[] PartNames = {
            "cham_part_head", "cham_part_eyes", "cham_part_torso",
            "cham_part_arms", "cham_part_legs", "cham_part_tail"
        };

        private static readonly string[] DyeKeys = {
            "cham_dye_head", "cham_dye_eyes", "cham_dye_torso",
            "cham_dye_arms", "cham_dye_legs", "cham_dye_tail"
        };

        public override float GetSettingsHeight()
        {
            float h = 0f;
            h += 28f; // 彩虹 Toggle
            if (ModSettings.IsChameleonRainbowModeEnabled)
                h += 28f; // 彩虹速度
            h += 28f; // 发光 Toggle
            if (ModSettings.IsChameleonGlowEnabled)
                h += 28f; // 发光强度
            if (!ModSettings.IsChameleonRainbowModeEnabled)
            {
                h += 30f; // 部位 Selector
                h += 56f; // Toggle + ColorPicker
            }
            return h;
        }

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            // ── 彩虹模式 ──
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_chameleon_rainbow"),
                ref ModSettings.IsChameleonRainbowModeEnabled);

            if (ModSettings.IsChameleonRainbowModeEnabled)
            {
                float speed = ModSettings.ChameleonRainbowSpeed;
                CustomGUI.DrawSlider(x, ref y, width, e,
                    Utils.I18n.Get("set_rainbow_speed") + $": {speed:F1}",
                    ref speed, 0.1f, 3f);
                ModSettings.ChameleonRainbowSpeed = speed;
            }

            // ── 发光 ──
            CustomGUI.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_chameleon_glow"),
                ref ModSettings.IsChameleonGlowEnabled);

            if (ModSettings.IsChameleonGlowEnabled)
            {
                float intensity = ModSettings.ChameleonIntensity;
                CustomGUI.DrawSlider(x, ref y, width, e,
                    Utils.I18n.Get("set_chameleon_intensity") + $": {intensity:F1}",
                    ref intensity, 1f, 5f);
                ModSettings.ChameleonIntensity = intensity;
            }

            // ── 部位编辑（非彩虹模式）─
            if (!ModSettings.IsChameleonRainbowModeEnabled)
            {
                int idx = ModSettings.ChameleonPartIndex;
                var partDisplay = new string[6];
                for (int i = 0; i < 6; i++) partDisplay[i] = Utils.I18n.Get(PartNames[i]);
                CustomGUI.DrawSelector(x, ref y, width, e,
                    Utils.I18n.Get("set_chameleon_color"), partDisplay, ref idx);
                ModSettings.ChameleonPartIndex = idx;

                switch (idx)
                {
                    case 0: // 头部
                        CustomGUI.DrawToggle(x, ref y, width, e,
                            Utils.I18n.Get(DyeKeys[0]), ref ModSettings.IsHeadColorEnabled);
                        if (ModSettings.IsHeadColorEnabled)
                            CustomGUI.DrawColorPicker(x, ref y, width, e,
                                "", ref ModSettings.HeadColor);
                        break;
                    case 1: // 眼睛
                        CustomGUI.DrawToggle(x, ref y, width, e,
                            Utils.I18n.Get(DyeKeys[1]), ref ModSettings.IsEyesColorEnabled);
                        if (ModSettings.IsEyesColorEnabled)
                            CustomGUI.DrawColorPicker(x, ref y, width, e,
                                "", ref ModSettings.EyesColor);
                        break;
                    case 2: // 躯干
                        CustomGUI.DrawToggle(x, ref y, width, e,
                            Utils.I18n.Get(DyeKeys[2]), ref ModSettings.IsTorsoColorEnabled);
                        if (ModSettings.IsTorsoColorEnabled)
                            CustomGUI.DrawColorPicker(x, ref y, width, e,
                                "", ref ModSettings.TorsoColor);
                        break;
                    case 3: // 手臂
                        CustomGUI.DrawToggle(x, ref y, width, e,
                            Utils.I18n.Get(DyeKeys[3]), ref ModSettings.IsArmsColorEnabled);
                        if (ModSettings.IsArmsColorEnabled)
                            CustomGUI.DrawColorPicker(x, ref y, width, e,
                                "", ref ModSettings.ArmsColor);
                        break;
                    case 4: // 腿部
                        CustomGUI.DrawToggle(x, ref y, width, e,
                            Utils.I18n.Get(DyeKeys[4]), ref ModSettings.IsLegsColorEnabled);
                        if (ModSettings.IsLegsColorEnabled)
                            CustomGUI.DrawColorPicker(x, ref y, width, e,
                                "", ref ModSettings.LegsColor);
                        break;
                    case 5: // 尾巴
                        CustomGUI.DrawToggle(x, ref y, width, e,
                            Utils.I18n.Get(DyeKeys[5]), ref ModSettings.IsTailColorEnabled);
                        if (ModSettings.IsTailColorEnabled)
                            CustomGUI.DrawColorPicker(x, ref y, width, e,
                                "", ref ModSettings.TailColor);
                        break;
                }
            }
        }
    }
}
