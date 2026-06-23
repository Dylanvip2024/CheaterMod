using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// AntiSpikes（反地刺）—— 基于 Physics2D.IgnoreCollision 的零延迟地刺免疫。
    /// 
    /// 原理：
    ///   开启后遍历所有地刺，让本地玩家 Collider 与每个地刺的 Collider 互相忽略。
    ///   关闭时取消所有忽略——Unity 物理引擎在取消忽略的瞬间，若玩家仍在地刺内部，
    ///   会在下一帧强制重新触发 OnTriggerEnter2D，完美解决"站在地刺上关模块不受伤"的 Bug。
    /// 
    /// 与 Patches/CombatPatches.cs 中的 AntiSpikesTriggerPatch（Harmony Prefix 拦截
    /// OnTriggerEnter2D）互为兜底：IgnoreCollision 是物理层的"第一道防线"，
    /// Harmony Patch 是逻辑层的"最后防爆发"。
    /// </summary>
    public class AntiSpikes : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_anti_spikes") ?? "反地刺";
        public override ModuleCategory Category => ModuleCategory.Combat;

        /// <summary>已设置忽略的玩家-地刺碰撞体对（用于关闭时批量恢复）</summary>
        private readonly HashSet<Collider2D> _ignoredColliders = new HashSet<Collider2D>();

        /// <summary>主动扫描间隔（秒），只用于捕获新刷出的地刺</summary>
        private float _scanTimer;
        private const float SCAN_INTERVAL = 1.0f;

        public override void OnEnable()
        {
            ModSettings.IsAntiSpikesEnabled = true;
        }

        public override void OnDisable()
        {
            ModSettings.IsAntiSpikesEnabled = false;
            RestoreAllCollisions();
        }

        /// <summary>
        /// 每 SCAN_INTERVAL 秒扫描一次全局地刺缓存，对新增地刺设置 IgnoreCollision。
        /// IgnoreCollision 是持久化设置，只需对新出现的碰撞体调用一次。
        /// </summary>
        public override void OnUpdate()
        {
            if (!ModSettings.IsAntiSpikesEnabled) return;

            _scanTimer += Time.deltaTime;
            if (_scanTimer < SCAN_INTERVAL) return;
            _scanTimer = 0f;

            Collider2D playerCol = PlayerCamera.main?.body?.col;
            if (playerCol == null) return;

            var spikes = ESPCache.CachedSpikes;
            if (spikes == null) return;

            foreach (var spike in spikes)
            {
                if (spike == null || !spike) continue;

                Collider2D[] colliders = spike.GetComponentsInChildren<Collider2D>();
                foreach (var c in colliders)
                {
                    if (c == null || _ignoredColliders.Contains(c)) continue;

                    Physics2D.IgnoreCollision(playerCol, c, true);
                    _ignoredColliders.Add(c);
                }
            }
        }

        /// <summary>
        /// 关闭模块时，遍历所有已忽略的碰撞体，取消 IgnoreCollision。
        /// 取消忽略的瞬间，Unity 物理引擎若检测到玩家仍在地刺内部，
        /// 会在下一帧强制触发 OnTriggerEnter2D，从而消除状态粘连。
        /// </summary>
        private void RestoreAllCollisions()
        {
            Collider2D playerCol = PlayerCamera.main?.body?.col;

            foreach (var c in _ignoredColliders)
            {
                if (c != null && playerCol != null)
                {
                    try { Physics2D.IgnoreCollision(playerCol, c, false); }
                    catch { }
                }
            }
            _ignoredColliders.Clear();
        }
    }
}
