using System.Collections.Generic;
using FullBrightMod.Core;

namespace FullBrightMod.Utils
{
    public static class I18n
    {
        private static readonly Dictionary<string, string> _zh = new Dictionary<string, string>
        {
            // Category names
            { "cat_combat", "⚔ 战斗" },
            { "cat_player", "👤 玩家" },
            { "cat_movement", "🏃 移动" },
            { "cat_render", "👁 视觉" },
            { "cat_world", "🌍 世界" },
            { "cat_misc", "🔧 杂项" },

            // Player modules
            { "mod_autobandage", "绷带高手" },
            { "mod_freecam", "灵魂出窍" },
            { "mod_longhands", "长手模式" },
            { "mod_throughwall", "隔墙取物" },
            { "mod_shrapnel_maker", "破片制造者" },
            { "mod_instant_amputation", "秒截肢" },
            { "mod_instant_shrapnel", "秒拔破片" },

            // Render modules
            { "mod_fullbright", "全亮模式" },
            { "mod_itemesp", "物品透视" },
            { "mod_creatureesp", "生物透视" },
            { "mod_trapesp", "陷阱警告" },
            { "mod_visionexpand", "局部光照扩大" },
            { "mod_camerazoom", "视距拉远" },

            // Movement modules
            { "mod_flight", "超级飞侠" },
            { "mod_noclip", "实体穿墙" },
            { "mod_jumpboost", "跳跃增强" },

            // World modules
            { "mod_autounlock", "秒开锁" },

            // Misc modules
            { "mod_autotranslate", "聊天机翻" },
            { "set_trans_source", "源语言" },
            { "set_trans_target", "目标语言" },
            { "mod_iq250", "万事通模式" },
            { "mod_clickgui", "菜单设置" },
            { "mod_language", "Language" },
            { "mod_antiragdoll", "反布娃娃" },
            { "mod_fetch_macro", "捡取宏" },
            { "set_fetch_distance", "拾取距离" },
            { "mod_explosives_macro", "一键引爆" },
            { "mod_human_boombox", "人形音响" },
            { "set_boombox_track", "当前曲目" },

            // Settings labels
            { "set_intensity", "强度" },
            { "set_camera_size", "视距大小" },
            { "set_vision_radius", "光照范围" },
            { "set_jump_force", "跳跃力度" },
            { "set_item_color", "物品颜色" },
            { "set_creature_color", "生物颜色" },
            { "set_trap_color", "陷阱颜色" },
        };

        private static readonly Dictionary<string, string> _en = new Dictionary<string, string>
        {
            // Category names
            { "cat_combat", "⚔ Combat" },
            { "cat_player", "👤 Player" },
            { "cat_movement", "🏃 Movement" },
            { "cat_render", "👁 Render" },
            { "cat_world", "🌍 World" },
            { "cat_misc", "🔧 Misc" },

            // Player modules
            { "mod_autobandage", "Auto Bandage" },
            { "mod_freecam", "Free Camera" },
            { "mod_longhands", "Extended Reach" },
            { "mod_throughwall", "Through Wall" },
            { "mod_shrapnel_maker", "Shrapnel Maker" },
            { "mod_instant_amputation", "Instant Amputation" },
            { "mod_instant_shrapnel", "Instant Shrapnel Removal" },

            // Render modules
            { "mod_fullbright", "Full Luminance" },
            { "mod_itemesp", "Item ESP" },
            { "mod_creatureesp", "Creature ESP" },
            { "mod_trapesp", "Trap ESP" },
            { "mod_visionexpand", "Vision Expand" },
            { "mod_camerazoom", "Camera Zoom" },

            // Movement modules
            { "mod_flight", "Flight" },
            { "mod_noclip", "No-Clip" },
            { "mod_jumpboost", "Jump Boost" },

            // World modules
            { "mod_autounlock", "Auto Unlock" },

            // Misc modules
            { "mod_autotranslate", "Auto Translate" },
            { "set_trans_source", "Source Lang" },
            { "set_trans_target", "Target Lang" },
            { "mod_iq250", "Omniscience" },
            { "mod_clickgui", "GUI Config" },
            { "mod_language", "Language" },
            { "mod_antiragdoll", "AntiRagdoll" },
            { "mod_fetch_macro", "Fetch Macro" },
            { "set_fetch_distance", "Fetch Distance" },
            { "mod_explosives_macro", "Ignite Explosives Macro" },
            { "mod_human_boombox", "Human Boombox" },
            { "set_boombox_track", "Current Track" },

            // Settings labels
            { "set_intensity", "Intensity" },
            { "set_camera_size", "Camera Size" },
            { "set_vision_radius", "Vision Radius" },
            { "set_jump_force", "Jump Force" },
            { "set_item_color", "Item Color" },
            { "set_creature_color", "Creature Color" },
            { "set_trap_color", "Trap Color" },
        };

        public static string Get(string key)
        {
            var currentDict = Settings.CurrentLanguage == AppLanguage.English ? _en : _zh;
            if (currentDict.TryGetValue(key, out string value)) return value;
            return key;
        }
    }
}
