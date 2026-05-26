using HarmonyLib;
using UnityEngine;
using FullBrightMod.Core;
using KrokoshaCasualtiesMP;
using System.Collections.Generic;

namespace FullBrightMod.Patches
{
    // ==========================================
    // 🔫 枪械核心数据深度劫持 (基础作弊与拉栓)
    // ==========================================
    [HarmonyPatch(typeof(GunScript), "Update")]
    internal static class GunScriptUpdatePatch
    {
        [HarmonyPrefix]
        private static void Prefix(GunScript __instance)
        {
            if (__instance == null) return;

            // 1. 无限子弹
            if (Settings.IsInfiniteAmmoEnabled)
            {
                __instance.roundInChamber = GunScript.RoundInChamber.Round;
                if (__instance.feedType == GunScript.FeedType.Mag && !__instance.hasMag)
                {
                    __instance.hasMag = true;
                    __instance.roundsInMag = __instance.magCapacity;
                }
                else if (__instance.roundsInMag < __instance.magCapacity)
                {
                    __instance.roundsInMag = __instance.magCapacity;
                }
            }

            // 2. 无散布
            if (Settings.IsNoSpreadEnabled)
            {
                __instance.verticalSpread = 0f;
            }

            // 3. 自定义射速
            if (Settings.IsRapidFireEnabled && Settings.CustomFireRateMultiplier > 1f)
            {
                __instance.firingMode = GunScript.FiringMode.Auto; 
                var trav = Traverse.Create(__instance);
                float currentGasTime = trav.Field("gasTime").GetValue<float>();
                if (currentGasTime > 0f)
                {
                    float extraCooldown = Time.deltaTime * (Settings.CustomFireRateMultiplier - 1f);
                    trav.Field("gasTime").SetValue(currentGasTime - extraCooldown);
                }
                if (__instance.triggerPressed) __instance.firingPinStruck = false;
            }

            // 4. 自动拉栓 (完美两段物理模拟)
            if (Settings.IsAutoBoltEnabled)
            {
                if (__instance.roundInChamber == GunScript.RoundInChamber.Casing)
                {
                    __instance.racked = true;
                    __instance.lastRacked = false; 
                }
                else if (__instance.racked && __instance.roundInChamber == GunScript.RoundInChamber.None && __instance.roundsInMag > 0)
                {
                    __instance.racked = false;
                    __instance.lastRacked = true; 
                }
            }
        }
    }

    // ==========================================
    // 💥 无后座力补丁 (彻底封杀原版与联机反冲)
    // ==========================================
    [HarmonyPatch(typeof(GunScript), "Fire")]
    internal static class GunScriptFireRecoilPatch
    {
        [HarmonyPrefix]
        private static void Prefix(GunScript __instance)
        {
            if (Settings.IsNoRecoilEnabled && __instance != null) __instance.knockBack = 0f;
        }
    }

    [HarmonyPatch(typeof(KrokoshaGunScriptTrackerComponent), "ApplyRecoil")]
    internal static class NoRecoilMPPatch
    {
        [HarmonyPrefix]
        private static bool Prefix() => !Settings.IsNoRecoilEnabled; 
    }

