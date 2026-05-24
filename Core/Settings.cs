using UnityEngine;
using System.Collections.Generic;

namespace FullBrightMod.Core
{
    public enum AppLanguage
    {
        Chinese,
        English
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
        public static bool IsCameraZoomEnabled   = false;
        public static bool IsIQ250Enabled        = false;
        public static bool IsLongHandsEnabled    = false;
        public static bool IsThroughWallEnabled  = false;
        public static bool IsAutoTranslateEnabled= false;
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
        public static List<string> AvailableMusicTracks = new List<string>();
        public static readonly string[] TranslateLangCodes = { 
            "auto", "zh-CN", "en", "ru", "ja", "ko", "es", "fr", "de" 
        };
        public static Color SelectedEspColor     = Color.green;
        public static Color SelectedCreatureColor= Color.cyan;
        public static Color SelectedTrapColor    = Color.red;
        public static Vector3 FreecamPosition    = Vector3.zero;
    }
}
