using FullBrightMod.Core;
using HarmonyLib;
using UnityEngine;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// ChameleonPatch - 地毯式 SpriteRenderer 染色
    /// 支持空间流光彩虹 + 6 部位独立色 + HDR 发光
    /// </summary>
    [HarmonyPatch(typeof(Body), "Update")]
    internal static class ChameleonPatch
    {
        [HarmonyPostfix]
        private static void OnBodyUpdate_Postfix(Body __instance)
        {
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;

            var allRenderers = __instance.GetComponentsInChildren<SpriteRenderer>();
            if (allRenderers == null || allRenderers.Length == 0) return;

            if (ModSettings.IsChameleonEnabled)
                ApplyColor(allRenderers);
            else
                ResetColor(allRenderers);
        }

        private static void ApplyColor(SpriteRenderer[] renderers)
        {
            float intensity = ModSettings.IsChameleonGlowEnabled ? ModSettings.ChameleonIntensity : 1f;
            bool rainbow = ModSettings.IsChameleonRainbowModeEnabled;
            float rainbowSpeed = ModSettings.ChameleonRainbowSpeed;

            foreach (var sr in renderers)
            {
                if (sr == null || !sr) continue;

                try
                {
                    Color final;

                    if (rainbow)
                    {
                        // 空间流光彩虹：色调 = 时间 + Y 轴坐标偏移
                        float hue = Mathf.Repeat(Time.time * rainbowSpeed + sr.transform.position.y * 0.5f, 1f);
                        final = Color.HSVToRGB(hue, 1f, 1f);
                    }
                    else
                    {
                        // 部位独立色（关闭的部位用 white 恢复原色）
                        final = GetPartColor(sr);
                    }

                    final.r *= intensity;
                    final.g *= intensity;
                    final.b *= intensity;
                    sr.color = final;
                }
                catch { }
            }
        }

        private static void ResetColor(SpriteRenderer[] renderers)
        {
            foreach (var sr in renderers)
            {
                if (sr == null || !sr) continue;
                try { sr.color = Color.white; } catch { }
            }
        }

        /// <summary>根据 SpriteRenderer 所在 GameObject 名称判定部位，返回对应颜色</summary>
        private static Color GetPartColor(SpriteRenderer sr)
        {
            string name = sr.gameObject.name.ToLowerInvariant();

            // 眼睛 (eye/pupil)
            if (name.Contains("eye") || name.Contains("pupil"))
                return ModSettings.IsEyesColorEnabled ? ModSettings.EyesColor : Color.white;

            // 头部
            if (name.Contains("head"))
                return ModSettings.IsHeadColorEnabled ? ModSettings.HeadColor : Color.white;

            // 尾巴 (必须在 torso/body 之前，避免 tail 被误匹配)
            if (name.Contains("tail"))
                return ModSettings.IsTailColorEnabled ? ModSettings.TailColor : Color.white;

            // 手臂 (arm/hand)
            if (name.Contains("arm") || name.Contains("hand"))
                return ModSettings.IsArmsColorEnabled ? ModSettings.ArmsColor : Color.white;

            // 腿部 (leg/foot)
            if (name.Contains("leg") || name.Contains("foot"))
                return ModSettings.IsLegsColorEnabled ? ModSettings.LegsColor : Color.white;

            // 躯干 (torso/body) — 兜底
            if (name.Contains("torso") || name.Contains("body"))
                return ModSettings.IsTorsoColorEnabled ? ModSettings.TorsoColor : Color.white;

            // 无法识别 — 保持原色
            return Color.white;
        }
    }
}
