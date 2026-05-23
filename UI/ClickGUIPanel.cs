using System.Collections.Generic;
using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.UI
{
    /// <summary>
    /// ClickGUI 面板数据容器 —— 纯数据类，用于序列化持久化。
    /// 每个面板对应一个 Category，包含其拖拽状态和折叠状态。
    /// 所有字段均为 public 以便 JSON/XML 序列化。
    /// </summary>
    [System.Serializable]
    public class ClickGUIPanel
    {
        /// <summary>该面板对应的模块分类</summary>
        public ModuleCategory Category;

        /// <summary>面板在屏幕上的矩形区域（Position.x/y 为左上角坐标）</summary>
        public Rect Position;

        /// <summary>是否正在被鼠标拖拽</summary>
        public bool IsDragging;

        /// <summary>鼠标按下点相对于面板左上角的偏移（用于拖拽计算）</summary>
        public Vector2 DragOffset;

        /// <summary>是否展开（显示内部模块列表）</summary>
        public bool IsExpanded = true;

        // ---- 渲染常量（非序列化） ----
        public const float TitleHeight = 32f;     // 原来是 24，加高标题栏
        public const float ModuleRowHeight = 28f; // 原来是 20，加高模块行
        public const float AccentLineThickness = 2f;

        /// <summary>创建默认位置的面板（按 Category 顺序依次向右排列）</summary>
        public ClickGUIPanel(ModuleCategory category, int index)
        {
            Category = category;
            // 宽度从180增加到220，间距从190增加到240
            Position = new Rect(20 + index * 240, 60, 220, TitleHeight);
            IsDragging = false;
            DragOffset = Vector2.zero;
            IsExpanded = true;
        }
    }
}
