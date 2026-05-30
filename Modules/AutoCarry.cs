using System.Collections;
using FullBrightMod.Core;
using FullBrightMod.Patches;
using FullBrightMod.Utils;
using KrokoshaCasualtiesMP;
using UnityEngine;

namespace FullBrightMod.Modules.Player
{
    public class AutoCarry : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_autocarry");
        public override ModuleCategory Category => ModuleCategory.Player;
        public override void OnEnable()  => Settings.IsAutoCarryEnabled = true;
        public override void OnDisable() => Settings.IsAutoCarryEnabled = false;

        private bool _coroutineRunning;
        private const float MouseWorldRadius = 2.5f; // 鼠标世界坐标周围容错半径

        public override void OnUpdate()
        {
            if (!Input.GetMouseButtonDown(1) || _coroutineRunning) return;

            Body targetBody = FindClosestPlayerToMouse();
            if (targetBody == null) return;

            NetBody myNetBody = NetPlayer.LOCAL_PLAYER?.playerbody;
            NetBody targetNetBody = targetBody.GetComponent<NetBody>();
            if (myNetBody == null || targetNetBody == null) return;

            _coroutineRunning = true;
            FullBrightPlugin.Instance.StartCoroutine(CarryCoroutine(myNetBody, targetNetBody));
        }

        private IEnumerator CarryCoroutine(NetBody myNetBody, NetBody targetNetBody)
        {
            Body myBody = myNetBody.body;
            Body targetBody = targetNetBody.body;
            if (myBody == null || targetBody == null)
            { _coroutineRunning = false; yield break; }

            Vector3 originalPos = myBody.transform.position;
            Vector3 targetPos = targetBody.transform.position;
            float dist = Vector2.Distance(myBody.transform.position, targetPos);
            bool didBlink = false;
            bool hasError = false;

            if (dist > Settings.CustomPickupRange)
            {
                Vector3 safePos = TPHelper.GetSafeTeleportPosition(
                    myBody.transform.position, targetPos, 1.5f);
                myBody.transform.position = safePos;
                try { ClientMain.Client_SendCharacterSyncPacket(); } catch { }
                didBlink = true;
                yield return new WaitForSeconds(0.15f);
            }

            try { myNetBody.Push(targetNetBody); }
            catch { hasError = true; }

            if (hasError)
            {
                if (didBlink) { myBody.transform.position = originalPos; }
                _coroutineRunning = false;
                yield break;
            }

            yield return new WaitForSeconds(0.5f);

            try
            {
                if (targetNetBody.StartPiggyback(myNetBody, check_distance: false, force: true))
                    KrokoshaScavMultiplayer.Client_SendSimpleMessageToServer((ushort)30033, targetNetBody.netId);
            }
            catch { }

            yield return new WaitForSeconds(0.8f);

            if (didBlink)
            {
                myBody.transform.position = originalPos;
                try { ClientMain.Client_SendCharacterSyncPacket(); } catch { }
            }

            _coroutineRunning = false;
        }

        /// <summary>
        /// 基于鼠标指针所在的世界坐标寻找最近的合法玩家目标。
        /// 使用 ScreenToWorldPoint 转换，绕开屏幕准星判断。
        /// </summary>
        private Body FindClosestPlayerToMouse()
        {
            var players = ESPCache.CachedPlayers;
            if (players == null) return null;

            Camera cam = Camera.main;
            if (cam == null) return null;

            // 鼠标指针 → 世界坐标
            Vector2 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            float radiusSqr = MouseWorldRadius * MouseWorldRadius;

            Body best = null;
            float bestDist = float.MaxValue;

            foreach (Body body in players)
            {
                if (body == null || !body || !body.alive) continue;

                float dSqr = ((Vector2)body.transform.position - mouseWorldPos).sqrMagnitude;
                if (dSqr > radiusSqr) continue;

                if (dSqr < bestDist)
                {
                    bestDist = dSqr;
                    best = body;
                }
            }

            return best;
        }
    }
}
