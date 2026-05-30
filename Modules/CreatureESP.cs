using FullBrightMod.Core;
using FullBrightMod.UI;
using UnityEngine;

namespace FullBrightMod.Modules
{
    public class CreatureESP : ModuleBase
    {
        public override string Name => Utils.I18n.Get("mod_creatureesp");
        public override ModuleCategory Category => ModuleCategory.Render;
        public override void OnEnable()  => Settings.IsCreatureEspEnabled = true;
        public override void OnDisable() => Settings.IsCreatureEspEnabled = false;

        public override float GetSettingsHeight() => 60f;

        public override void DrawSettings(float x, ref float y, float width, Event e)
        {
            ItemESP.DrawToggle(x, ref y, width, e,
                Utils.I18n.Get("set_esp_wireframe"),
                ref Settings.IsCreatureEspWireframeEnabled);

            // 复用 ItemESP 中的静态颜色选择器绘制方法
            ItemESP.DrawColorPicker(x, ref y, width, e,
                Utils.I18n.Get("set_creature_color") + ":",
                ref Settings.SelectedCreatureColor);
        }
    }
}
