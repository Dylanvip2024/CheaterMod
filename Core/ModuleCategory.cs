namespace FullBrightMod.Core
{
    /// <summary>
    /// 模块功能分类 —— 用于 ClickGUI 面板分类展示。
    /// 每个 Module 必须归属到一个 Category，ClickGUIManager 按此分组渲染面板。
    /// </summary>
    public enum ModuleCategory
    {
        /// <summary>战斗相关（攻击、防御等）</summary>
        Combat,
        /// <summary>玩家自身（移动、血量、物品等）</summary>
        Player,
        /// <summary>移动相关（飞行、瞬移、加速等）</summary>
        Movement,
        /// <summary>渲染/视觉（ESP、全亮、视距等）</summary>
        Render,
        /// <summary>世界交互（挖掘、采集、开锁等）</summary>
        World,
        /// <summary>杂项（翻译、万事通等）</summary>
        Misc
    }
}
