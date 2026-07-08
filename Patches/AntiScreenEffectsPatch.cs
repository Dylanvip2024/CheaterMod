using HarmonyLib;
using FullBrightMod.Core;
using KrokoshaCasualtiesMP;
using UnityEngine;
using UnityEngine.UI;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// 反屏幕特效 Harmony Patch — 在 PlayerCamera.HandleScreenShaders() 执行后，
    /// 根据用户选择独立清零对应类别的 Shader 参数或禁用 UI 覆盖层。
    ///
    /// 8 个独立开关：
    /// 1. DisableBloodFx      — 疼痛红屏 (_Pain) / 失血褪色 (_BloodAmount, _Bloodloss)
    /// 2. DisableTemperatureFx — 寒冷冰冻 (_FrostAmount) / 过热灼烧 (_OverheatAmount)
    /// 3. DisableMentalFx      — 疯狂扭曲 (_InsanityFX) / 抑郁呕吐模糊 (_BadFocusAmount) / 药物幻视+屏幕翻转 (bonusPass)
    /// 4. DisableBlurFx        — 通用模糊 (_BlurIntensity) / 饥饿模糊 (_HungerIntensity) / 疾病扭曲 (_SicknessAmount) / 水滴 (_DropletAmount)
    /// 5. DisableRadiationFx   — 辐射噪点 (_RadAmount)
    /// 6. DisableDamageFx      — 眼部损伤 (_EyeDamage) / 脑损伤 (_BrainDamage) / 污垢 (_Dirtyness)
    /// 7. DisableUIOverlays    — 昏迷黑屏 / 死亡画面 / 脑损闪光 / 盲眼光 / 背景雪花 / 后处理 Volume
    /// 8. DisableFogFx         — 大雾/雨天雾效 (fogSprite alpha + fogAmount + _Consciousness)
    /// </summary>
    [HarmonyPatch(typeof(PlayerCamera), "HandleScreenShaders")]
    internal static class AntiScreenEffectsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(PlayerCamera __instance)
        {
            // 快速路径：模块未启用 或 所有开关都关时不执行任何操作
            if (!ModSettings.IsAntiScreenEffectsActive) return;
            if (!ModSettings.DisableBloodFx && !ModSettings.DisableTemperatureFx &&
                !ModSettings.DisableMentalFx && !ModSettings.DisableBlurFx &&
                !ModSettings.DisableRadiationFx && !ModSettings.DisableDamageFx &&
                !ModSettings.DisableUIOverlays && !ModSettings.DisableFogFx)
                return;

            // ==== 1. 流血/疼痛红屏 ====
            if (ModSettings.DisableBloodFx)
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
            if (ModSettings.DisableTemperatureFx && __instance.mainScreenPass != null)
            {
                __instance.mainScreenPass.SetFloat("_FrostAmount", 0f);
                __instance.mainScreenPass.SetFloat("_OverheatAmount", 0f);
            }

            // ==== 3. 精神异常 (疯狂/抑郁/药物幻视/屏幕翻转) ====
            if (ModSettings.DisableMentalFx)
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
            if (ModSettings.DisableBlurFx && __instance.blurPass != null)
            {
                __instance.blurPass.SetFloat("_BlurIntensity", 0f);
                __instance.blurPass.SetFloat("_HungerIntensity", 0f);
                __instance.blurPass.SetFloat("_SicknessAmount", 0f);
                __instance.blurPass.SetFloat("_DropletAmount", 0f);
            }

            // ==== 5. 辐射噪点 ====
            if (ModSettings.DisableRadiationFx && __instance.damagePass != null)
                __instance.damagePass.SetFloat("_RadAmount", 0f);

            // ==== 6. 损伤视觉遮罩 (眼伤/脑伤/污垢) ====
            if (ModSettings.DisableDamageFx)
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
            if (ModSettings.DisableUIOverlays)
            {
                if (__instance.consciousnessFade != null)
                    __instance.consciousnessFade.gameObject.SetActive(false);
                if (__instance.consciousnessVignette != null)
                    __instance.consciousnessVignette.gameObject.SetActive(false);

                // ★ 7.0.1 兼容：deathScreen 已更名为 endScreen (Image)
                //     使用 Traverse 安全反射获取，新旧字段名均兼容
                var deathScreen = Traverse.Create(__instance).Field("endScreen").GetValue<Image>()
                               ?? Traverse.Create(__instance).Field("deathScreen").GetValue<Image>();
                if (deathScreen != null)
                    deathScreen.gameObject.SetActive(false);

                if (__instance.brainFlash != null)
                    __instance.brainFlash.enabled = false;

                // ★ 7.0.1 兼容：blindLight 字段已被完全移除
                //     使用 Traverse 安全反射获取，不存在时静默跳过
                var blindLight = Traverse.Create(__instance).Field("blindLight").GetValue<GameObject>();
                if (blindLight != null)
                    blindLight.SetActive(false);

                if (__instance.backgroundSnow != null)
                    __instance.backgroundSnow.SetActive(false);
                if (__instance.volume != null)
                    __instance.volume.weight = 0f;
            }

            // ==== 8. 大雾/雨天雾效 ====
            if (ModSettings.DisableFogFx)
            {
                if (WorldGeneration.world != null && WorldGeneration.world.fogAmount > 0.01f)
                {
                    WorldGeneration.world.fogAmount = 0f;
                    if (WorldGeneration.world.fogSprite != null)
                        WorldGeneration.world.fogSprite.color = new Color(0.8f, 0.8f, 0.8f, 0f);
                }
                if (__instance.mainScreenPass != null)
                    __instance.mainScreenPass.SetFloat("_Consciousness", 1f);
            }
        }
    }

    // =========================================================
    //  AntiConfusionControlPatch — 物理硬件级输入覆写
    //  7.0.1: reversedControls 仍然在 Body.Update() 中反转 moveDir
    //  此 Patch 在 Body.Update 执行完毕后，直接读取物理键盘输入覆写
    // =========================================================
    [HarmonyPatch(typeof(Body), "Update")]
    internal static class AntiConfusionControlPatch
    {
        [HarmonyPostfix]
        private static void OnBodyUpdate_Postfix(Body __instance)
        {
            if (!ModSettings.IsAntiScreenEffectsActive || !ModSettings.DisableMentalFx) return;
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;
            if (!__instance.conscious) return;

            float rawX = 0f, rawY = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyBinds.GetBind("left")))  rawX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyBinds.GetBind("right"))) rawX += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyBinds.GetBind("down")))  rawY -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyBinds.GetBind("up")))    rawY += 1f;

            Vector2 correctMove = new Vector2(rawX, rawY);
            if (correctMove.sqrMagnitude > 1f) correctMove.Normalize();

            Traverse.Create(__instance).Field("moveDir").SetValue(correctMove);
        }
    }
}
