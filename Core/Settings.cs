using UnityEngine;
using System.Collections.Generic;

namespace FullBrightMod.Core
{
    public enum AppLanguage
    {
        Chinese,
        English
    }

    public enum InventorySortMode
    {
        Space,
        Weight
    }

    public enum TranslationEngine
    {
        Google,
        OpenAI
    }

    /// <summary>
    /// 全局状态仓库 —— 所有模块的功能开关与动态参数集中于此。
    /// 各个 Module 和 Harmony Patch 均可通过 Settings.Xxx 读写。
    /// </summary>
    public static class Settings
    {
        // 语言设置
        public static AppLanguage CurrentLanguage = AppLanguage.Chinese;

        // ======== 功能开关（默认全部关闭，由用户在 ClickGUI 手动开启） ========
        public static bool IsFullBrightEnabled   = false;
        public static bool IsVisionExpandEnabled = false;
        public static bool IsItemEspEnabled      = false;
        public static bool IsCreatureEspEnabled  = false;
        public static bool IsTrapEspEnabled      = false;
        public static bool IsPlayerEspEnabled    = false;
        public static bool IsItemEspWireframeEnabled     = false;
        public static bool IsCreatureEspWireframeEnabled = false;
        public static bool IsTrapEspWireframeEnabled     = false;
        public static bool IsPlayerEspWireframeEnabled   = false;
        public static float EspLineWidth         = 2f;
        public static bool IsCameraZoomEnabled   = false;
        public static bool IsIQ250Enabled        = false;
        public static bool IsLongHandsEnabled    = false;
        public static bool IsThroughWallEnabled  = false;
        public static bool IsAutoTranslateEnabled= false;
        public static bool IsTwoWayTranslationEnabled = false;
        public static bool IsAutoUnlockEnabled   = false;
        public static bool IsFreecamEnabled      = false;
        public static bool IsFlightEnabled       = false;
        public static bool IsJumpBoostEnabled    = false;
        public static bool IsNoClipEnabled       = false;
        public static bool IsAutoBandageEnabled  = false;
        public static bool IsAntiRagdollEnabled  = false;
        public static bool IsShrapnelMakerEnabled = false;
        public static bool IsInstantAmputationEnabled = false;
        public static bool IsExplosivesMacroEnabled = false;
        public static bool IsFetchMacroEnabled = false;
        public static bool IsInstantShrapnelRemovalEnabled = false;
        public static bool IsBoomboxEnabled = false;
        public static bool StopMusicWhenTalking = true;
        public static bool IsSTTEnabled = false;
        public static bool IsSTTTranslateLinkEnabled = false;

        // 库存整理（一键执行，始终为 false，仅为持久化一致性）
        public static bool IsAutoSortSpaceEnabled = false;
        public static bool IsAutoSortWeightEnabled = false;
        public static bool IsInfiniteAmmoEnabled = false;
        public static bool IsNoSpreadEnabled = false;
        public static bool IsRapidFireEnabled = false;
        public static bool IsNoJamEnabled = false;
        public static bool IsNoRecoilEnabled = false;
        public static bool IsAutoReloadEnabled = false;
        public static bool IsAutoBoltEnabled = false;
        public static bool IsMouseAimbotEnabled = false;
        public static bool IsKillAuraEnabled       = false;
        public static bool KillAuraAttackPlayers   = false;
        public static float KillAuraAPS            = 10f;
        public static float KillAuraRange          = 4f;
        public static bool KillAuraRenderTarget    = true;
        public static Color KillAuraTargetColor    = Color.red;

        // ======== 速度修改 / 反负重 ========
        public static bool IsSpeedModifierEnabled = false;
        public static float CustomSpeedMultiplier = 1.5f;
        public static bool IsAntiWeightEnabled    = false;
        public static bool IsAirJumpEnabled       = false;
        public static bool IsJetpackEnabled       = false;

        // ======== CPR ========
        public static bool IsInstantReviveEnabled  = false;
        public static bool IsRibCrusherEnabled    = false;

        // ======== 自动推人 / 强制背人 ========
        public static bool IsAutoPushEnabled   = false;
        public static float AutoPushDistance   = 1.5f;
        public static bool IsAutoCarryEnabled  = false;

        // ======== 正骨大师 ========
        public static bool IsAutoDislocationEnabled = false;

        // ======== 长手 TP 模式 ========
        public static bool IsLongHandsTPModeEnabled = false;

        // ======== 相机传送 ========
        public static bool IsCameraTeleportEnabled = false;

        // ======== 杀戮光环传送攻击 ========
        public static bool KillAuraTeleportAttack  = false;
        public static float KillAuraTeleportRange  = 50f;

        // ======== Logo 水印 ========
        public static bool IsLogoOverlayEnabled  = false;

        public static TranslationEngine CurrentTranslationEngine = TranslationEngine.Google;
        public static string OpenAIBaseUrl  = "https://api.openai.com/v1";
        public static string OpenAIApiKey   = "";
        public static string OpenAIModel    = "gpt-4o-mini";
        public static bool IsTranslationContextEnabled = false;
        public static System.Collections.Generic.Queue<string> ChatHistory = new System.Collections.Generic.Queue<string>();

        public static InventorySortMode InventorySortMode = InventorySortMode.Space;

        // ======== 动态调节数值 ========
        public static float BrightenIntensity    = 1.0f;
        public static float CustomVisionRadius   = 25.0f;
        public static float CustomCameraSize     = 30.0f;
        public static int   EspFontSize          = 14;
        public static float CustomPickupRange    = 13.0f;
        public static float CustomJumpForce      = 18f;
        public static float FetchDistance = 13f;
        public static int CurrentTrackIndex = 0;
        public static int TranslateSourceIndex = 0;
        public static int TranslateTargetIndex = 1;
        public static float CustomFireRateMultiplier = 1.0f;
        public static float AimbotRadius = 150f;
        public static float SunCheckTimer = 0f;
        public static List<string> AvailableMusicTracks = new List<string>();
        public static readonly string[] TranslateLangCodes = { 
            "auto", "zh-CN", "en", "ru", "ja", "ko", "es", "fr", "de" 
        };
        public static Color SelectedEspColor     = Color.green;
        public static Color SelectedCreatureColor= Color.cyan;
        public static Color SelectedTrapColor    = Color.red;
        public static Color SelectedPlayerColor  = Color.magenta;
        public static Vector3 FreecamPosition    = Vector3.zero;

        // ======== 投掷轨迹 ========
        public static bool IsTrajectoryEnabled   = false;
        public static float TrajectoryMaxLength  = 40f;
        public static Color TrajectoryColor      = Color.red;
    }
}
