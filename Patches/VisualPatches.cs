using FullBrightMod.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FullBrightMod.Patches
{
    // =========================================================
    // Light2D.LateUpdate — 不闪烁光照控制
    // =========================================================
    [HarmonyPatch(typeof(Light2D), "LateUpdate")]
    internal static class Light2DLateUpdatePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Light2D __instance)
        {
            if (__instance == null) return;

            if (Settings.IsFullBrightEnabled)
            {
                if (__instance.lightType == Light2D.LightType.Global)
                {
                    __instance.intensity = Settings.BrightenIntensity;
                    __instance.color = Color.white;
                }
                if (__instance.shadowsEnabled)
                {
                    __instance.shadowsEnabled = false;
                    __instance.shadowIntensity = 0f;
                }
            }
            else if (Settings.IsVisionExpandEnabled)
            {
                if (__instance.lightType == Light2D.LightType.Point || __instance.lightType == Light2D.LightType.Freeform)
                {
                    __instance.pointLightOuterRadius = Settings.CustomVisionRadius;
                    __instance.pointLightInnerRadius = Settings.CustomVisionRadius * 0.1f;
                }
                if (__instance.shadowsEnabled)
                {
                    __instance.shadowsEnabled = false;
                    __instance.shadowIntensity = 0f;
                }
            }
        }
    }

    // =========================================================
    // Camera.orthographicSize — 视距拉远
    // =========================================================
    [HarmonyPatch(typeof(Camera), "orthographicSize", MethodType.Setter)]
    internal static class CameraSizePatch
    {
        private static void Prefix(Camera __instance, ref float value)
        {
            if (Settings.IsCameraZoomEnabled && __instance.CompareTag("MainCamera"))
            {
                value = Settings.CustomCameraSize;
            }
        }
    }

    // =========================================================
    // Volume.Update — 后处理截杀
    // =========================================================
    [HarmonyPatch(typeof(UnityEngine.Rendering.Volume), "Update")]
    internal static class VolumePatch
    {
        [HarmonyPostfix]
        private static void Postfix(UnityEngine.Rendering.Volume __instance)
        {
            if (__instance == null) return;
            if (Settings.IsFullBrightEnabled && __instance.enabled)
            {
                __instance.enabled = false;
            }
        }
    }

    // =========================================================
    // VisionMask.UpdateMask — 黑幕截杀
    // =========================================================
    [HarmonyPatch(typeof(VisionMask), "UpdateMask")]
    internal static class VisionMaskPatch
    {
        [HarmonyPrefix]
        private static void Prefix(VisionMask __instance)
        {
            if (__instance == null) return;

            if (Settings.IsFullBrightEnabled)
            {
                __instance.ClearMask();
            }
            else if (Settings.IsVisionExpandEnabled)
            {
                float activeRadius = Mathf.Max(Settings.CustomVisionRadius, Settings.CustomCameraSize * 1.8f);
                __instance.farDistance = activeRadius * 2.5f;
            }
        }
    }
}