    [HarmonyPatch(typeof(GunScript), "JamChance")]
    internal static class GunScriptJamChancePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref float __result)
        {
            if (Settings.IsNoJamEnabled) __result = 0f;
        }
    }

    // ==========================================
    // 🎒 战斗总控：自瞄 + 完美合法装弹宏
    // ==========================================
    [HarmonyPatch(typeof(Body), "Update")]
    internal static class CombatBodyUpdatePatch
    {
        private static float _reloadTimer = 0f;

        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;

            // ===============================================
            // 🎯 鼠标吸附自瞄 (绕过 Clamp 截断)
            // ===============================================
            if (Settings.IsMouseAimbotEnabled)
            {
                Camera cam = PlayerCamera.main.GetComponent<Camera>() ?? Camera.main;
                if (cam != null)
                {
                    Vector3 mousePos = Input.mousePosition;
                    BuildingEntity bestTarget = null;
                    float minDistance = Settings.AimbotRadius;

                    // 【性能优化】：使用你的 ESPCache.CachedEntities 替代每帧的 FindObjectsOfType，大幅消除卡顿！
                    var entities = Utils.ESPCache.CachedEntities;
                    if (entities != null)
                    {
                        foreach (BuildingEntity entity in entities)
                        {
                            // ✨【核心修复】：新增 !entity.animal 过滤条件！
                            // 这样就只会锁定玩家、怪物、虫子等活物，彻底忽略门、箱子、建筑物和掉落物。
                            if (entity == null || !entity.animal || entity.health < 0.5f || entity.transform == __instance.transform) continue;

                            Vector3 screenPos = cam.WorldToScreenPoint(entity.transform.position);
                            if (screenPos.z <= 0) continue; 

                            float dist = Vector2.Distance(mousePos, screenPos);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                bestTarget = entity;
                            }
                        }
                    }

                    if (bestTarget != null)
                    {
                        // 强制改写玩家瞄准坐标！加 0.5f 瞄准躯干
                        __instance.targetLookPos = bestTarget.transform.position + Vector3.up * 0.5f;
                    }
                }
            }

            // ===============================================
            // 🔄 智能自动装弹 (直接调用底层 CombineItems 模拟真实拖放)
            // ===============================================
            if (Settings.IsAutoReloadEnabled)
            {
                _reloadTimer += Time.deltaTime;
                if (_reloadTimer < 0.25f) return; // 0.25秒冷却，给网络同步发包留足时间
                _reloadTimer = 0f;

                Item gunItem = __instance.GetItem(__instance.handSlot);
                if (gunItem == null) return;
                GunScript gun = gunItem.GetComponent<GunScript>();
                if (gun == null) return;

                List<Item> invItems = __instance.GetAllItemsThorough();
                if (invItems == null) return;

                bool didCombineThisFrame = false; // 限制每帧只进行一次合并动作，防止死循环或网络阻塞

                // 【优先动作：自动压弹】
                foreach (Item item1 in invItems)
                {
                    AmmoScript mag = item1.GetComponent<AmmoScript>();
                    if (mag != null && mag.itemType == AmmoScript.AmmoItemType.Magazine && mag.rounds < mag.maxRounds)
                    {
                        foreach (Item item2 in invItems)
                        {
                            if (item1 == item2) continue; // 防止自己跟自己合并
                            AmmoScript round = item2.GetComponent<AmmoScript>();
                            if (round != null && round.itemType == AmmoScript.AmmoItemType.Round && round.ammoType == mag.ammoType)
                            {
                                // 完美模拟：把子弹(item2)拖动到弹匣(item1)上
                                __instance.CombineItems(item1, item2);
                                didCombineThisFrame = true;
                                break;
                            }
                        }
                    }
                    if (didCombineThisFrame) break;
                }

                // 如果这一帧压了子弹，等下一帧再装枪，确保动画和网络包顺序执行
                if (didCombineThisFrame) return;

                // 【装枪动作】
                if (gun.roundInChamber == GunScript.RoundInChamber.None || gun.roundsInMag < gun.magCapacity)
                {
                    Item bestAmmoItem = null;

                    if (gun.feedType == GunScript.FeedType.Mag)
                    {
                        // 找子弹最多的备用弹匣
                        int maxRounds = -1;
                        foreach (Item item in invItems)
                        {
                            AmmoScript mag = item.GetComponent<AmmoScript>();
                            if (mag != null && mag.itemType == AmmoScript.AmmoItemType.Magazine && mag.ammoType == gun.ammoType && mag.rounds > maxRounds)
                            {
                                if (mag.transform != gun.transform)
                                {
                                    maxRounds = mag.rounds;
                                    bestAmmoItem = item;
                                }
                            }
                        }

                        if (bestAmmoItem != null)
                        {
                            // 换弹匣前，先规规矩矩地把原弹匣退下来
                            if (gun.hasMag) gun.UnloadMag();
                            
                            // 完美模拟：把新弹匣(bestAmmoItem)拖动到枪(gunItem)上
                            __instance.CombineItems(gunItem, bestAmmoItem);
                        }
                    }
                    else
                    {
                        // 霰弹枪直插
                        foreach (Item item in invItems)
                        {
                            AmmoScript round = item.GetComponent<AmmoScript>();
                            if (round != null && round.itemType == AmmoScript.AmmoItemType.Round && round.ammoType == gun.ammoType)
                            {
                                bestAmmoItem = item;
                                break;
                            }
                        }

                        if (bestAmmoItem != null)
                        {
                            // 完美模拟：把霰弹(bestAmmoItem)拖动到枪(gunItem)上
                            __instance.CombineItems(gunItem, bestAmmoItem);
                        }
                    }
                }
            }
        }
    }
}