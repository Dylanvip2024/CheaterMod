using System;
using System.Collections.Generic;
using UnityEngine;

namespace FullBrightMod.Core
{
    public abstract class ModuleBase
    {
        public virtual string Name => GetType().Name;
        public virtual string Description => "";
        public virtual ModuleCategory Category => ModuleCategory.Misc;
        public bool Enabled { get; set; } = false;
        public bool IsExpanded { get; set; } = false;
        public KeyCode BindKey { get; set; } = KeyCode.None;
        public bool IsBinding { get; set; } = false;

        public virtual float GetSettingsHeight() => 0f;
        public virtual void DrawSettings(float x, ref float y, float width, Event e) { }

        public virtual void OnEnable()  { }
        public virtual void OnDisable() { }
        public virtual void OnUpdate()  { }
        public virtual void OnLateUpdate() { }
        public virtual void OnGUI()     { }

        // ==== 持久化自定义设置 ====
        public Dictionary<string, string> CustomSettings = new Dictionary<string, string>();

        public string GetSetting(string key, string defaultValue = "")
        {
            return CustomSettings.TryGetValue(key, out string v) ? v : defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            CustomSettings[key] = value;
        }

        public Color GetSetting(string key, Color defaultValue)
        {
            if (CustomSettings.TryGetValue(key, out string hex))
            {
                Color c;
                if (ColorUtility.TryParseHtmlString(hex, out c))
                    return c;
            }
            return defaultValue;
        }

        public void SetSetting(string key, Color value)
        {
            CustomSettings[key] = "#" + ColorUtility.ToHtmlStringRGBA(value);
        }

        internal void DumpSettings()
        {
            foreach (var kvp in CustomSettings)
                Debug.Log($"  [{GetType().Name}] {kvp.Key} = {kvp.Value}");
        }
    }
}
