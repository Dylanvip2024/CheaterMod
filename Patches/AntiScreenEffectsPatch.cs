using HarmonyLib;
using FullBrightMod.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// 反屏幕特效 Harmony Patch — 在 PlayerCamera.HandleScreenShaders() 执行后，
    /// 根据用户选择独立清零对应类别的 Shader 参数或禁用 UI 覆盖层。
    ///
    /// 7 个独立开关：
    /// 1. DisableBloodFx      — 疼痛红屏 (_Pain) / 失血褪色 (_BloodAmount, _Bloodloss)
    /// 2. DisableTemperatureFx — 寒冷冰冻 (_FrostAmount) / 过热灼烧 (_OverheatAmount)
    /// 3. DisableMentalFx      — 疯狂扭曲 (_InsanityFX) / 抑郁呕吐模糊 (_BadFocusAmount) / 药物幻视+屏幕翻转 (bonusPass)
    /// 4. DisableBlurFx        — 通用模糊 (_BlurIntensity) / 饥饿模糊 (_HungerIntensity) / 疾病扭曲 (_SicknessAmount) / 水滴 (_DropletAmount)
    /// 5. DisableRadiationFx   — 辐射噪点 (_RadAmount)
    /// 6. DisableDamageFx      — 眼部损伤 (_EyeDamage) / 脑损伤 (_BrainDamage) / 污垢 (_Dirtyness)
    /// 7. DisableUIOverlays    — 昏迷黑屏 / 死亡画面 / 脑损闪光 / 盲眼光 / 背景雪花 / 后处理 Volume
    /// </summary>
    [HarmonyPatch(typeof(PlayerCamera), "HandleScreenShaders")]
    internal static class AntiScreenEffectsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerCamera __instance)
        {
            // 快速路径：模块未启用 或 所有开关都关时不执行任何操作
            if (!Settings.IsAntiScreenEffectsActive) return;
            if (!Settings.DisableBloodFx && !Settings.DisableTemperatureFx &&
                !Settings.DisableMentalFx && !Settings.DisableBlurFx &&
                !Settings.DisableRadiationFx && !Settings.DisableDamageFx &&
                !Settings.DisableUIOverlays)
                return;

            // ==== 1. 流血/疼痛红屏 ====
            if (Settings.DisableBloodFx)
            {
                if (__instance.mainScreenPass != null)
                {
                    __instance.mainScreenPass.SetFloat("_Pain", 0f);
                    __instance.mainScreenPass.SetFloat("_BloodAmount", 1f);
                }
                if (__instance.damagePass != null)
                    __instance.damagePass.SetFloat("_Bloodloss", 0f);
            }

            // ==== 2. 温度异常 ====
            if (Settings.DisableTemperatureFx && __instance.mainScreenPass != null)
            {
                __instance.mainScreenPass.SetFloat("_FrostAmount", 0f);
                __instance.mainScreenPass.SetFloat("_OverheatAmount", 0f);
            }

            // ==== 3. 精神异常 (疯狂/抑郁/药物幻视/屏幕翻转) ====
            if (Settings.DisableMentalFx)
            {
                if (__instance.mainScreenPass != null)
                {
                    __instance.mainScreenPass.SetFloat("_InsanityFX", 0f);
                    __instance.mainScreenPass.SetFloat("_BadFocusAmount", 0f);
                }
                if (__instance.blurPass != null)
                    __instance.blurPass.SetFloat("_Insanity", 0f);
                if (__instance.bonusPass != null)
                {
                    __instance.bonusPass.SetFloat("_Intensity", 0f);
                    __instance.bonusPass.SetFloat("_Distort", 0f);
                    __instance.bonusPass.SetFloat("_FlipScreen", 0f);
                }
            }

            // ==== 4. 模糊/饥饿/疾病/水滴 ====
            if (Settings.DisableBlurFx && __instance.blurPass != null)
            {
                __instance.blurPass.SetFloat("_BlurIntensity", 0f);
                __instance.blurPass.SetFloat("_HungerIntensity", 0f);
                __instance.blurPass.SetFloat("_SicknessAmount", 0f);
                __instance.blurPass.SetFloat("_DropletAmount", 0f);
            }

            // ==== 5. 辐射噪点 ====
            if (Settings.DisableRadiationFx && __instance.damagePass != null)
                __instance.damagePass.SetFloat("_RadAmount", 0f);

            // ==== 6. 损伤视觉遮罩 (眼伤/脑伤/污垢) ====
            if (Settings.DisableDamageFx)
            {
                if (__instance.damagePass != null)
                {
                    __instance.damagePass.SetFloat("_EyeDamage", 0f);
                    __instance.damagePass.SetFloat("_BrainDamage", 0f);
                }
                if (__instance.mainScreenPass != null)
                    __instance.mainScreenPass.SetFloat("_Dirtyness", 0f);
            }

            // ==== 7. UI 覆盖层 ====
            if (Settings.DisableUIOverlays)
            {
                if (__instance.consciousnessFade != null)
                    __instance.consciousnessFade.gameObject.SetActive(false);
                if (__instance.consciousnessVignette != null)
                    __instance.consciousnessVignette.gameObject.SetActive(false);
                if (__instance.deathScreen != null)
                    __instance.deathScreen.gameObject.SetActive(false);
                if (__instance.brainFlash != null)
                    __instance.brainFlash.enabled = false;
                if (__instance.blindLight != null)
                    __instance.blindLight.SetActive(false);
                if (__instance.backgroundSnow != null)
                    __instance.backgroundSnow.SetActive(false);
                if (__instance.volume != null)
                    __instance.volume.weight = 0f;
            }
        }
    }
}
