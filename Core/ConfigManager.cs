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
    /// 将 Settings、模块状态、面板布局序列化到 BepInEx/config/CheaterMod.json。
    /// </summary>
    public static class ConfigManager
    {
        private static string ConfigPath => Path.Combine(BepInEx.Paths.ConfigPath, "CheaterMod.json");

        // ============================================================
        // 纯数据容器（用于 JSON 序列化）
        // ============================================================

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

            public float BrightenIntensity;
            public float CustomVisionRadius;
            public float CustomCameraSize;
            public int EspFontSize;
            public float CustomPickupRange;
            public float CustomJumpForce;

            public SColor SelectedEspColor;
            public SColor SelectedCreatureColor;
            public SColor SelectedTrapColor;

            public float FreecamX, FreecamY, FreecamZ;

            // ---- 语言设置 ----
            public int Language; // 0 = Chinese, 1 = English

            // ---- 模块状态列表 ----
            public List<ModuleEntry> Modules = new List<ModuleEntry>();

            // ---- 面板布局列表 ----
            public List<PanelEntry> Panels = new List<PanelEntry>();
        }

        /// <summary>可序列化的颜色（Unity Color 非 [Serializable]）</summary>
        [Serializable]
        public class SColor { public float r, g, b, a; }

        /// <summary>单个模块的持久化状态（以类名 GetType().Name 标识，不受语言影响）</summary>
        [Serializable]
        public class ModuleEntry
        {
            /// <summary>模块类名（如 "AutoBandage"），永远不变的唯一标识</summary>
            public string ClassName;
            public bool Enabled;
            public bool IsExpanded;
            public int BindKey;

            /// <summary>旧版兼容：以 module.Name 保存的字段（仅在旧配置中出现）</summary>
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string Name;
        }

        /// <summary>单个面板的持久化布局</summary>
        [Serializable]
        public class PanelEntry
        {
            public int Category; // ModuleCategory 枚举 int 值
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

                // --- 从 Settings 静态类复制 ---
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
                data.IsNoClipEnabled       = Settings.IsNoClipEnabled;

                data.BrightenIntensity  = Settings.BrightenIntensity;
                data.CustomVisionRadius = Settings.CustomVisionRadius;
                data.CustomCameraSize   = Settings.CustomCameraSize;
                data.EspFontSize        = Settings.EspFontSize;
                data.CustomPickupRange  = Settings.CustomPickupRange;
                data.CustomJumpForce    = Settings.CustomJumpForce;

                data.SelectedEspColor      = ToSColor(Settings.SelectedEspColor);
                data.SelectedCreatureColor = ToSColor(Settings.SelectedCreatureColor);
                data.SelectedTrapColor     = ToSColor(Settings.SelectedTrapColor);

                data.FreecamX = Settings.FreecamPosition.x;
                data.FreecamY = Settings.FreecamPosition.y;
                data.FreecamZ = Settings.FreecamPosition.z;

                // --- 保存语言设置 ---
                data.Language = (int)Settings.CurrentLanguage;

                // --- 从 ModuleManager 复制模块状态 ---
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

                // --- 从 ClickGUIManager 复制面板布局 ---
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

                // --- 恢复到 Settings ---
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
                Settings.IsNoClipEnabled       = data.IsNoClipEnabled;

                Settings.BrightenIntensity  = data.BrightenIntensity;
                Settings.CustomVisionRadius = data.CustomVisionRadius;
                Settings.CustomCameraSize   = data.CustomCameraSize;
                Settings.EspFontSize        = data.EspFontSize;
                Settings.CustomPickupRange  = data.CustomPickupRange;
                Settings.CustomJumpForce    = data.CustomJumpForce;

                Settings.SelectedEspColor      = ToColor(data.SelectedEspColor);
                Settings.SelectedCreatureColor = ToColor(data.SelectedCreatureColor);
                Settings.SelectedTrapColor     = ToColor(data.SelectedTrapColor);

                Settings.FreecamPosition = new Vector3(data.FreecamX, data.FreecamY, data.FreecamZ);

                // --- 恢复语言设置（必须在模块恢复之前，确保模块 Name 能正确渲染） ---
                if (Enum.IsDefined(typeof(AppLanguage), data.Language))
                    Settings.CurrentLanguage = (AppLanguage)data.Language;

                // --- 恢复到模块 ---
                if (data.Modules != null)
                {
                    var allMods = modManager.GetAllModules();
                    foreach (var entry in data.Modules)
                    {
                        // 优先按 ClassName 匹配（新版）
                        ModuleBase mod = allMods.Find(m => m.GetType().Name == entry.ClassName);

                        // 兼容旧版：尝试按本地化 Name 匹配（仅当 ClassName 为空或未匹配到时）
                        if (mod == null && !string.IsNullOrEmpty(entry.Name))
                            mod = allMods.Find(m => m.Name == entry.Name);

                        if (mod == null) continue;

                        // 迁移：如果旧配置有 Name 但没有 ClassName，自动补上
                        if (string.IsNullOrEmpty(entry.ClassName) && !string.IsNullOrEmpty(entry.Name))
                            entry.ClassName = mod.GetType().Name;

                        mod.Enabled = entry.Enabled;
                        mod.IsExpanded = entry.IsExpanded;
                        mod.BindKey = (KeyCode)entry.BindKey;

                        // 如果模块在存档中是启用状态，触发 OnEnable
                        if (mod.Enabled)
                            mod.OnEnable();
                    }
                }

                // --- 恢复到面板布局 ---
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

                Debug.Log("[CheaterMod] 配置加载成功！");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheaterMod] 加载配置失败: {ex.Message}");
            }
        }

        // ============================================================
        // 颜色转换辅助
        // ============================================================
        private static SColor ToSColor(Color c) => new SColor { r = c.r, g = c.g, b = c.b, a = c.a };
        private static Color ToColor(SColor c) => new Color(c.r, c.g, c.b, c.a);
    }
}
