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
            { "set_interact_distance", "交互距离" },
            { "mod_no_recoil", "无后座" },
            { "mod_auto_reload", "自动装弹" },
            { "mod_auto_bolt", "自动拉栓" },
            { "mod_mouse_aimbot", "鼠标吸附" },
            { "set_aimbot_radius", "吸附范围" },
            { "mod_killaura", "杀戮光环" },
            { "set_ka_players", "攻击玩家" },
            { "set_ka_creatures", "攻击生物" },
            { "set_ka_glass", "攻击玻璃碴" },
            { "set_ka_aps", "攻击速度" },
            { "set_ka_range", "攻击范围" },
            { "set_ka_render_target", "目标渲染" },
            { "set_ka_color", "目标颜色" },
            { "set_ka_tp_attack", "传送攻击" },
            { "set_ka_tp_range", "传送最大范围" },

            // Player modules
            { "mod_autobandage", "绷带高手" },
            { "mod_freecam", "灵魂出窍" },
            { "mod_camerateleport", "相机传送" },
            { "mod_longhands", "长手模式" },
            { "mod_throughwall", "隔墙取物" },
            { "mod_shrapnel_maker", "破片制造者" },
            { "mod_instant_amputation", "秒截肢" },
            { "mod_instant_shrapnel", "秒拔破片" },
            { "mod_auto_dislocation", "正骨大师" },
            { "mod_auto_generator", "自动发电" },
            { "mod_anti_solar_flare", "反耀斑" },

            // Render modules
            { "mod_fullbright", "全亮模式" },
            { "mod_itemesp", "物品透视" },
            { "mod_creatureesp", "生物透视" },
            { "mod_trapesp", "陷阱警告" },
            { "mod_playeresp", "玩家透视" },
            { "set_esp_wireframe", "线框模式" },
            { "set_esp_line_width", "线条粗细" },
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

            // Player modules - CPR
            { "mod_instant_revive", "完美复苏" },
            { "mod_rib_crusher", "肋骨粉碎者" },

            // Player modules - 自动推人 / 强制背人
            { "mod_autopush", "自动推人" },
            { "mod_autocarry", "强制背人" },
            { "set_autopush_distance", "推人距离" },
            { "set_longhands_tp", "TP 模式 (突破服务端距离限制)" },

            // Player modules - Auto Replenish
            { "mod_auto_replenish", "自动补货" },

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
            { "mod_inventory_sort", "背包整理" },
            { "mod_fetch_macro", "捡取宏" },
            { "set_fetch_distance", "拾取距离" },
            { "mod_explosives_macro", "一键引爆" },
            { "mod_human_boombox", "人形音响" },
            { "mod_stt", "语音转文字" },
            { "mod_anti_screen_fx", "反屏幕特效" },
            { "mod_melee_speed", "攻速调整" },
            { "set_melee_speed", "攻速倍率" },
            { "mod_remote_pickup", "远程拾取" },
            { "set_pickup_range", "拾取范围" },
            { "mod_container_esp", "箱子指示器" },
            { "set_container_range", "探测范围" },
            { "set_container_color", "箱子颜色" },
            { "container_esp_mode", "渲染模式" },
            { "mode_lines", "线条" },
            { "mode_arrows", "箭头" },

            // SoundCannon Indicator
            { "mod_sound_cannon_indicator", "声波炮指示器" },

            // Chameleon module
            { "mod_chameleon", "变色龙" },
            { "set_chameleon_rainbow", "彩虹模式" },
            { "set_rainbow_speed", "彩虹速度" },
            { "set_chameleon_glow", "发光效果" },
            { "set_chameleon_intensity", "发光强度" },
            { "set_chameleon_color", "部位颜色" },
            { "cham_part_head", "头部" },
            { "cham_part_eyes", "眼睛" },
            { "cham_part_torso", "躯干" },
            { "cham_part_arms", "手臂" },
            { "cham_part_legs", "腿部" },
            { "cham_part_tail", "尾巴" },
            { "cham_dye_head", "头部染色" },
            { "cham_dye_eyes", "眼睛染色" },
            { "cham_dye_torso", "躯干染色" },
            { "cham_dye_arms", "手臂染色" },
            { "cham_dye_legs", "腿部染色" },
            { "cham_dye_tail", "尾巴染色" },

            // KeyMapper module
            { "mod_keymapper", "按键映射" },
            { "km_source_key", "源按键" },
            { "km_target_key", "目标按键" },
            { "km_press_key", "[按下按键...]" },
            { "km_bidirectional", "双向映射" },
            { "km_delete", "删除" },
            { "km_add_mapping", "+ 添加映射" },

            // Color Picker UI
            { "ui_color_palette", "调色板" },
            { "ui_color_edit", "颜色编辑" },
            { "ui_cancel", "取消" },
            { "ui_confirm", "确定" },

            // Anti-Screen FX toggles
            { "fx_blood", "流血/疼痛红屏" },
            { "fx_temperature", "温度异常特效" },
            { "fx_mental", "精神异常特效" },
            { "fx_blur", "模糊/饥饿特效" },
            { "fx_radiation", "辐射噪点特效" },
            { "fx_damage", "损伤视觉遮罩" },
            { "fx_ui_overlays", "UI 覆盖层" },

            // Inventory sort modules
            { "mod_autosort_space", "整理背包 (空间优先)" },
            { "mod_autosort_weight", "整理背包 (负重优先)" },
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
            { "set_player_color", "玩家颜色" },
            { "set_trajectory_color", "轨迹颜色" },
            { "set_trajectory_length", "最大距离" },

            // Tab labels
            { "tab_modules", "功能模块" },
            { "tab_globalsettings", "全局设置" },
            { "tab_credits", "关于/鸣谢" },

            // Credits tab labels (ZH)
            { "credits_developer", "开发者" },
            { "credits_author_desc", "CheaterMod源作者" },
            { "credits_darnmik_desc", "为CheaterMod添加了一些实用的功能" },
            { "credits_thanks", "特别鸣谢" },
            { "credits_wanxi", "晚夕睡不醒：为模组的开发出谋划策并进行漏洞反馈" },
            { "credits_yumo", "是是是钰墨：多次帮助测试模组在多人游戏中的表现" },
            { "credits_andian", "四十毫克胺碘酮：多次报告该模组的BUG并提供建议" },
            { "credits_about", "关于" },
            { "credits_author_account", "作者账号: B站Dylanvip2024" },
            { "credits_source_url", "开源地址: https://github.com/Dylanvip2024/CheaterMod/" },

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
            { "gs_mod_radar", "开启CM使用者识别" },

            // Unit suffixes (appended to slider values)
            { "unit_px", "px" },
            { "unit_m", "m" },
            { "unit_x", "x" },

            // Global Settings - FeatureList
            { "gs_section_featurelist", "--- 开启功能列表 ---" },
            { "gs_enable_featurelist", "显示功能列表" },
            { "gs_fl_align", "左对齐" },
            { "gs_fl_fontsize", "字体大小" },
            { "gs_fl_color", "文本颜色" },
            { "gs_fl_rainbow", "彩虹模式" },
            { "gs_fl_bg", "背景底框" },
            { "gs_fl_accent", "边缘强调线" },

            // Global Settings - UI Sound / Feedback
            { "gs_section_feedback", "--- UI 反馈 ---" },
            { "gs_enable_toggle_sound", "模块开关提示音" },
            { "gs_enable_notifications", "模块状态提示通知" },
            { "gs_notif_y", "通知Y轴位置" },
            { "gs_notif_right", "从右侧弹出" },

            // TroublemakerAura module
            { "mod_troublemaker_aura", "闯祸光环" },
            { "set_ta_range", "光环范围" },
            { "set_ta_target_mode", "目标模式" },
            { "ta_mode_nearest", "最近玩家" },
            { "ta_mode_all", "所有范围内玩家" },
            { "set_ta_shrapnel", "破片制造" },
            { "set_ta_ribcrusher", "肋骨粉碎" },
            { "set_ta_push", "推倒" },
            { "set_ta_amputation", "截肢" },
            { "set_ta_burn", "激光灼烧" },
            { "set_ka_headshot", "优先锁头 (仅玩家)" },
            { "set_ka_indicator", "目标指示器样式" },
            { "mod_pickup_aura", "拾取光环" },
            { "set_pickup_filter", "过滤低价值物品" },
            { "set_pickup_tp", "传送捡取" },
            { "set_pickup_tp_range", "传送捡取范围" },
            { "mod_anti_spikes", "反地刺" },
            { "set_flight_speed", "飞行速度" },
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
            { "set_interact_distance", "Reach Distance" },
            { "mod_no_recoil", "No Recoil" },
            { "mod_auto_reload", "Auto Reload" },
            { "mod_auto_bolt", "Auto Bolt-Action" },
            { "mod_mouse_aimbot", "Mouse Magnet" },
            { "set_aimbot_radius", "Magnet Radius" },
            { "mod_killaura", "Kill Aura" },
            { "set_ka_players", "Attack Players" },
            { "set_ka_creatures", "Attack Creatures" },
            { "set_ka_glass", "Attack Glass" },
            { "set_ka_aps", "Attack Speed" },
            { "set_ka_range", "Attack Range" },
            { "set_ka_render_target", "Render Target" },
            { "set_ka_color", "Target Color" },
            { "set_ka_tp_attack", "Teleport Attack" },
            { "set_ka_tp_range", "Teleport Max Range" },

            // Player modules
            { "mod_autobandage", "Auto Bandage" },
            { "mod_freecam", "Free Camera" },
            { "mod_camerateleport", "Camera Teleport" },
            { "mod_longhands", "Extended Reach" },
            { "mod_throughwall", "Through Wall" },
            { "mod_shrapnel_maker", "Shrapnel Maker" },
            { "mod_instant_amputation", "Instant Amputation" },
            { "mod_instant_shrapnel", "Instant Shrapnel Removal" },
            { "mod_auto_dislocation", "Auto Bone Setter" },
            { "mod_auto_generator", "Auto Generator" },
            { "mod_anti_solar_flare", "Anti-Solar Flare" },

            // Render modules
            { "mod_fullbright", "Full Luminance" },
            { "mod_itemesp", "Item ESP" },
            { "mod_creatureesp", "Creature ESP" },
            { "mod_trapesp", "Trap ESP" },
            { "mod_playeresp", "Player ESP" },
            { "set_esp_wireframe", "Wireframe" },
            { "set_esp_line_width", "Line Width" },
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

            // Player modules - CPR
            { "mod_instant_revive", "Perfect CPR" },
            { "mod_rib_crusher", "Rib Crusher" },

            // Player modules - Auto Push / Auto Carry
            { "mod_autopush", "Auto Push" },
            { "mod_autocarry", "Auto Carry" },
            { "set_autopush_distance", "Push Distance" },
            { "set_longhands_tp", "Blink Mode (Bypass Server Distance)" },

            // Player modules - Auto Replenish
            { "mod_auto_replenish", "Auto Replenish" },

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
            { "mod_inventory_sort", "Inventory Sort" },
            { "mod_fetch_macro", "Fetch Macro" },
            { "set_fetch_distance", "Fetch Distance" },
            { "mod_explosives_macro", "Ignite Explosives Macro" },
            { "mod_human_boombox", "Human Boombox" },
            { "mod_stt", "Voice to Text" },
            { "mod_anti_screen_fx", "Anti Screen FX" },
            { "mod_melee_speed", "Melee Speed" },
            { "set_melee_speed", "Speed Multiplier" },
            { "mod_remote_pickup", "Remote Pickup" },
            { "set_pickup_range", "Pickup Range" },
            { "mod_container_esp", "Container ESP" },
            { "set_container_range", "Scan Range" },
            { "set_container_color", "Container Color" },
            { "container_esp_mode", "Render Mode" },
            { "mode_lines", "Lines" },
            { "mode_arrows", "Arrows" },

            // SoundCannon Indicator
            { "mod_sound_cannon_indicator", "Sound Cannon Indicator" },

            // Chameleon module
            { "mod_chameleon", "Chameleon" },
            { "set_chameleon_rainbow", "Rainbow Mode" },
            { "set_rainbow_speed", "Rainbow Speed" },
            { "set_chameleon_glow", "Glow Effect" },
            { "set_chameleon_intensity", "Glow Intensity" },
            { "set_chameleon_color", "Part Color" },
            { "cham_part_head", "Head" },
            { "cham_part_eyes", "Eyes" },
            { "cham_part_torso", "Torso" },
            { "cham_part_arms", "Arms" },
            { "cham_part_legs", "Legs" },
            { "cham_part_tail", "Tail" },
            { "cham_dye_head", "Dye Head" },
            { "cham_dye_eyes", "Dye Eyes" },
            { "cham_dye_torso", "Dye Torso" },
            { "cham_dye_arms", "Dye Arms" },
            { "cham_dye_legs", "Dye Legs" },
            { "cham_dye_tail", "Dye Tail" },

            // KeyMapper module
            { "mod_keymapper", "Key Mapper" },
            { "km_source_key", "Source" },
            { "km_target_key", "Target" },
            { "km_press_key", "[Press Key...]" },
            { "km_bidirectional", "Bidirectional" },
            { "km_delete", "Delete" },
            { "km_add_mapping", "+ Add Mapping" },

            // Color Picker UI
            { "ui_color_palette", "Palette" },
            { "ui_color_edit", "Color Edit" },
            { "ui_cancel", "Cancel" },
            { "ui_confirm", "Confirm" },

            // Anti-Screen FX toggles
            { "fx_blood", "Blood & Pain" },
            { "fx_temperature", "Temperature FX" },
            { "fx_mental", "Mental FX" },
            { "fx_blur", "Blur & Sickness" },
            { "fx_radiation", "Radiation Noise" },
            { "fx_damage", "Damage Overlay" },
            { "fx_ui_overlays", "UI Overlays" },

            // Inventory sort modules
            { "mod_autosort_space", "Auto Sort (Space)" },
            { "mod_autosort_weight", "Auto Sort (Weight)" },
            { "set_boombox_track", "Current Track" },

            // Settings labels
            { "set_intensity", "Intensity" },
            { "set_camera_size", "Camera Size" },
            { "set_vision_radius", "Vision Radius" },
            { "set_jump_force", "Jump Force" },
            { "set_item_color", "Item Color" },
            { "set_creature_color", "Creature Color" },
            { "set_trap_color", "Trap Color" },
            { "set_player_color", "Player Color" },
            { "set_trajectory_color", "Trajectory Color" },
            { "set_trajectory_length", "Max Length" },

            // Tab labels
            { "tab_modules", "Modules" },
            { "tab_globalsettings", "Global Settings" },
            { "tab_credits", "About / Credits" },

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
            { "gs_mod_radar", "CM User Detection" },

            // Unit suffixes
            { "unit_px", "px" },
            { "unit_m", "m" },
            { "unit_x", "x" },

            // Global Settings - FeatureList
            { "gs_section_featurelist", "--- Active Modules ---" },
            { "gs_enable_featurelist", "Show Feature List" },
            { "gs_fl_align", "Left Align" },
            { "gs_fl_fontsize", "Font Size" },
            { "gs_fl_color", "Text Color" },
            { "gs_fl_rainbow", "Rainbow Mode" },
            { "gs_fl_bg", "Background Box" },
            { "gs_fl_accent", "Accent Line" },

            // Global Settings - UI Sound / Feedback
            { "gs_section_feedback", "--- UI Feedback ---" },
            { "gs_enable_toggle_sound", "Toggle Sound Effect" },
            { "gs_enable_notifications", "Toast Notifications" },
            { "gs_notif_y", "Notification Y Pos" },
            { "gs_notif_right", "Slide From Right" },

            // TroublemakerAura module
            { "mod_troublemaker_aura", "Troublemaker Aura" },
            { "set_ta_range", "Aura Range" },
            { "set_ta_target_mode", "Target Mode" },
            { "ta_mode_nearest", "Nearest Player" },
            { "ta_mode_all", "All In Range" },
            { "set_ta_shrapnel", "Shrapnel Maker" },
            { "set_ta_ribcrusher", "Rib Crusher" },
            { "set_ta_push", "Push" },
            { "set_ta_amputation", "Amputation" },
            { "set_ta_burn", "Laser Burn" },
            { "set_ka_headshot", "Prioritize Head (Players)" },
            { "set_ka_indicator", "Target Indicator Style" },
            { "mod_pickup_aura", "Pickup Aura" },
            { "set_pickup_filter", "Filter Low Value Items" },
            { "set_pickup_tp", "Teleport Pickup" },
            { "set_pickup_tp_range", "Teleport Pickup Range" },
            { "mod_anti_spikes", "Anti-Spikes" },
            { "set_flight_speed", "Flight Speed" },
        };

        public static string Get(string key)
        {
            var currentDict = ModSettings.CurrentLanguage == AppLanguage.English ? _en : _zh;
            if (currentDict.TryGetValue(key, out string value)) return value;
            return key;
        }
    }
}
