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

            // Combat modules
            { "mod_rapid_fire", "自定义射速" },
            { "mod_no_jam", "不卡壳" },
            { "set_fire_rate_mult", "射速倍率" },
            { "mod_no_recoil", "无后座" },
            { "mod_auto_reload", "自动装弹" },
            { "mod_auto_bolt", "自动拉栓" },
            { "mod_mouse_aimbot", "鼠标吸附" },
            { "set_aimbot_radius", "吸附范围" },
            { "mod_killaura", "杀戮光环" },
            { "set_ka_players", "攻击玩家" },
            { "set_ka_aps", "攻击速度" },
            { "set_ka_range", "攻击范围" },
            { "set_ka_render_target", "目标渲染" },
            { "set_ka_color", "目标颜色" },

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

            // Movement modules - speed
            { "mod_speed_modifier", "速度修改" },
            { "set_speed_mult", "速度倍率" },
            { "mod_anti_weight", "反负重" },
            { "mod_airjump", "空气跳跃" },
            { "mod_jetpack", "喷气背包" },

            // World modules
            { "mod_autounlock", "秒开锁" },

            // Misc modules
            { "mod_autotranslate", "聊天机翻" },
            { "set_trans_source", "源语言" },
            { "set_trans_target", "目标语言" },
            { "set_two_way_trans", "开启双向外发翻译" },
            { "mod_iq250", "万事通模式" },
            { "mod_clickgui", "菜单设置" },
            { "mod_language", "Language" },
            { "mod_antiragdoll", "反布娃娃" },
            { "mod_fetch_macro", "捡取宏" },
            { "set_fetch_distance", "拾取距离" },
            { "mod_explosives_macro", "一键引爆" },
            { "mod_human_boombox", "人形音响" },
            { "set_boombox_track", "当前曲目" },

            // Render modules - throw trajectory
            { "mod_trajectory", "投掷抛物线" },

            // Settings labels
            { "set_intensity", "强度" },
            { "set_camera_size", "视距大小" },
            { "set_vision_radius", "光照范围" },
            { "set_jump_force", "跳跃力度" },
            { "set_item_color", "物品颜色" },
            { "set_creature_color", "生物颜色" },
            { "set_trap_color", "陷阱颜色" },
            { "set_trajectory_color", "轨迹颜色" },
            { "set_trajectory_length", "最大距离" },

            // Tab labels
            { "tab_modules", "功能模块" },
            { "tab_globalsettings", "全局设置" },

            // Global Settings - Grid Snapping section
            { "gs_section_grid", "--- 网格吸附 ---" },
            { "gs_enable_grid", "启用网格吸附" },
            { "gs_grid_size", "网格大小" },

            // Global Settings - Minimap section
            { "gs_section_minimap", "--- 小地图 ---" },
            { "gs_enable_minimap", "启用小地图" },
            { "gs_minimap_size", "尺寸" },
            { "gs_minimap_radius", "视野范围" },

            // Global Settings - Limb HUD section
            { "gs_section_limb", "--- 肢体状态 HUD ---" },
            { "gs_enable_limb", "启用肢体 HUD" },
            { "gs_limb_scale", "缩放比例" },
            { "gs_limb_pos_x", "X 轴偏移" },
            { "gs_limb_pos_y", "Y 轴偏移" },

            // Global Settings - Logo Overlay section
            { "gs_section_logo", "--- Logo 水印 ---" },
            { "gs_enable_logo", "显示 Logo" },
            { "gs_logo_height", "Logo 大小" },

            // Unit suffixes (appended to slider values)
            { "unit_px", "px" },
            { "unit_m", "m" },
            { "unit_x", "x" },
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

            // Combat modules
            { "mod_rapid_fire", "Custom Fire Rate" },
            { "mod_no_jam", "Anti-Jam" },
            { "set_fire_rate_mult", "Multiplier" },
            { "mod_no_recoil", "No Recoil" },
            { "mod_auto_reload", "Auto Reload" },
            { "mod_auto_bolt", "Auto Bolt-Action" },
            { "mod_mouse_aimbot", "Mouse Magnet" },
            { "set_aimbot_radius", "Magnet Radius" },
            { "mod_killaura", "Kill Aura" },
            { "set_ka_players", "Attack Players" },
            { "set_ka_aps", "Attack Speed" },
            { "set_ka_range", "Attack Range" },
            { "set_ka_render_target", "Render Target" },
            { "set_ka_color", "Target Color" },

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

            // Render modules - throw trajectory
            { "mod_trajectory", "Throw Trajectory" },

            // Movement modules
            { "mod_flight", "Flight" },
            { "mod_noclip", "No-Clip" },
            { "mod_jumpboost", "Jump Boost" },

            // Movement modules - speed
            { "mod_speed_modifier", "Speed Modifier" },
            { "set_speed_mult", "Speed Multiplier" },
            { "mod_anti_weight", "Anti-Weight" },
            { "mod_airjump", "Air Jump" },
            { "mod_jetpack", "Jetpack" },

            // World modules
            { "mod_autounlock", "Auto Unlock" },

            // Misc modules
            { "mod_autotranslate", "Auto Translate" },
            { "set_trans_source", "Source Lang" },
            { "set_trans_target", "Target Lang" },
            { "set_two_way_trans", "Enable Outgoing Translation" },
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
            { "set_trajectory_color", "Trajectory Color" },
            { "set_trajectory_length", "Max Length" },

            // Tab labels
            { "tab_modules", "Modules" },
            { "tab_globalsettings", "Global Settings" },

            // Global Settings - Grid Snapping section
            { "gs_section_grid", "--- Grid Snapping ---" },
            { "gs_enable_grid", "Enable Grid Snap" },
            { "gs_grid_size", "Grid Size" },

            // Global Settings - Minimap section
            { "gs_section_minimap", "--- Minimap ---" },
            { "gs_enable_minimap", "Enable Minimap" },
            { "gs_minimap_size", "Size" },
            { "gs_minimap_radius", "View Radius" },

            // Global Settings - Limb HUD section
            { "gs_section_limb", "--- Limb Status HUD ---" },
            { "gs_enable_limb", "Enable Limb HUD" },
            { "gs_limb_scale", "Scale" },
            { "gs_limb_pos_x", "X Offset" },
            { "gs_limb_pos_y", "Y Offset" },

            // Global Settings - Logo Overlay section
            { "gs_section_logo", "--- Logo ---" },
            { "gs_enable_logo", "Show Logo" },
            { "gs_logo_height", "Logo Size" },

            // Unit suffixes
            { "unit_px", "px" },
            { "unit_m", "m" },
            { "unit_x", "x" },
        };

        public static string Get(string key)
        {
            var currentDict = Settings.CurrentLanguage == AppLanguage.English ? _en : _zh;
            if (currentDict.TryGetValue(key, out string value)) return value;
            return key;
        }
    }
}
