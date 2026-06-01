using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using KrokoshaCasualtiesMP;
using FullBrightMod.Core;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Patches
{
    /// <summary>
    /// 进入 Steam 大厅后广播 CM使用者 标识（_chk_mod=1）
    /// </summary>
    [HarmonyPatch(typeof(KSteam), "OnLobbyEnter_APIDispatch")]
    internal static class ModRadarBroadcastPatch
    {
        [HarmonyPostfix]
        private static void Postfix(LobbyEnter_t pCallback, bool bIOFailure)
        {
            if (bIOFailure) return;
            if (!GlobalSettings.EnableModRadar) return;

            try
            {
                CSteamID lobbyId = (CSteamID)pCallback.m_ulSteamIDLobby;
                SteamMatchmaking.SetLobbyMemberData(lobbyId, "_chk_mod", "1");
                Debug.Log("[CheaterMod] 已向 Steam 大厅广播CM标识。");
            }
            catch { }
        }
    }

    /// <summary>
    /// CM用户加入时在聊天栏用青色提示
    /// </summary>
    [HarmonyPatch(typeof(NetPlayer), "Start")]
    internal static class ModRadarJoinNotifyPatch
    {
        private static readonly HashSet<ulong> _notified = new HashSet<ulong>();

        [HarmonyPostfix]
        private static void Postfix(NetPlayer __instance)
        {
            if (!GlobalSettings.EnableModRadar) return;
            if (__instance.is_local) return;

            ulong steamId = __instance.steam_id;
            if (steamId == 0 || _notified.Contains(steamId)) return;

            try
            {
                if (ModRadarHelper.IsModUser(steamId))
                {
                    _notified.Add(steamId);
                    string name = __instance.playername ?? "???";
                    Chat.LogMessage("", $"<color=#00FFFF>[CM用户] {name} 加入了游戏</color>", true);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// UI 渲染时注入 [CM用户] 前缀 — 通过临时修改 playername 字段实现
    /// 覆盖: 侧边栏玩家列表、屏幕外箭头、睡眠列表、交互菜单、世界名牌
    /// </summary>
    [HarmonyPatch(typeof(UIMainMenu), "_GUI__PlrListDrawPlr")]
    internal static class PlayerListDisplayPatch
    {
        private static readonly Dictionary<NetPlayer, string> _backup = new Dictionary<NetPlayer, string>();

        [HarmonyPrefix]
        private static void Prefix(NetPlayer plr)
        {
            if (!GlobalSettings.EnableModRadar) return;
            if (plr == null || plr.steam_id == 0) return;
            try
            {
                if (ModRadarHelper.IsModUser(plr.steam_id)
                    && !_backup.ContainsKey(plr)
                    && !plr.playername.StartsWith("[CM]"))
                {
                    _backup[plr] = plr.playername;
                    plr.playername = "[CM] " + plr.playername;
                }
            }
            catch { }
        }

        [HarmonyPostfix]
        private static void Postfix(NetPlayer plr)
        {
            if (_backup.TryGetValue(plr, out string original))
            {
                plr.playername = original;
                _backup.Remove(plr);
            }
        }
    }
}
