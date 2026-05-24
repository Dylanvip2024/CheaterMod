using HarmonyLib;
using UnityEngine;
using FullBrightMod.Core;
using System.Collections.Generic;

namespace FullBrightMod.Patches
{
    // ==========================================
    // ⚙️ 宏指令集中处理补丁
    // ==========================================
    [HarmonyPatch(typeof(Body), "Update")]
    internal static class MacroPatches
    {
        [HarmonyPostfix]
        private static void Postfix(Body __instance)
        {
            // 极其重要：确保只在本地玩家身上执行宏，防止控制了别的玩家或NPC
            if (PlayerCamera.main == null || PlayerCamera.main.body != __instance) return;

            // 1. 隔空取物逻辑
            if (Settings.IsFetchMacroEnabled)
            {
                ExecuteFetchMacro(__instance);
                Settings.IsFetchMacroEnabled = false; // 执行一次后立刻重置开关
            }

            // 2. 一键引爆逻辑
            if (Settings.IsExplosivesMacroEnabled)
            {
                ExecuteExplosivesMacro(__instance);
                Settings.IsExplosivesMacroEnabled = false; // 执行一次后立刻重置开关
            }
        }

        private static void ExecuteFetchMacro(Body body)
        {
            if (UnityEngine.Camera.main == null) return;

            // 屏幕坐标转世界坐标
            Vector2 mouseWorldPos = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // 物理重叠检测
            Collider2D col = Physics2D.OverlapPoint(mouseWorldPos);
            if (col != null)
            {
                Item item = col.GetComponent<Item>();
                if (item != null)
                {
                    float dist = Vector2.Distance(body.transform.position, item.transform.position);
                    if (dist <= Settings.FetchDistance)
                    {
                        body.AutoPickUpItem(item);
                        PlayerCamera.main.DoAlert($"<color=green>已隔空拾取：{item.Stats?.fullName ?? item.id}</color>", false);
                    }
                    else
                    {
                        PlayerCamera.main.DoAlert($"<color=orange>拾取失败：超出距离 ({dist:F1}m > {Settings.FetchDistance:F1}m)</color>", false);
                    }
                }
            }
        }

        private static void ExecuteExplosivesMacro(Body body)
        {
            List<Item> allItems = body.GetAllItemsThorough();
            int ignitedCount = 0;

            foreach (Item item in allItems)
            {
                if (item == null || item.Stats == null) continue;

                if (item.id.ToLower().Contains("dynamite"))
                {
                    if (item.Stats.usable && item.Stats.useAction != null)
                    {
                        item.Stats.useAction(body, item);
                        ignitedCount++;
                    }
                }
            }

            if (ignitedCount > 0)
            {
                PlayerCamera.main.DoAlert($"<color=red>警告：已点燃 {ignitedCount} 个炸药！赶紧跑！</color>", false);
            }
            else
            {
                PlayerCamera.main.DoAlert("身上或背包内未发现炸药 (dynamite)！", false);
            }
        }
    }
}