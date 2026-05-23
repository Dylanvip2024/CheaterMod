using UnityEngine;

namespace FullBrightMod.Core
{
    /// <summary>
    /// 模块抽象基类 —— 所有功能模块必须继承此类。
    /// 覆写需要的方法即可，ModuleManager 会在对应生命周期统一调度。
    /// </summary>
    public abstract class ModuleBase
    {
        /// <summary>模块名称（显示在 ClickGUI 面板中）</summary>
        public virtual string Name => GetType().Name;

        /// <summary>模块功能简述</summary>
        public virtual string Description => "";

        /// <summary>模块分类（ClickGUIManager 按此分组到对应面板）</summary>
        public virtual ModuleCategory Category => ModuleCategory.Misc;

        /// <summary>模块是否已启用（ClickGUI 左键切换）</summary>
        public bool Enabled { get; set; } = false;

        // ==== 右键展开子菜单 ====
        /// <summary>是否在 ClickGUI 中展开了设置子面板</summary>
        public bool IsExpanded { get; set; } = false;

        // ==== 快捷键绑定 ====
        /// <summary>绑定的快捷键（KeyCode.None 表示未绑定）</summary>
        public KeyCode BindKey { get; set; } = KeyCode.None;

        /// <summary>是否正在监听按键绑定（点击 Keybind 行后置 true，按下任意键后清除）</summary>
        public bool IsBinding { get; set; } = false;

        // ==== 自定义设置项 ====
        /// <summary>返回该模块自定义设置项需要的总高度（默认 0，无自定义设置）</summary>
        public virtual float GetSettingsHeight() => 0f;

        /// <summary>
        /// 绘制该模块的自定义设置项（如滑动条、颜色选择等）。
        /// 由 ClickGUIManager 在子面板区域内调用。
        /// </summary>
        /// <param name="x">面板左边界 X 坐标</param>
        /// <param name="y">当前绘制 Y 坐标（引用传递，调用方会在此值上累加已用高度）</param>
        /// <param name="width">面板宽度</param>
        /// <param name="e">当前 GUI 事件（用于鼠标检测）</param>
        public virtual void DrawSettings(float x, ref float y, float width, Event e) { }

        // ==== 生命周期 ====
        public virtual void OnEnable()  { }
        public virtual void OnDisable() { }
        public virtual void OnUpdate()  { }
        public virtual void OnLateUpdate() { }
        public virtual void OnGUI()     { }
    }
}
