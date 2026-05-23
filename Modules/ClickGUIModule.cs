using FullBrightMod.Core;
using UnityEngine;

namespace FullBrightMod.Modules
{
    /// <summary>
    /// ClickGUI 菜单控制模块 —— 将"打开菜单"本身模块化，允许右键绑定快捷键。
    /// 构造函数强制默认绑键为 F6。
    /// </summary>
    public class ClickGUIModule : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_clickgui");
        public override ModuleCategory Category => ModuleCategory.Misc;

        public ClickGUIModule()
        {
            BindKey = KeyCode.F6;
        }
    }
}
