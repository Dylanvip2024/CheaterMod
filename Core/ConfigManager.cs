using System;
using System.Collections.Generic;
using System.IO;
using FullBrightMod.Modules;
using FullBrightMod.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace FullBrightMod.Core
{
    /// <summary>
    /// 配置持久化管理器 —— 基于 JSON 的保存/加载系统。
    /// 将 Settings、模块状态、面板布局以及 GlobalSettings 序列化到 BepInEx/config/CheaterMod.json。
    /// </summary>
    public static class ConfigManager
    {
        private static string ConfigPath => Path.Combine(BepInEx.Paths.ConfigPath, "CheaterMod.json");

        [Serializable]
        public class SaveData
        {
            // ---- Settings 副本 ----
            public bool IsFullBrightEnabled;
            public bool IsVisionExpandEnabled;
            public bool IsItemEspEnabled;
            public bool IsCreatureEspEnabled;
            public bool IsTrapEspEnabled;
            public bool IsCameraZoomEnabled;
            public bool IsIQ250Enabled;
            public bool IsLongHandsEnabled;
            public bool IsThroughWallEnabled;
            public bool IsAutoTranslateEnabled;
            public bool IsAutoUnlockEnabled;
            public bool IsFreecamEnabled;
            public bool IsFlightEnabled;
            public bool IsJumpBoostEnabled;
            public bool IsNoClipEnabled;
            public bool IsAutoBandageEnabled;
            public bool IsAntiRagdollEnabled;
            public bool IsShrapnelMakerEnabled;
            public bool IsInstantAmputationEnabled;
            public bool IsExplosivesMacroEnabled;
            public bool IsFetchMacroEnabled;
            public bool IsInstantShrapnelRemovalEnabled;
            public bool IsBoomboxEnabled;
            public bool StopMusicWhenTalking;
            public bool IsInfiniteAmmoEnabled;
            public bool IsNoSpreadEnabled;
            public bool IsRapidFireEnabled;
            public bool IsNoJamEnabled;
            public bool IsNoRecoilEnabled;
            public bool IsAutoReloadEnabled;
            public bool IsAutoBoltEnabled;
            public bool IsMouseAimbotEnabled;

            public float BrightenIntensity;
            public float CustomVisionRadius;
            public float CustomCameraSize;
            public int EspFontSize;
            public float CustomPickupRange;
            public float CustomJumpForce;
            public float CustomFireRateMultiplier;
            public float FetchDistance;
            public float AimbotRadius;
            public int CurrentTrackIndex;

            public SColor SelectedEspColor;
            public SColor SelectedCreatureColor;
            public SColor SelectedTrapColor;
            public SColor TrajectoryColor;

            public float FreecamX, FreecamY, FreecamZ;

            // ---- 语言 ----
            public int Language;

            // ---- 模块状态 ----
            public List<ModuleEntry> Modules = new List<ModuleEntry>();

            // ---- 面板布局 ----
            public List<PanelEntry> Panels = new List<PanelEntry>();

            // ---- 聊天翻译 ----
            public int TranslateSourceIndex;
            public int TranslateTargetIndex;
            public bool IsTwoWayTranslationEnabled;

            // ---- 投掷轨迹 ----
            public bool IsTrajectoryEnabled;
            public float TrajectoryMaxLength;

            // ---- 速度修改 / 反负重 ----
            public bool IsSpeedModifierEnabled;
            public float CustomSpeedMultiplier;
            public bool IsAntiWeightEnabled;
            public bool IsAirJumpEnabled;
            public bool IsJetpackEnabled;

            // ---- KillAura ----
            public bool IsKillAuraEnabled;
            public bool KillAuraAttackPlayers;
            public float KillAuraAPS;
            public float KillAuraRange;
            public bool KillAuraRenderTarget;
            public SColor KillAuraTargetColor;

            // ============================================================
            // ★ GlobalSettings 持久化字段 ★
            // ============================================================

            // Grid Snapping
            public bool EnableGridSnap;
            public float GridSize;

            // Minimap
            public bool EnableMinimap;
            public float MinimapRectX, MinimapRectY, MinimapRectW, MinimapRectH;
            public float MinimapRadius;

            // Limb HUD
            public bool EnableLimbHUD;
            public float LimbHUDPosX, LimbHUDPosY;
            public float LimbHUDScale;

            // Logo Overlay
            public bool EnableLogoOverlay;
            public float LogoOverlayHeight;
        }

        [Serializable]
        public class SColor { public float r, g, b, a; }

        [Serializable]
        public class ModuleEntry
        {
            public string ClassName;
            public bool Enabled;
            public bool IsExpanded;
            public int BindKey;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Name;
        }

        [Serializable]
        public class PanelEntry
        {
            public int Category;
            public float X, Y;
            public bool IsExpanded;
        }

        // ============================================================
        // 保存
        // ============================================================
        public static void Save(ModuleManager modManager, ClickGUIManager guiManager)
        {
            try
            {
                SaveData data = new SaveData();

                // --- Settings ---
                data.IsFullBrightEnabled   = Settings.IsFullBrightEnabled;
                data.IsVisionExpandEnabled = Settings.IsVisionExpandEnabled;
                data.IsItemEspEnabled      = Settings.IsItemEspEnabled;
                data.IsCreatureEspEnabled  = Settings.IsCreatureEspEnabled;
                data.IsTrapEspEnabled      = Settings.IsTrapEspEnabled;
                data.IsCameraZoomEnabled   = Settings.IsCameraZoomEnabled;
                data.IsIQ250Enabled        = Settings.IsIQ250Enabled;
                data.IsLongHandsEnabled    = Settings.IsLongHandsEnabled;
                data.IsThroughWallEnabled  = Settings.IsThroughWallEnabled;
                data.IsAutoTranslateEnabled= Settings.IsAutoTranslateEnabled;
                data.IsAutoUnlockEnabled   = Settings.IsAutoUnlockEnabled;
                data.IsFreecamEnabled      = Settings.IsFreecamEnabled;
                data.IsFlightEnabled       = Settings.IsFlightEnabled;
                data.IsJumpBoostEnabled    = Settings.IsJumpBoostEnabled;
                data.IsNoClipEnabled              = Settings.IsNoClipEnabled;
                data.IsAutoBandageEnabled         = Settings.IsAutoBandageEnabled;
                data.IsAntiRagdollEnabled         = Settings.IsAntiRagdollEnabled;
                data.IsShrapnelMakerEnabled       = Settings.IsShrapnelMakerEnabled;
                data.IsInstantAmputationEnabled   = Settings.IsInstantAmputationEnabled;
                data.IsExplosivesMacroEnabled     = Settings.IsExplosivesMacroEnabled;
                data.IsFetchMacroEnabled           = Settings.IsFetchMacroEnabled;
                data.IsInstantShrapnelRemovalEnabled = Settings.IsInstantShrapnelRemovalEnabled;
                data.IsBoomboxEnabled             = Settings.IsBoomboxEnabled;
                data.StopMusicWhenTalking         = Settings.StopMusicWhenTalking;
                data.IsInfiniteAmmoEnabled        = Settings.IsInfiniteAmmoEnabled;
                data.IsNoSpreadEnabled            = Settings.IsNoSpreadEnabled;
                data.IsRapidFireEnabled           = Settings.IsRapidFireEnabled;
                data.IsNoJamEnabled               = Settings.IsNoJamEnabled;
                data.IsNoRecoilEnabled            = Settings.IsNoRecoilEnabled;
                data.IsAutoReloadEnabled          = Settings.IsAutoReloadEnabled;
                data.IsAutoBoltEnabled            = Settings.IsAutoBoltEnabled;
                data.IsMouseAimbotEnabled         = Settings.IsMouseAimbotEnabled;
                data.IsTrajectoryEnabled          = Settings.IsTrajectoryEnabled;
                data.TrajectoryMaxLength          = Settings.TrajectoryMaxLength;
                data.IsSpeedModifierEnabled       = Settings.IsSpeedModifierEnabled;
                data.CustomSpeedMultiplier        = Settings.CustomSpeedMultiplier;
                data.IsAntiWeightEnabled          = Settings.IsAntiWeightEnabled;
                data.IsAirJumpEnabled             = Settings.IsAirJumpEnabled;
                data.IsJetpackEnabled             = Settings.IsJetpackEnabled;

                data.IsKillAuraEnabled          = Settings.IsKillAuraEnabled;
                data.KillAuraAttackPlayers      = Settings.KillAuraAttackPlayers;
                data.KillAuraAPS                = Settings.KillAuraAPS;
                data.KillAuraRange              = Settings.KillAuraRange;
                data.KillAuraRenderTarget       = Settings.KillAuraRenderTarget;
                data.KillAuraTargetColor        = ToSColor(Settings.KillAuraTargetColor);
                data.CustomFireRateMultiplier     = Settings.CustomFireRateMultiplier;

                data.BrightenIntensity  = Settings.BrightenIntensity;
                data.CustomVisionRadius = Settings.CustomVisionRadius;
                data.CustomCameraSize   = Settings.CustomCameraSize;
                data.EspFontSize        = Settings.EspFontSize;
                data.CustomPickupRange  = Settings.CustomPickupRange;
                data.CustomJumpForce    = Settings.CustomJumpForce;
                data.FetchDistance      = Settings.FetchDistance;
                data.AimbotRadius       = Settings.AimbotRadius;
                data.CurrentTrackIndex  = Settings.CurrentTrackIndex;

                data.SelectedEspColor      = ToSColor(Settings.SelectedEspColor);
                data.SelectedCreatureColor = ToSColor(Settings.SelectedCreatureColor);
                data.SelectedTrapColor     = ToSColor(Settings.SelectedTrapColor);
                data.TrajectoryColor       = ToSColor(Settings.TrajectoryColor);

                data.FreecamX = Settings.FreecamPosition.x;
                data.FreecamY = Settings.FreecamPosition.y;
                data.FreecamZ = Settings.FreecamPosition.z;

                data.Language = (int)Settings.CurrentLanguage;

                data.TranslateSourceIndex = Settings.TranslateSourceIndex;
                data.TranslateTargetIndex = Settings.TranslateTargetIndex;
                data.IsTwoWayTranslationEnabled = Settings.IsTwoWayTranslationEnabled;

                // --- ModuleManager ---
                foreach (var mod in modManager.GetAllModules())
                {
                    data.Modules.Add(new ModuleEntry
                    {
                        ClassName = mod.GetType().Name,
                        Enabled = mod.Enabled,
                        IsExpanded = mod.IsExpanded,
                        BindKey = (int)mod.BindKey
                    });
                }

                // --- 面板布局 ---
                if (guiManager != null)
                {
                    foreach (var panel in guiManager.Panels)
                    {
                        data.Panels.Add(new PanelEntry
                        {
                            Category = (int)panel.Category,
                            X = panel.Position.x,
                            Y = panel.Position.y,
                            IsExpanded = panel.IsExpanded
                        });
                    }
                }

                // ============================================================
                // ★ GlobalSettings ★
                // ============================================================
                data.EnableGridSnap = GlobalSettings.EnableGridSnap;
                data.GridSize       = GlobalSettings.GridSize;

                data.EnableMinimap    = GlobalSettings.EnableMinimap;
                data.MinimapRectX     = GlobalSettings.MinimapRect.x;
                data.MinimapRectY     = GlobalSettings.MinimapRect.y;
                data.MinimapRectW     = GlobalSettings.MinimapRect.width;
                data.MinimapRectH     = GlobalSettings.MinimapRect.height;
                data.MinimapRadius    = GlobalSettings.MinimapRadius;

                data.EnableLimbHUD    = GlobalSettings.EnableLimbHUD;
                data.LimbHUDPosX      = GlobalSettings.LimbHUDPosition.x;
                data.LimbHUDPosY      = GlobalSettings.LimbHUDPosition.y;
                data.LimbHUDScale     = GlobalSettings.LimbHUDScale;

                data.EnableLogoOverlay    = GlobalSettings.EnableLogoOverlay;
                data.LogoOverlayHeight    = GlobalSettings.LogoOverlayHeight;

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheaterMod] 保存配置失败: {ex.Message}");
            }
        }

        // ============================================================
        // 加载
        // ============================================================
        public static void Load(ModuleManager modManager, ClickGUIManager guiManager)
        {
            if (!File.Exists(ConfigPath))
            {
                Debug.Log("[CheaterMod] 未找到配置文件，使用默认设置。");
                return;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
                if (data == null) return;

                // --- Settings ---
                Settings.IsFullBrightEnabled   = data.IsFullBrightEnabled;
                Settings.IsVisionExpandEnabled = data.IsVisionExpandEnabled;
                Settings.IsItemEspEnabled      = data.IsItemEspEnabled;
                Settings.IsCreatureEspEnabled  = data.IsCreatureEspEnabled;
                Settings.IsTrapEspEnabled      = data.IsTrapEspEnabled;
                Settings.IsCameraZoomEnabled   = data.IsCameraZoomEnabled;
                Settings.IsIQ250Enabled        = data.IsIQ250Enabled;
                Settings.IsLongHandsEnabled    = data.IsLongHandsEnabled;
                Settings.IsThroughWallEnabled  = data.IsThroughWallEnabled;
                Settings.IsAutoTranslateEnabled= data.IsAutoTranslateEnabled;
                Settings.IsAutoUnlockEnabled   = data.IsAutoUnlockEnabled;
                Settings.IsFreecamEnabled      = data.IsFreecamEnabled;
                Settings.IsFlightEnabled       = data.IsFlightEnabled;
                Settings.IsJumpBoostEnabled    = data.IsJumpBoostEnabled;
                Settings.IsNoClipEnabled              = data.IsNoClipEnabled;
                Settings.IsAutoBandageEnabled         = data.IsAutoBandageEnabled;
                Settings.IsAntiRagdollEnabled         = data.IsAntiRagdollEnabled;
                Settings.IsShrapnelMakerEnabled       = data.IsShrapnelMakerEnabled;
                Settings.IsInstantAmputationEnabled   = data.IsInstantAmputationEnabled;
                Settings.IsExplosivesMacroEnabled     = data.IsExplosivesMacroEnabled;
                Settings.IsFetchMacroEnabled           = data.IsFetchMacroEnabled;
                Settings.IsInstantShrapnelRemovalEnabled = data.IsInstantShrapnelRemovalEnabled;
                Settings.IsBoomboxEnabled             = data.IsBoomboxEnabled;
                Settings.StopMusicWhenTalking         = data.StopMusicWhenTalking;
                Settings.IsInfiniteAmmoEnabled        = data.IsInfiniteAmmoEnabled;
                Settings.IsNoSpreadEnabled            = data.IsNoSpreadEnabled;
                Settings.IsRapidFireEnabled           = data.IsRapidFireEnabled;
                Settings.IsNoJamEnabled               = data.IsNoJamEnabled;
                Settings.IsNoRecoilEnabled            = data.IsNoRecoilEnabled;
                Settings.IsAutoReloadEnabled          = data.IsAutoReloadEnabled;
                Settings.IsAutoBoltEnabled            = data.IsAutoBoltEnabled;
                Settings.IsMouseAimbotEnabled         = data.IsMouseAimbotEnabled;
                Settings.IsTrajectoryEnabled          = data.IsTrajectoryEnabled;
                Settings.TrajectoryMaxLength          = data.TrajectoryMaxLength > 0f ? data.TrajectoryMaxLength : 20f;
                Settings.IsSpeedModifierEnabled       = data.IsSpeedModifierEnabled;
                Settings.CustomSpeedMultiplier        = data.CustomSpeedMultiplier > 0f ? data.CustomSpeedMultiplier : 1.5f;
                Settings.IsAntiWeightEnabled          = data.IsAntiWeightEnabled;
                Settings.IsAirJumpEnabled             = data.IsAirJumpEnabled;
                Settings.IsJetpackEnabled             = data.IsJetpackEnabled;

                Settings.IsKillAuraEnabled          = data.IsKillAuraEnabled;
                Settings.KillAuraAttackPlayers      = data.KillAuraAttackPlayers;
                Settings.KillAuraAPS                = data.KillAuraAPS > 0f ? data.KillAuraAPS : 10f;
                Settings.KillAuraRange              = data.KillAuraRange > 0f ? data.KillAuraRange : 4f;
                Settings.KillAuraRenderTarget       = data.KillAuraRenderTarget;
                if (data.KillAuraTargetColor != null)
                    Settings.KillAuraTargetColor    = ToColor(data.KillAuraTargetColor);

                Settings.BrightenIntensity  = data.BrightenIntensity;
                Settings.CustomVisionRadius = data.CustomVisionRadius;
                Settings.CustomCameraSize   = data.CustomCameraSize;
                Settings.EspFontSize        = data.EspFontSize;
                Settings.CustomPickupRange  = data.CustomPickupRange;
                Settings.CustomJumpForce    = data.CustomJumpForce;
                Settings.FetchDistance      = data.FetchDistance;
                Settings.AimbotRadius       = data.AimbotRadius;
                Settings.CurrentTrackIndex  = data.CurrentTrackIndex;

                Settings.SelectedEspColor      = ToColor(data.SelectedEspColor);
                Settings.SelectedCreatureColor = ToColor(data.SelectedCreatureColor);
                Settings.SelectedTrapColor     = ToColor(data.SelectedTrapColor);
                if (data.TrajectoryColor != null)
                    Settings.TrajectoryColor  = ToColor(data.TrajectoryColor);

                Settings.FreecamPosition = new Vector3(data.FreecamX, data.FreecamY, data.FreecamZ);

                if (Enum.IsDefined(typeof(AppLanguage), data.Language))
                    Settings.CurrentLanguage = (AppLanguage)data.Language;

                Settings.TranslateSourceIndex = data.TranslateSourceIndex;
                Settings.TranslateTargetIndex = data.TranslateTargetIndex == 0 ? 1 : data.TranslateTargetIndex;
                Settings.IsTwoWayTranslationEnabled = data.IsTwoWayTranslationEnabled;
                Settings.CustomFireRateMultiplier = data.CustomFireRateMultiplier <= 0f ? 1.0f : data.CustomFireRateMultiplier;

                // --- 模块状态 ---
                if (data.Modules != null)
                {
                    var allMods = modManager.GetAllModules();
                    foreach (var entry in data.Modules)
                    {
                        ModuleBase mod = allMods.Find(m => m.GetType().Name == entry.ClassName);
                        if (mod == null && !string.IsNullOrEmpty(entry.Name))
                            mod = allMods.Find(m => m.Name == entry.Name);
                        if (mod == null) continue;

                        if (string.IsNullOrEmpty(entry.ClassName) && !string.IsNullOrEmpty(entry.Name))
                            entry.ClassName = mod.GetType().Name;

                        mod.Enabled = entry.Enabled;
                        mod.IsExpanded = entry.IsExpanded;
                        mod.BindKey = (KeyCode)entry.BindKey;

                        if (mod.Enabled)
                            mod.OnEnable();
                    }
                }

                // --- 面板布局 ---
                if (data.Panels != null && guiManager != null)
                {
                    foreach (var entry in data.Panels)
                    {
                        ModuleCategory cat = (ModuleCategory)entry.Category;
                        ClickGUIPanel panel = guiManager.Panels.Find(p => p.Category == cat);
                        if (panel == null) continue;

                        panel.Position.x = entry.X;
                        panel.Position.y = entry.Y;
                        panel.IsExpanded = entry.IsExpanded;
                    }
                }

                // ============================================================
                // ★ GlobalSettings 恢复 ★
                // ============================================================
                GlobalSettings.EnableGridSnap = data.EnableGridSnap;
                GlobalSettings.GridSize       = data.GridSize > 1f ? data.GridSize : 20f;

                GlobalSettings.EnableMinimap = data.EnableMinimap;
                if (data.MinimapRectW > 0f && data.MinimapRectH > 0f)
                {
                    GlobalSettings.MinimapRect = new Rect(data.MinimapRectX, data.MinimapRectY, data.MinimapRectW, data.MinimapRectH);
                }
                else
                {
                    // 默认安全位置
                    GlobalSettings.MinimapRect = new Rect(Screen.width - 230f, Screen.height - 230f, 210f, 210f);
                }
                
                GlobalSettings.MinimapRadius = data.MinimapRadius > 0f ? data.MinimapRadius : 30f;
                GlobalSettings.EnableLimbHUD = data.EnableLimbHUD;

                // 核心修复：防止旧配置文件默认读取到 (0, 0) 从而卡在屏幕最边缘只露一个角
                if (data.LimbHUDPosX == 0f && data.LimbHUDPosY == 0f)
                {
                    // 使用一个远离右下角死角的安全位置 (X 往左偏移250, Y 往上偏移150)
                    GlobalSettings.LimbHUDPosition = new Vector2(-250f, 150f); 
                }
                else
                {
                    GlobalSettings.LimbHUDPosition = new Vector2(data.LimbHUDPosX, data.LimbHUDPosY);
                }
                
                GlobalSettings.LimbHUDScale = data.LimbHUDScale > 0f ? data.LimbHUDScale : 0.6f;

                GlobalSettings.EnableLogoOverlay = data.EnableLogoOverlay;
                GlobalSettings.LogoOverlayHeight = data.LogoOverlayHeight > 0f ? data.LogoOverlayHeight : 300f;

                Debug.Log("[CheaterMod] 配置加载成功！");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheaterMod] 加载配置失败: {ex.Message}");
            }
        }

        // ============================================================
        // 颜色转换
        // ============================================================
        private static SColor ToSColor(Color c) => new SColor { r = c.r, g = c.g, b = c.b, a = c.a };
        private static Color ToColor(SColor c) => new Color(c.r, c.g, c.b, c.a);
    }
}
