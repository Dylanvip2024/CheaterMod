using System.Collections.Generic;
using UnityEngine;

namespace FullBrightMod.Core
{
    /// <summary>
    /// 模块管理器 —— 统一管理所有 ModuleBase 子类的生命周期调度。
    /// 主插件仅需将 Unity 生命周期转发给此管理器。
    /// </summary>
    public class ModuleManager
    {
        private readonly List<ModuleBase> _modules = new List<ModuleBase>();

        /// <summary>注册一个模块（仅加入列表，不强制启用）</summary>
        public void Register(ModuleBase module)
        {
            if (module == null) return;
            _modules.Add(module);
            Debug.Log($"[CheaterMod] Module registered: {module.Name}");
        }

        /// <summary>注册多个模块</summary>
        public void RegisterAll(params ModuleBase[] modules)
        {
            foreach (var m in modules) Register(m);
        }

        /// <summary>
        /// 快捷键监听 —— 遍历所有模块，检测绑定的按键是否被按下。
        /// 应在 Plugin.Update() 中每帧调用。
        /// </summary>
        public void HandleKeybinds()
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                ModuleBase m = _modules[i];
                if (m.BindKey == KeyCode.None) continue;
                if (Input.GetKeyDown(m.BindKey))
                {
                    m.Enabled = !m.Enabled;
                    if (m.Enabled) m.OnEnable();
                    else m.OnDisable();
                }
            }
        }

        /// <summary>对所有已启用的模块调用 OnUpdate</summary>
        public void UpdateModules()
        {
            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i].Enabled) _modules[i].OnUpdate();
        }

        /// <summary>对所有已启用的模块调用 OnLateUpdate</summary>
        public void LateUpdateModules()
        {
            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i].Enabled) _modules[i].OnLateUpdate();
        }

        /// <summary>对所有已启用的模块调用 OnGUI</summary>
        public void OnGUIModules()
        {
            for (int i = 0; i < _modules.Count; i++)
                if (_modules[i].Enabled) _modules[i].OnGUI();
        }

        /// <summary>获取所有已注册模块（供 ClickGUIManager 等查询）</summary>
        public List<ModuleBase> GetAllModules() => _modules;

        /// <summary>通过类型获取指定模块实例（泛型查询）</summary>
        public T GetModule<T>() where T : ModuleBase
        {
            foreach (var m in _modules)
                if (m is T t) return t;
            return null;
        }

        /// <summary>停用所有模块</summary>
        public void DisableAll()
        {
            foreach (var m in _modules)
            { m.Enabled = false; m.OnDisable(); }
        }
    }
}
