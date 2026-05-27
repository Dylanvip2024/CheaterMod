using System.Collections.Generic;
using FullBrightMod.Core;
using FullBrightMod.UI;       // ClickGUIManager.WhiteTexture
using FullBrightMod.Utils;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// 小地图模块 —— 使用双摄像机 + RenderTexture 方案。
    ///
    /// 设计：
    ///   - 动态创建一个独立的正交摄像机，强制跟随玩家位置（OnLateUpdate）。
    ///   - CullingMask: 白名单模式，只渲染 Ground, Item, Body, Limb, Descriptor。
    ///     绝不包含 Default、UI、TransparentFX 等可能含黑幕的层。
    ///   - 创建 RenderTexture 绑定到摄像机的 targetTexture。
    ///   - 在 OnGUI 中于右上角绘制 RenderTexture。
    ///   - 利用 ESPCache 在 RenderTexture 上叠加绘制生物/陷阱的红绿点。
    ///   - 支持鼠标拖拽移动小地图位置。
    ///   - orthographicSize 实时绑定 GlobalSettings.MinimapRadius。
    /// </summary>
    public class MinimapModule : ModuleBase
    {
        public override string Name => "Minimap";
        public override string Description => "Real-time minimap overlay using a second camera.";
        // Category=999 使其不在 Modules 标签页任何面板中显示（通过 Global Settings 控制）
        public override ModuleCategory Category => (ModuleCategory)999;

        // ---- 摄像机 ----
        private Camera _minimapCamera;
        private GameObject _cameraObject;

        // ---- RenderTexture ----
        private RenderTexture _renderTexture;
        private const int RenderTextureSize = 256;

        // ---- 拖拽状态 ----
        private bool _isDragging;
        private Vector2 _dragOffset;

        // ---- 缓存 ----
        private static readonly Color PlayerDotColor = Color.green;
        private static readonly Color CreatureDotColor = new Color(1f, 1f, 0f, 0.8f);  // 黄色：生物
        private static readonly Color TrapDotColor = new Color(1f, 0f, 0f, 0.9f);      // 红色：陷阱
        private static readonly Color ItemDotColor = new Color(0f, 1f, 1f, 0.7f);      // 青色：掉落物

        public override void OnEnable()
        {
        }

        /// <summary>跟随玩家位置</summary>
        private void TryFollowPlayer()
        {
            if (_minimapCamera == null) return;
            if (PlayerCamera.main?.body == null) return;
            Vector3 playerPos = PlayerCamera.main.body.transform.position;
            _minimapCamera.transform.position = new Vector3(playerPos.x, playerPos.y, -10f);
            _minimapCamera.transform.rotation = Quaternion.identity;
        }

        public override void OnDisable()
        {
            _isDragging = false;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_cameraObject != null)
            {
                Object.Destroy(_cameraObject);
                _cameraObject = null;
                _minimapCamera = null;
            }
        }
        
        public override void OnGUI()
        {
            if (!GlobalSettings.EnableMinimap) return;
            if (_renderTexture == null || !_renderTexture.IsCreated()) return;
            if (PlayerCamera.main == null || PlayerCamera.main.body == null) return;

            Event e = Event.current;
            Rect minimapRect = GlobalSettings.MinimapRect;

            // 1. 绘制 RenderTexture
            GUI.DrawTexture(minimapRect, _renderTexture);

            // 2. 绘制边框
            GUI.color = new Color(0.2f, 0.6f, 1.0f, 0.8f);
            DrawRectOutline(minimapRect, 2f);
            GUI.color = Color.white;

            // 3. 在 RenderTexture 上叠加绘制实体标记
            Vector3 playerPos = PlayerCamera.main.body.transform.position;
            float worldRadius = GlobalSettings.MinimapRadius;

            // 绘制玩家位置（中心绿点）
            Vector2 playerDot = new Vector2(
                minimapRect.x + minimapRect.width * 0.5f,
                minimapRect.y + minimapRect.height * 0.5f
            );
            GUI.color = PlayerDotColor;
            GUI.DrawTexture(new Rect(playerDot.x - 3f, playerDot.y - 3f, 6f, 6f), ClickGUIManager.WhiteTexture);

            // 绘制陷阱（使用 ESPCache 的白名单列表）
            GUI.color = TrapDotColor;
            DrawTrapDots(playerPos, minimapRect, worldRadius);

            // 绘制掉落物
            GUI.color = ItemDotColor;
            if (ESPCache.CachedItems != null)
            {
                int count = 0;
                foreach (var item in ESPCache.CachedItems)
                {
                    if (item == null) continue;
                    Vector2 screenPos;
                    if (WorldToMinimapPos(item.transform.position, playerPos, minimapRect, worldRadius, out screenPos))
                    {
                        GUI.DrawTexture(new Rect(screenPos.x - 1.5f, screenPos.y - 1.5f, 3f, 3f), ClickGUIManager.WhiteTexture);
                        count++;
                        if (count > 100) break;
                    }
                }
            }

            GUI.color = Color.white;

            // 4. 拖拽交互
            if (e.type == EventType.MouseDown && e.button == 0 && minimapRect.Contains(e.mousePosition))
            {
                _isDragging = true;
                _dragOffset = e.mousePosition - new Vector2(minimapRect.x, minimapRect.y);
                e.Use();
            }
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isDragging = false;
            }
            if (_isDragging && e.type == EventType.MouseDrag && e.button == 0)
            {
                float newX = e.mousePosition.x - _dragOffset.x;
                float newY = e.mousePosition.y - _dragOffset.y;

                if (GlobalSettings.EnableGridSnap && GlobalSettings.GridSize > 1f)
                {
                    float gs = GlobalSettings.GridSize;
                    newX = Mathf.Round(newX / gs) * gs;
                    newY = Mathf.Round(newY / gs) * gs;
                }

                GlobalSettings.MinimapRect = new Rect(newX, newY, minimapRect.width, minimapRect.height);
                e.Use();
            }
        }

        private void InitializeMinimap()
        {
            if (PlayerCamera.main == null || PlayerCamera.main.body == null) return;

            // 1. 创建 RenderTexture
            _renderTexture = new RenderTexture(RenderTextureSize, RenderTextureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "MinimapRT",
                hideFlags = HideFlags.DontSave
            };
            _renderTexture.Create();

            // 2. 创建小地图摄像机
            _cameraObject = new GameObject("MinimapCamera");
            _cameraObject.hideFlags = HideFlags.DontSave;

            _minimapCamera = _cameraObject.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = GlobalSettings.MinimapRadius;
            _minimapCamera.targetTexture = _renderTexture;
            _minimapCamera.clearFlags = CameraClearFlags.Color;
            _minimapCamera.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            _minimapCamera.depth = -99;
            _minimapCamera.nearClipPlane = 0.01f;
            _minimapCamera.farClipPlane = 500f;
            _minimapCamera.cullingMask = LayerMask.GetMask("Ground", "Item", "Body", "Limb", "Descriptor");

            var listener = _cameraObject.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;

            TryFollowPlayer();
        }

        public override void OnLateUpdate()
        {
            if (!GlobalSettings.EnableMinimap) return;

            // 延迟加载：如果还没初始化成功，尝试初始化
            if (_minimapCamera == null)
            {
                InitializeMinimap();
                if (_minimapCamera == null) return; // 如果玩家还是没加载出来，下帧再试
            }

            TryFollowPlayer();
            _minimapCamera.orthographicSize = GlobalSettings.MinimapRadius;
        }

        // =========================================================
        // 辅助方法
        // =========================================================

        /// <summary>
        /// 判断世界坐标是否在小地图范围内。
        /// 如果在范围内，通过 out 参数返回映射到小地图屏幕空间的坐标。
        /// </summary>
        private bool WorldToMinimapPos(Vector3 worldPos, Vector3 playerPos, Rect minimapRect, float worldRadius, out Vector2 screenPos)
        {
            screenPos = Vector2.zero;
            Vector3 offset = worldPos - playerPos;
            float halfSize = worldRadius;

            if (Mathf.Abs(offset.x) > halfSize || Mathf.Abs(offset.y) > halfSize)
                return false;

            float u = 0.5f + offset.x / (halfSize * 2f);
            float v = 0.5f - offset.y / (halfSize * 2f);

            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return false;

            screenPos = new Vector2(
                minimapRect.x + u * minimapRect.width,
                minimapRect.y + v * minimapRect.height
            );
            return true;
        }

        /// <summary>绘制陷阱的点标记（完整白名单）</summary>
        private void DrawTrapDots(Vector3 playerPos, Rect minimapRect, float worldRadius)
        {
            Vector2 sp;
            const float trapDotSize = 5f;

            // 逐个遍历 ESPCache 中的所有陷阱类型
            if (ESPCache.CachedBearTraps != null)
                foreach (var t in ESPCache.CachedBearTraps)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedMines != null)
                foreach (var t in ESPCache.CachedMines)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedSpikes != null)
                foreach (var t in ESPCache.CachedSpikes)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedTurrets != null)
                foreach (var t in ESPCache.CachedTurrets)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedCannons != null)
                foreach (var t in ESPCache.CachedCannons)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedDroppers != null)
                foreach (var t in ESPCache.CachedDroppers)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedGeysers != null)
                foreach (var t in ESPCache.CachedGeysers)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedJumpPads != null)
                foreach (var t in ESPCache.CachedJumpPads)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedBarbedWires != null)
                foreach (var t in ESPCache.CachedBarbedWires)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedCoils != null)
                foreach (var t in ESPCache.CachedCoils)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedTentacles != null)
                foreach (var t in ESPCache.CachedTentacles)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedGlassShards != null)
                foreach (var t in ESPCache.CachedGlassShards)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedRadObjects != null)
                foreach (var t in ESPCache.CachedRadObjects)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);

            if (ESPCache.CachedTickSwarms != null)
                foreach (var t in ESPCache.CachedTickSwarms)
                    if (t != null && WorldToMinimapPos(t.transform.position, playerPos, minimapRect, worldRadius, out sp))
                        GUI.DrawTexture(new Rect(sp.x - trapDotSize / 2, sp.y - trapDotSize / 2, trapDotSize, trapDotSize), ClickGUIManager.WhiteTexture);
        }

        /// <summary>绘制矩形边框</summary>
        private static void DrawRectOutline(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), ClickGUIManager.WhiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), ClickGUIManager.WhiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), ClickGUIManager.WhiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), ClickGUIManager.WhiteTexture);
        }
    }
}
