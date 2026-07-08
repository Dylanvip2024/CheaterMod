using FullBrightMod.Core;
using FullBrightMod.UI;

namespace FullBrightMod
{
    public static class CheaterAPI
    {
        private static ModuleManager _moduleManager;
        private static ClickGUIManager _guiManager;

        public static void RegisterModule(ModuleBase module)
        {
            if (module == null) return;
            _moduleManager?.Register(module);
            _guiManager?.RebuildPanels();
        }

        internal static void Init(ModuleManager modManager, ClickGUIManager guiManager)
        {
            _moduleManager = modManager;
            _guiManager = guiManager;
        }
    }
}
