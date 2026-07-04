using System.Collections.Generic;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.UI
{
    [System.Serializable]
    public class ClickGUIPanel
    {
        public ModuleCategory Category;
        public Rect Position;
        public bool IsDragging;
        public Vector2 DragOffset;
        public bool IsExpanded = true;

        public const float TitleHeight = 32f;
        public const float ModuleRowHeight = 28f;
        public const float AccentLineThickness = 2f;

        /// <summary>抽屉动画当前高度（本帧实际渲染高度）</summary>
        public float CurrentHeight = -1f;

        /// <summary>抽屉动画目标高度（展开=全部, 折叠=仅标题）</summary>
        public float TargetHeight;

        /// <summary>SmoothDamp 速度缓存</summary>
        private float _heightVelocity;

        public void UpdateHeight(float target, float deltaTime)
        {
            TargetHeight = target;
            if (CurrentHeight < 0f) CurrentHeight = target; // 首帧初始化
            CurrentHeight = Mathf.SmoothDamp(CurrentHeight, TargetHeight, ref _heightVelocity, 0.12f);
        }

        public ClickGUIPanel(ModuleCategory category, int index)
        {
            Category = category;
            Position = new Rect(20 + index * 240, 60, 220, TitleHeight);
            IsDragging = false;
            DragOffset = Vector2.zero;
            IsExpanded = true;
        }
    }
}
