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
            public bool IsPlayerEspEnabled;
            public bool IsItemEspWireframeEnabled;
            public bool IsCreatureEspWireframeEnabled;
            public bool IsTrapEspWireframeEnabled;
            public bool IsPlayerEspWireframeEnabled;
            public float EspLineWidth;
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
            public int InventorySortMode; // 0=Space, 1=Weight
            public bool IsSTTEnabled;
            public bool IsSTTTranslateLinkEnabled;

            // ---- 库存整理 (一键执行，状态始终为 false，显式注册以防 ModuleEntry 遗漏) ----
            public bool AutoSortSpaceEnabled;
            public bool AutoSortWeightEnabled;
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
            public SColor SelectedPlayerColor;
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
            public int CurrentTranslationEngine; // 0=Google, 1=OpenAI
            public string OpenAIBaseUrl;
            public string OpenAIApiKey;
            public string OpenAIModel;
            public bool IsTranslationContextEnabled;

            // ---- 投掷轨迹 ----
            public bool IsTrajectoryEnabled;
            public float TrajectoryMaxLength;

            // ---- 速度修改 / 反负重 ----
            public bool IsSpeedModifierEnabled;
            public float CustomSpeedMultiplier;
            public bool IsAntiWeightEnabled;
            public bool IsAirJumpEnabled;
            public bool IsJetpackEnabled;

            // ---- CPR ----
            public bool IsInstantReviveEnabled;
            public bool IsRibCrusherEnabled;

            // ---- 自动推人 / 强制背人 ----
            public bool IsAutoPushEnabled;
            public float AutoPushDistance;
            public bool IsAutoCarryEnabled;

            // ---- 正骨大师 ----
            public bool IsAutoDislocationEnabled;

            // ---- 长手 TP 模式 ----
            public bool IsLongHandsTPModeEnabled;

            // ---- 相机传送 ----
            public bool IsCameraTeleportEnabled;

            // ---- 杀戮光环传送攻击 ----
            public bool KillAuraTeleportAttack;
            public float KillAuraTeleportRange;

            // ---- KillAura ----
            public bool IsKillAuraEnabled;
            public bool KillAuraAttackPlayers;
            public bool KillAuraAttackCreatures;
            public bool KillAuraAttackGlass;
            public float KillAuraAPS;
            public float KillAuraRange;
            public bool KillAuraRenderTarget;
            public SColor KillAuraTargetColor;
            public bool KillAuraPrioritizeHead;
            public int  KillAuraIndicatorStyle;

            // Flight
            public float FlightSpeed;

            // PickupAura
            public bool IsPickupAuraEnabled;
            public float PickupAuraRange;
            public bool PickupAuraFilterLowValue;
            public bool PickupAuraTeleportPickup;
            public float PickupAuraTeleportRange;

            // AntiSpikes
            public bool IsAntiSpikesEnabled;

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

            // CM使用者识别
            public bool EnableModRadar;

            public bool IsMeleeSpeedEnabled;
            public float MeleeSpeedMultiplier;

            public bool DisableBloodFx;
            public bool DisableTemperatureFx;
            public bool DisableMentalFx;
            public bool DisableBlurFx;
            public bool DisableRadiationFx;
            public bool DisableDamageFx;
            public bool DisableUIOverlays;

            public bool IsRemotePickupEnabled;
            public float RemotePickupRange;

            public bool IsContainerEspEnabled;
            public float ContainerEspRange;
            public SColor ContainerEspColor;
            public bool ContainerEspUseArrows;

            // TroublemakerAura
            public bool IsTroublemakerAuraEnabled;
            public int  TroublemakerTargetMode;
            public float TroublemakerRange;
            public bool TroublemakerEnableShrapnel;
            public bool TroublemakerEnableRibCrusher;
            public bool TroublemakerEnablePush;
            public bool TroublemakerEnableAmputation;
            public bool TroublemakerEnableBurn;

            // Auto Generator
            public bool IsAutoGeneratorEnabled;

            // Anti Solar Flare
            public bool IsAntiSolarFlareEnabled;

            // SoundCannon Indicator
            public bool IsSoundCannonIndicatorEnabled;

            // Auto Replenish
            public bool IsAutoReplenishEnabled;

            // FeatureList
            public bool EnableFeatureList;
            public float FeatureListRectX, FeatureListRectY, FeatureListRectW, FeatureListRectH;
            public bool FeatureListAlignLeft;
            public int FeatureListFontSize;
            public SColor FeatureListColor;
            public bool FeatureListRainbow;
            public bool FeatureListBackground;
            public bool FeatureListAccentLine;

            // UI Sound
            public bool EnableToggleSound;

            // Toast Notifications
            public bool EnableNotifications;
            public float NotificationYPosition;
            public bool NotificationSlideFromRight;
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
                data.IsFullBrightEnabled   = ModSettings.IsFullBrightEnabled;
                data.IsVisionExpandEnabled = ModSettings.IsVisionExpandEnabled;
                data.IsItemEspEnabled      = ModSettings.IsItemEspEnabled;
                data.IsCreatureEspEnabled  = ModSettings.IsCreatureEspEnabled;
                data.IsTrapEspEnabled      = ModSettings.IsTrapEspEnabled;
                data.IsPlayerEspEnabled    = ModSettings.IsPlayerEspEnabled;
                data.IsItemEspWireframeEnabled     = ModSettings.IsItemEspWireframeEnabled;
                data.IsCreatureEspWireframeEnabled = ModSettings.IsCreatureEspWireframeEnabled;
                data.IsTrapEspWireframeEnabled     = ModSettings.IsTrapEspWireframeEnabled;
                data.IsPlayerEspWireframeEnabled   = ModSettings.IsPlayerEspWireframeEnabled;
                data.EspLineWidth          = ModSettings.EspLineWidth;
                data.IsCameraZoomEnabled   = ModSettings.IsCameraZoomEnabled;
                data.IsIQ250Enabled        = ModSettings.IsIQ250Enabled;
                data.IsLongHandsEnabled    = ModSettings.IsLongHandsEnabled;
                data.IsThroughWallEnabled  = ModSettings.IsThroughWallEnabled;
                data.IsAutoTranslateEnabled= ModSettings.IsAutoTranslateEnabled;
                data.IsAutoUnlockEnabled   = ModSettings.IsAutoUnlockEnabled;
                data.IsFreecamEnabled      = ModSettings.IsFreecamEnabled;
                data.IsFlightEnabled       = ModSettings.IsFlightEnabled;
                data.IsJumpBoostEnabled    = ModSettings.IsJumpBoostEnabled;
                data.IsNoClipEnabled              = ModSettings.IsNoClipEnabled;
                data.IsAutoBandageEnabled         = ModSettings.IsAutoBandageEnabled;
                data.IsAntiRagdollEnabled         = ModSettings.IsAntiRagdollEnabled;
                data.IsShrapnelMakerEnabled       = ModSettings.IsShrapnelMakerEnabled;
                data.IsInstantAmputationEnabled   = ModSettings.IsInstantAmputationEnabled;
                data.IsExplosivesMacroEnabled     = ModSettings.IsExplosivesMacroEnabled;
                data.IsFetchMacroEnabled           = ModSettings.IsFetchMacroEnabled;
                data.IsInstantShrapnelRemovalEnabled = ModSettings.IsInstantShrapnelRemovalEnabled;
                data.IsBoomboxEnabled             = ModSettings.IsBoomboxEnabled;
                data.StopMusicWhenTalking         = ModSettings.StopMusicWhenTalking;
                data.InventorySortMode            = (int)ModSettings.InventorySortMode;
                data.IsSTTEnabled                 = ModSettings.IsSTTEnabled;
                data.IsSTTTranslateLinkEnabled  = ModSettings.IsSTTTranslateLinkEnabled;
                data.AutoSortSpaceEnabled     = ModSettings.IsAutoSortSpaceEnabled;
                data.AutoSortWeightEnabled    = ModSettings.IsAutoSortWeightEnabled;
                data.IsInfiniteAmmoEnabled        = ModSettings.IsInfiniteAmmoEnabled;
                data.IsNoSpreadEnabled            = ModSettings.IsNoSpreadEnabled;
                data.IsRapidFireEnabled           = ModSettings.IsRapidFireEnabled;
                data.IsNoJamEnabled               = ModSettings.IsNoJamEnabled;
                data.IsNoRecoilEnabled            = ModSettings.IsNoRecoilEnabled;
                data.IsAutoReloadEnabled          = ModSettings.IsAutoReloadEnabled;
                data.IsAutoBoltEnabled            = ModSettings.IsAutoBoltEnabled;
                data.IsMouseAimbotEnabled         = ModSettings.IsMouseAimbotEnabled;
                data.IsTrajectoryEnabled          = ModSettings.IsTrajectoryEnabled;
                data.TrajectoryMaxLength          = ModSettings.TrajectoryMaxLength;
                data.IsSpeedModifierEnabled       = ModSettings.IsSpeedModifierEnabled;
                data.CustomSpeedMultiplier        = ModSettings.CustomSpeedMultiplier;
                data.IsAntiWeightEnabled          = ModSettings.IsAntiWeightEnabled;
                data.IsAirJumpEnabled             = ModSettings.IsAirJumpEnabled;
                data.IsJetpackEnabled             = ModSettings.IsJetpackEnabled;
                data.IsInstantReviveEnabled        = ModSettings.IsInstantReviveEnabled;
                data.IsRibCrusherEnabled          = ModSettings.IsRibCrusherEnabled;

                data.IsAutoPushEnabled          = ModSettings.IsAutoPushEnabled;
                data.AutoPushDistance           = ModSettings.AutoPushDistance;
                data.IsAutoCarryEnabled         = ModSettings.IsAutoCarryEnabled;
                data.IsAutoDislocationEnabled  = ModSettings.IsAutoDislocationEnabled;
                data.IsLongHandsTPModeEnabled   = ModSettings.IsLongHandsTPModeEnabled;

                data.IsCameraTeleportEnabled    = ModSettings.IsCameraTeleportEnabled;

                data.KillAuraTeleportAttack     = ModSettings.KillAuraTeleportAttack;
                data.KillAuraTeleportRange      = ModSettings.KillAuraTeleportRange;

                data.IsKillAuraEnabled          = ModSettings.IsKillAuraEnabled;
                data.KillAuraAttackPlayers      = ModSettings.KillAuraAttackPlayers;
                data.KillAuraAttackCreatures  = ModSettings.KillAuraAttackCreatures;
                data.KillAuraAttackGlass      = ModSettings.KillAuraAttackGlass;
                data.KillAuraAPS                = ModSettings.KillAuraAPS;
                data.KillAuraRange              = ModSettings.KillAuraRange;
                data.KillAuraRenderTarget       = ModSettings.KillAuraRenderTarget;
                data.KillAuraTargetColor        = ToSColor(ModSettings.KillAuraTargetColor);
                data.KillAuraPrioritizeHead     = ModSettings.KillAuraPrioritizeHead;
                data.KillAuraIndicatorStyle     = ModSettings.KillAuraIndicatorStyle;

                data.FlightSpeed                = ModSettings.FlightSpeed;

                data.IsPickupAuraEnabled        = ModSettings.IsPickupAuraEnabled;
                data.PickupAuraRange              = ModSettings.PickupAuraRange;
                data.PickupAuraFilterLowValue   = ModSettings.PickupAuraFilterLowValue;
                data.PickupAuraTeleportPickup   = ModSettings.PickupAuraTeleportPickup;
                data.PickupAuraTeleportRange    = ModSettings.PickupAuraTeleportRange;

                data.IsAntiSpikesEnabled        = ModSettings.IsAntiSpikesEnabled;
                data.CustomFireRateMultiplier     = ModSettings.CustomFireRateMultiplier;

                data.BrightenIntensity  = ModSettings.BrightenIntensity;
                data.CustomVisionRadius = ModSettings.CustomVisionRadius;
                data.CustomCameraSize   = ModSettings.CustomCameraSize;
                data.EspFontSize        = ModSettings.EspFontSize;
                data.CustomPickupRange  = ModSettings.CustomPickupRange;
                data.CustomJumpForce    = ModSettings.CustomJumpForce;
                data.FetchDistance      = ModSettings.FetchDistance;
                data.AimbotRadius       = ModSettings.AimbotRadius;
                data.CurrentTrackIndex  = ModSettings.CurrentTrackIndex;

                data.SelectedEspColor      = ToSColor(ModSettings.SelectedEspColor);
                data.SelectedCreatureColor = ToSColor(ModSettings.SelectedCreatureColor);
                data.SelectedTrapColor     = ToSColor(ModSettings.SelectedTrapColor);
                data.SelectedPlayerColor   = ToSColor(ModSettings.SelectedPlayerColor);
                data.TrajectoryColor       = ToSColor(ModSettings.TrajectoryColor);

                data.FreecamX = ModSettings.FreecamPosition.x;
                data.FreecamY = ModSettings.FreecamPosition.y;
                data.FreecamZ = ModSettings.FreecamPosition.z;

                data.Language = (int)ModSettings.CurrentLanguage;

                data.TranslateSourceIndex = ModSettings.TranslateSourceIndex;
                data.TranslateTargetIndex = ModSettings.TranslateTargetIndex;
                data.IsTwoWayTranslationEnabled = ModSettings.IsTwoWayTranslationEnabled;
                data.CurrentTranslationEngine = (int)ModSettings.CurrentTranslationEngine;
                data.OpenAIBaseUrl = ModSettings.OpenAIBaseUrl;
                data.OpenAIApiKey = ModSettings.OpenAIApiKey;
                data.OpenAIModel = ModSettings.OpenAIModel;
                data.IsTranslationContextEnabled = ModSettings.IsTranslationContextEnabled;

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

                data.EnableModRadar     = GlobalSettings.EnableModRadar;

                data.IsMeleeSpeedEnabled      = ModSettings.IsMeleeSpeedEnabled;
                data.MeleeSpeedMultiplier      = ModSettings.MeleeSpeedMultiplier;

                data.DisableBloodFx       = ModSettings.DisableBloodFx;
                data.DisableTemperatureFx = ModSettings.DisableTemperatureFx;
                data.DisableMentalFx      = ModSettings.DisableMentalFx;
                data.DisableBlurFx        = ModSettings.DisableBlurFx;
                data.DisableRadiationFx   = ModSettings.DisableRadiationFx;
                data.DisableDamageFx      = ModSettings.DisableDamageFx;
                data.DisableUIOverlays    = ModSettings.DisableUIOverlays;

                data.IsRemotePickupEnabled = ModSettings.IsRemotePickupEnabled;
                data.RemotePickupRange     = ModSettings.RemotePickupRange;

                data.IsContainerEspEnabled = ModSettings.IsContainerEspEnabled;
                data.ContainerEspRange     = ModSettings.ContainerEspRange;
                data.ContainerEspColor     = ToSColor(ModSettings.ContainerEspColor);
                data.ContainerEspUseArrows = ModSettings.ContainerEspUseArrows;

                data.IsTroublemakerAuraEnabled   = ModSettings.IsTroublemakerAuraEnabled;
                data.TroublemakerTargetMode      = ModSettings.TroublemakerTargetMode;
                data.TroublemakerRange            = ModSettings.TroublemakerRange;
                data.TroublemakerEnableShrapnel  = ModSettings.TroublemakerEnableShrapnel;
                data.TroublemakerEnableRibCrusher = ModSettings.TroublemakerEnableRibCrusher;
                data.TroublemakerEnablePush      = ModSettings.TroublemakerEnablePush;
                data.TroublemakerEnableAmputation = ModSettings.TroublemakerEnableAmputation;
                data.TroublemakerEnableBurn       = ModSettings.TroublemakerEnableBurn;

                data.IsAutoGeneratorEnabled = ModSettings.IsAutoGeneratorEnabled;

                data.IsAntiSolarFlareEnabled = ModSettings.IsAntiSolarFlareEnabled;

                data.IsSoundCannonIndicatorEnabled = ModSettings.IsSoundCannonIndicatorEnabled;

                data.IsAutoReplenishEnabled = ModSettings.IsAutoReplenishEnabled;

                data.EnableFeatureList   = GlobalSettings.EnableFeatureList;
                data.FeatureListRectX    = GlobalSettings.FeatureListRect.x;
                data.FeatureListRectY    = GlobalSettings.FeatureListRect.y;
                data.FeatureListRectW    = GlobalSettings.FeatureListRect.width;
                data.FeatureListRectH    = GlobalSettings.FeatureListRect.height;
                data.FeatureListAlignLeft = GlobalSettings.FeatureListAlignLeft;
                data.FeatureListFontSize = GlobalSettings.FeatureListFontSize;
                data.FeatureListColor    = ToSColor(GlobalSettings.FeatureListColor);
                data.FeatureListRainbow  = GlobalSettings.FeatureListRainbow;
                data.FeatureListBackground = GlobalSettings.FeatureListBackground;
                data.FeatureListAccentLine = GlobalSettings.FeatureListAccentLine;

                data.EnableToggleSound = GlobalSettings.EnableToggleSound;

                data.EnableNotifications = GlobalSettings.EnableNotifications;
                data.NotificationYPosition = GlobalSettings.NotificationYPosition;
                data.NotificationSlideFromRight = GlobalSettings.NotificationSlideFromRight;

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
                ModSettings.IsFullBrightEnabled   = data.IsFullBrightEnabled;
                ModSettings.IsVisionExpandEnabled = data.IsVisionExpandEnabled;
                ModSettings.IsItemEspEnabled      = data.IsItemEspEnabled;
                ModSettings.IsCreatureEspEnabled  = data.IsCreatureEspEnabled;
                ModSettings.IsTrapEspEnabled      = data.IsTrapEspEnabled;
                ModSettings.IsPlayerEspEnabled    = data.IsPlayerEspEnabled;
                ModSettings.IsItemEspWireframeEnabled     = data.IsItemEspWireframeEnabled;
                ModSettings.IsCreatureEspWireframeEnabled = data.IsCreatureEspWireframeEnabled;
                ModSettings.IsTrapEspWireframeEnabled     = data.IsTrapEspWireframeEnabled;
                ModSettings.IsPlayerEspWireframeEnabled   = data.IsPlayerEspWireframeEnabled;
                ModSettings.EspLineWidth          = data.EspLineWidth > 0f ? data.EspLineWidth : 2f;
                ModSettings.IsCameraZoomEnabled   = data.IsCameraZoomEnabled;
                ModSettings.IsIQ250Enabled        = data.IsIQ250Enabled;
                ModSettings.IsLongHandsEnabled    = data.IsLongHandsEnabled;
                ModSettings.IsThroughWallEnabled  = data.IsThroughWallEnabled;
                ModSettings.IsAutoTranslateEnabled= data.IsAutoTranslateEnabled;
                ModSettings.IsAutoUnlockEnabled   = data.IsAutoUnlockEnabled;
                ModSettings.IsFreecamEnabled      = data.IsFreecamEnabled;
                ModSettings.IsFlightEnabled       = data.IsFlightEnabled;
                ModSettings.IsJumpBoostEnabled    = data.IsJumpBoostEnabled;
                ModSettings.IsNoClipEnabled              = data.IsNoClipEnabled;
                ModSettings.IsAutoBandageEnabled         = data.IsAutoBandageEnabled;
                ModSettings.IsAntiRagdollEnabled         = data.IsAntiRagdollEnabled;
                ModSettings.IsShrapnelMakerEnabled       = data.IsShrapnelMakerEnabled;
                ModSettings.IsInstantAmputationEnabled   = data.IsInstantAmputationEnabled;
                ModSettings.IsExplosivesMacroEnabled     = data.IsExplosivesMacroEnabled;
                ModSettings.IsFetchMacroEnabled           = data.IsFetchMacroEnabled;
                ModSettings.IsInstantShrapnelRemovalEnabled = data.IsInstantShrapnelRemovalEnabled;
                ModSettings.IsBoomboxEnabled             = data.IsBoomboxEnabled;
                ModSettings.StopMusicWhenTalking         = data.StopMusicWhenTalking;
                if (Enum.IsDefined(typeof(InventorySortMode), data.InventorySortMode))
                    ModSettings.InventorySortMode    = (InventorySortMode)data.InventorySortMode;
                ModSettings.IsSTTEnabled                 = data.IsSTTEnabled;
                ModSettings.IsSTTTranslateLinkEnabled  = data.IsSTTTranslateLinkEnabled;
                ModSettings.IsAutoSortSpaceEnabled     = data.AutoSortSpaceEnabled;
                ModSettings.IsAutoSortWeightEnabled    = data.AutoSortWeightEnabled;
                ModSettings.IsInfiniteAmmoEnabled        = data.IsInfiniteAmmoEnabled;
                ModSettings.IsNoSpreadEnabled            = data.IsNoSpreadEnabled;
                ModSettings.IsRapidFireEnabled           = data.IsRapidFireEnabled;
                ModSettings.IsNoJamEnabled               = data.IsNoJamEnabled;
                ModSettings.IsNoRecoilEnabled            = data.IsNoRecoilEnabled;
                ModSettings.IsAutoReloadEnabled          = data.IsAutoReloadEnabled;
                ModSettings.IsAutoBoltEnabled            = data.IsAutoBoltEnabled;
                ModSettings.IsMouseAimbotEnabled         = data.IsMouseAimbotEnabled;
                ModSettings.IsTrajectoryEnabled          = data.IsTrajectoryEnabled;
                ModSettings.TrajectoryMaxLength          = data.TrajectoryMaxLength > 0f ? data.TrajectoryMaxLength : 20f;
                ModSettings.IsSpeedModifierEnabled       = data.IsSpeedModifierEnabled;
                ModSettings.CustomSpeedMultiplier        = data.CustomSpeedMultiplier > 0f ? data.CustomSpeedMultiplier : 1.5f;
                ModSettings.IsAntiWeightEnabled          = data.IsAntiWeightEnabled;
                ModSettings.IsAirJumpEnabled             = data.IsAirJumpEnabled;
                ModSettings.IsJetpackEnabled             = data.IsJetpackEnabled;
                ModSettings.IsInstantReviveEnabled        = data.IsInstantReviveEnabled;
                ModSettings.IsRibCrusherEnabled          = data.IsRibCrusherEnabled;

                ModSettings.IsAutoPushEnabled          = data.IsAutoPushEnabled;
                ModSettings.AutoPushDistance           = data.AutoPushDistance > 0f ? data.AutoPushDistance : 1.5f;
                ModSettings.IsAutoCarryEnabled         = data.IsAutoCarryEnabled;
                ModSettings.IsAutoDislocationEnabled  = data.IsAutoDislocationEnabled;
                ModSettings.IsLongHandsTPModeEnabled   = data.IsLongHandsTPModeEnabled;

                ModSettings.IsCameraTeleportEnabled    = data.IsCameraTeleportEnabled;

                ModSettings.KillAuraTeleportAttack     = data.KillAuraTeleportAttack;
                ModSettings.KillAuraTeleportRange      = data.KillAuraTeleportRange > 0f ? data.KillAuraTeleportRange : 50f;

                ModSettings.IsKillAuraEnabled          = data.IsKillAuraEnabled;
                ModSettings.KillAuraAttackPlayers      = data.KillAuraAttackPlayers;
                ModSettings.KillAuraAttackCreatures  = data.KillAuraAttackCreatures;
                ModSettings.KillAuraAttackGlass      = data.KillAuraAttackGlass;
                ModSettings.KillAuraAPS                = data.KillAuraAPS > 0f ? data.KillAuraAPS : 10f;
                ModSettings.KillAuraRange              = data.KillAuraRange > 0f ? data.KillAuraRange : 4f;
                ModSettings.KillAuraRenderTarget       = data.KillAuraRenderTarget;
                if (data.KillAuraTargetColor != null)
                    ModSettings.KillAuraTargetColor    = ToColor(data.KillAuraTargetColor);

                ModSettings.KillAuraPrioritizeHead = data.KillAuraPrioritizeHead;
                ModSettings.KillAuraIndicatorStyle = data.KillAuraIndicatorStyle;

                ModSettings.FlightSpeed            = data.FlightSpeed > 0f ? data.FlightSpeed : 15f;

                ModSettings.IsPickupAuraEnabled       = data.IsPickupAuraEnabled;
                ModSettings.PickupAuraRange            = data.PickupAuraRange > 0f ? data.PickupAuraRange : 10f;
                ModSettings.PickupAuraFilterLowValue = data.PickupAuraFilterLowValue;
                ModSettings.PickupAuraTeleportPickup = data.PickupAuraTeleportPickup;
                ModSettings.PickupAuraTeleportRange  = data.PickupAuraTeleportRange > 0f ? data.PickupAuraTeleportRange : 200f;

                ModSettings.IsAntiSpikesEnabled     = data.IsAntiSpikesEnabled;

                ModSettings.BrightenIntensity  = data.BrightenIntensity;
                ModSettings.CustomVisionRadius = data.CustomVisionRadius;
                ModSettings.CustomCameraSize   = data.CustomCameraSize;
                ModSettings.EspFontSize        = data.EspFontSize;
                ModSettings.CustomPickupRange  = data.CustomPickupRange;
                ModSettings.CustomJumpForce    = data.CustomJumpForce;
                ModSettings.FetchDistance      = data.FetchDistance;
                ModSettings.AimbotRadius       = data.AimbotRadius;
                ModSettings.CurrentTrackIndex  = data.CurrentTrackIndex;

                ModSettings.SelectedEspColor      = ToColor(data.SelectedEspColor);
                ModSettings.SelectedCreatureColor = ToColor(data.SelectedCreatureColor);
                ModSettings.SelectedTrapColor     = ToColor(data.SelectedTrapColor);
                if (data.SelectedPlayerColor != null)
                    ModSettings.SelectedPlayerColor = ToColor(data.SelectedPlayerColor);
                if (data.TrajectoryColor != null)
                    ModSettings.TrajectoryColor  = ToColor(data.TrajectoryColor);

                ModSettings.FreecamPosition = new Vector3(data.FreecamX, data.FreecamY, data.FreecamZ);

                if (Enum.IsDefined(typeof(AppLanguage), data.Language))
                    ModSettings.CurrentLanguage = (AppLanguage)data.Language;

                ModSettings.TranslateSourceIndex = data.TranslateSourceIndex;
                ModSettings.TranslateTargetIndex = data.TranslateTargetIndex == 0 ? 1 : data.TranslateTargetIndex;
                ModSettings.IsTwoWayTranslationEnabled = data.IsTwoWayTranslationEnabled;
                if (Enum.IsDefined(typeof(TranslationEngine), data.CurrentTranslationEngine))
                    ModSettings.CurrentTranslationEngine = (TranslationEngine)data.CurrentTranslationEngine;
                if (!string.IsNullOrEmpty(data.OpenAIBaseUrl)) ModSettings.OpenAIBaseUrl = data.OpenAIBaseUrl;
                if (data.OpenAIApiKey != null) ModSettings.OpenAIApiKey = data.OpenAIApiKey;
                if (!string.IsNullOrEmpty(data.OpenAIModel)) ModSettings.OpenAIModel = data.OpenAIModel;
                ModSettings.IsTranslationContextEnabled = data.IsTranslationContextEnabled;
                ModSettings.CustomFireRateMultiplier = data.CustomFireRateMultiplier <= 0f ? 1.0f : data.CustomFireRateMultiplier;

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

                GlobalSettings.EnableModRadar = data.EnableModRadar;

                ModSettings.IsMeleeSpeedEnabled      = data.IsMeleeSpeedEnabled;
                ModSettings.MeleeSpeedMultiplier      = data.MeleeSpeedMultiplier > 0f ? data.MeleeSpeedMultiplier : 2f;

                ModSettings.DisableBloodFx       = data.DisableBloodFx;
                ModSettings.DisableTemperatureFx = data.DisableTemperatureFx;
                ModSettings.DisableMentalFx      = data.DisableMentalFx;
                ModSettings.DisableBlurFx        = data.DisableBlurFx;
                ModSettings.DisableRadiationFx   = data.DisableRadiationFx;
                ModSettings.DisableDamageFx      = data.DisableDamageFx;
                ModSettings.DisableUIOverlays    = data.DisableUIOverlays;

                ModSettings.IsRemotePickupEnabled = data.IsRemotePickupEnabled;
                ModSettings.RemotePickupRange     = data.RemotePickupRange > 0f ? data.RemotePickupRange : 50f;

                ModSettings.IsContainerEspEnabled = data.IsContainerEspEnabled;
                ModSettings.ContainerEspRange     = data.ContainerEspRange > 0f ? data.ContainerEspRange : 50f;
                if (data.ContainerEspColor != null)
                    ModSettings.ContainerEspColor = ToColor(data.ContainerEspColor);
                ModSettings.ContainerEspUseArrows = data.ContainerEspUseArrows;

                ModSettings.IsTroublemakerAuraEnabled   = data.IsTroublemakerAuraEnabled;
                ModSettings.TroublemakerTargetMode      = data.TroublemakerTargetMode;
                ModSettings.TroublemakerRange            = data.TroublemakerRange > 0f ? data.TroublemakerRange : 10f;
                ModSettings.TroublemakerEnableShrapnel  = data.TroublemakerEnableShrapnel;
                ModSettings.TroublemakerEnableRibCrusher = data.TroublemakerEnableRibCrusher;
                ModSettings.TroublemakerEnablePush      = data.TroublemakerEnablePush;
                ModSettings.TroublemakerEnableAmputation = data.TroublemakerEnableAmputation;
                ModSettings.TroublemakerEnableBurn       = data.TroublemakerEnableBurn;

                ModSettings.IsAutoGeneratorEnabled = data.IsAutoGeneratorEnabled;

                ModSettings.IsAntiSolarFlareEnabled = data.IsAntiSolarFlareEnabled;

                ModSettings.IsSoundCannonIndicatorEnabled = data.IsSoundCannonIndicatorEnabled;

                ModSettings.IsAutoReplenishEnabled = data.IsAutoReplenishEnabled;

                GlobalSettings.EnableFeatureList   = data.EnableFeatureList;
                // ★ 防越界检测：确保列表坐标在屏幕可见范围内
                float safeX = Mathf.Clamp(data.FeatureListRectX, 0f, Mathf.Max(0f, Screen.width - 50f));
                float safeY = Mathf.Clamp(data.FeatureListRectY, 0f, Mathf.Max(0f, Screen.height - 50f));
                GlobalSettings.FeatureListRect = new Rect(safeX, safeY, 200f, 200f);
                GlobalSettings.FeatureListAlignLeft = data.FeatureListAlignLeft;
                GlobalSettings.FeatureListFontSize = data.FeatureListFontSize > 0 ? data.FeatureListFontSize : 14;
                if (data.FeatureListColor != null)
                    GlobalSettings.FeatureListColor = ToColor(data.FeatureListColor);
                GlobalSettings.FeatureListRainbow   = data.FeatureListRainbow;
                GlobalSettings.FeatureListBackground = data.FeatureListBackground;
                GlobalSettings.FeatureListAccentLine = data.FeatureListAccentLine;

                GlobalSettings.EnableToggleSound = data.EnableToggleSound;

                GlobalSettings.EnableNotifications = data.EnableNotifications;
                GlobalSettings.NotificationYPosition = data.NotificationYPosition > 0f ? data.NotificationYPosition : 60f;
                GlobalSettings.NotificationSlideFromRight = data.NotificationSlideFromRight;

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
