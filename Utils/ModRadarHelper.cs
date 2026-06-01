using Steamworks;
using KrokoshaCasualtiesMP;

namespace FullBrightMod.Utils
{
    public static class ModRadarHelper
    {
        /// <summary>
        /// 通过 Steam Lobby MemberData 检查玩家是否也是CM使用者。
        /// </summary>
        public static bool IsModUser(ulong steamId)
        {
            if (steamId == 0) return false;

            CSteamID lobbyId = KSteam.lobbyId;
            if (lobbyId.m_SteamID == 0) return false;

            try
            {
                string modFlag = SteamMatchmaking.GetLobbyMemberData(
                    lobbyId, (CSteamID)steamId, "_chk_mod");
                return modFlag == "1";
            }
            catch { return false; }
        }
    }
}
