using System.Text.RegularExpressions;
using System.Collections;
using BepInEx;
using HarmonyLib;
using KrokoshaCasualtiesMP;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Networking;
using FullBrightMod.Core;
using FullBrightMod.Modules;
using FullBrightMod.UI;
using FullBrightMod.Utils;

namespace FullBrightMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class FullBrightPlugin : BaseUnityPlugin
    {
        private const string PluginGuid   = "com.mod.casualties.cheatermod";
        private const string PluginName   = "CheaterMod";
        private const string PluginVersion = "1.0.1";

        public static FullBrightPlugin Instance { get; private set; }

        private Harmony _harmony;
        private ModuleManager _moduleManager;
        private ClickGUIManager _clickGUI;

        // ==== 运行时状态（从旧 VisualModule/PlayerModule 迁移至此） ====
        private const string SunName = "ModGlobalSunObject";
        private GameObject _cachedSun;
        private Camera _mainCam;

        private void Awake()
        {
            Instance = this;
            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll();

                _moduleManager = new ModuleManager();
                // ★ 13 个原子功能模块，每个模块独立控制一个开关
                _moduleManager.RegisterAll(
                    // --- Render 分类 ---
                    new ItemESP(),        // 物品透视
                    new CreatureESP(),    // 生物透视
                    new TrapESP(),        // 陷阱警告
                    new FullBright(),     // 全亮模式
                    new VisionExpand(),   // 局部光照扩大
                    new CameraZoom(),     // 视距拉远
                    // --- Player 分类 ---
                    new Freecam(),        // 灵魂出窍
                    new LongHands(),      // 长手模式
                    new ThroughWall(),    // 隔墙取物
                    new AutoBandage(),    // 包扎大师
                    new ShrapnelMaker(),
                    new InstantAmputation(),
                    // --- Movement 分类 ---
                    new Flight(),         // 超级飞侠
                    new JumpBoost(),      // 跳跃增强
                    new NoClip(),         // 实体穿墙
                    // --- World 分类 ---
                    new AutoUnlock(),     // 秒开锁
                    // --- Misc 分类 ---
                    new AutoTranslate(),  // 聊天机翻
                    new IQ250(),          // 万事通模式
                    new LanguageModule(), // 语言切换
                    new ClickGUIModule(), // 菜单设置（默认 F6，可右键改键）
                    new AntiRagdoll(),    // 反布娃娃
                    new ExplosivesMacro(),// 一键引爆
                    new FetchMacro(),     // 捡取宏
                    new InstantShrapnelRemoval(),// 秒拔破片
                    new HumanBoombox()    // 人形音响
                );

                _clickGUI = new ClickGUIManager(_moduleManager);

                // ★ 加载持久化配置（覆盖模块状态、面板位置、Settings）
                ConfigManager.Load(_moduleManager, _clickGUI);

                Logger.LogInfo("========================================");
                Logger.LogInfo("[" + PluginName + "] 加载完毕！");
                Logger.LogInfo("  已注册模块数: " + _moduleManager.GetAllModules().Count);
                Logger.LogInfo("  ClickGUI: 默认按 [F6] 打开菜单");
                Logger.LogInfo("========================================");
            }
            catch (System.Exception ex)
            {
                Logger.LogError("[" + PluginName + "] 初始化失败: " + ex);
                throw;
            }
        }

        private void Update()
        {
            // 动态读取 ClickGUIModule 的绑键来切换菜单（替代硬编码 F6）
            var clickGuiMod = _moduleManager?.GetModule<ClickGUIModule>();
            if (clickGuiMod != null && clickGuiMod.BindKey != KeyCode.None)
            {
                if (Input.GetKeyDown(clickGuiMod.BindKey))
                {
                    _clickGUI.Toggle();
                    if (!_clickGUI.Visible)
                        ConfigManager.Save(_moduleManager, _clickGUI);
                }
            }

            // 菜单打开时解锁鼠标
            if (_clickGUI.Visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // 模块生命周期调度
            _moduleManager?.UpdateModules();

            // 快捷键监听（遍历所有模块，检测绑定的按键）
            _moduleManager?.HandleKeybinds();

            // ESP 实体缓存扫描
            ESPCache.Tick(Time.deltaTime);

            // ==== 运行时逻辑（从旧 VisualModule/PlayerModule 迁移至 Plugin 直接管理） ====
            _mainCam = Camera.main;

            // 人工太阳（仅在全亮开启时维护）
            ManageArtificialSun();

            // 灵魂出窍移动
            if (Settings.IsFreecamEnabled && _mainCam != null)
            {
                float moveSpeed = 30f * Time.deltaTime;
                if (Input.GetKey(KeyCode.LeftShift)) moveSpeed *= 3f;
                if (Input.GetKey(KeyCode.UpArrow))    Settings.FreecamPosition += Vector3.up    * moveSpeed;
                if (Input.GetKey(KeyCode.DownArrow))  Settings.FreecamPosition += Vector3.down  * moveSpeed;
                if (Input.GetKey(KeyCode.LeftArrow))  Settings.FreecamPosition += Vector3.left  * moveSpeed;
                if (Input.GetKey(KeyCode.RightArrow)) Settings.FreecamPosition += Vector3.right * moveSpeed;
            }
        }

        private void LateUpdate()
        {
            // 模块扩展预留
            _moduleManager?.LateUpdateModules();

            // 视距拉远（从旧 VisualModule 迁移至此）
            if (_mainCam != null && Settings.IsCameraZoomEnabled)
            {
                MonoBehaviour ppc = _mainCam.GetComponent("PixelPerfectCamera") as MonoBehaviour;
                if (ppc != null && ppc.enabled) ppc.enabled = false;
                _mainCam.orthographicSize = Settings.CustomCameraSize;
            }
        }

        private void OnGUI()
        {
            // ClickGUI 菜单（纯代码手绘）
            _clickGUI?.OnGUI();

            // 模块扩展预留
            _moduleManager?.OnGUIModules();

            // ==== ESP 渲染（从旧 ESPModule 迁移至此，直接读取 Settings 开关） ====
            if (_mainCam == null) return;

            if (Settings.IsItemEspEnabled)     ESPRenderer.DrawItems(_mainCam);
            if (Settings.IsCreatureEspEnabled) ESPRenderer.DrawCreatures(_mainCam);
            if (Settings.IsTrapEspEnabled)     ESPRenderer.DrawTraps(_mainCam);
        }

        // =========================================================
        // 人工太阳管理（从旧 VisualModule 迁移至此）
        // =========================================================
        private void ManageArtificialSun()
        {
            if (!Settings.IsFullBrightEnabled)
            {
                if (_cachedSun != null) { Destroy(_cachedSun); _cachedSun = null; }
                return;
            }

            bool hasGlobalLight = false;
            Light2D[] allLights = FindObjectsOfType<Light2D>();
            foreach (Light2D l in allLights)
            {
                if (l != null && l.lightType == Light2D.LightType.Global && l.gameObject.name != SunName)
                { hasGlobalLight = true; break; }
            }

            if (_cachedSun == null) _cachedSun = GameObject.Find(SunName);

            if (!hasGlobalLight && _cachedSun == null)
            {
                var sunObj = new GameObject(SunName);
                var sunLight = sunObj.AddComponent<Light2D>();
                sunLight.lightType = Light2D.LightType.Global;
                sunLight.intensity = Settings.BrightenIntensity;
                sunLight.color = Color.white;
                _cachedSun = sunObj;
            }
            else if (_cachedSun != null && hasGlobalLight)
            { Destroy(_cachedSun); _cachedSun = null; }
            else if (_cachedSun != null)
            {
                var sunLight = _cachedSun.GetComponent<Light2D>();
                if (sunLight != null) sunLight.intensity = Settings.BrightenIntensity;
            }
        }

        // =========================================================
        // 自动翻译（从旧 MiscModule 迁移至此，由 ChatTranslatePatch 触发）
        // =========================================================
        public void StartTranslate(string plrname, string originalMsg, bool richtext)
        {
            StartCoroutine(TranslateAndLogCoro(plrname, originalMsg, richtext));
        }

        private IEnumerator TranslateAndLogCoro(string plrname, string originalMsg, bool richtext)
        {
            // 防越界安全校验
            int sIdx = Mathf.Clamp(Settings.TranslateSourceIndex, 0, Settings.TranslateLangCodes.Length - 1);
            int tIdx = Mathf.Clamp(Settings.TranslateTargetIndex, 1, Settings.TranslateLangCodes.Length - 1);

            string sl = Settings.TranslateLangCodes[sIdx];
            string tl = Settings.TranslateLangCodes[tIdx];

            // 动态将 sl(源) 和 tl(目标) 拼接到 Google 翻译 API 中
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sl}&tl={tl}&dt=t&q="
                       + UnityWebRequest.EscapeURL(originalMsg);

            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    string translatedText = ExtractGoogleTranslate(req.downloadHandler.text);
                    if (!string.IsNullOrEmpty(translatedText) && translatedText.Trim() != originalMsg.Trim())
                        Chat.LogMessage(plrname, "<color=#00FFFF>[译]</color> " + translatedText, richtext);
                }
            }
        }

        private static string ExtractGoogleTranslate(string json)
        {
            string result = "";
            try
            {
                var matches = Regex.Matches(json, @"\[""(.*?)"",""");
                foreach (Match m in matches)
                {
                    if (m.Index > json.IndexOf("]],")) break;
                    result += Regex.Unescape(m.Groups[1].Value);
                }
            }
            catch { }
            return result;
        }

        private void OnDestroy()
        {
            // 退出前保存配置
            ConfigManager.Save(_moduleManager, _clickGUI);

            _moduleManager?.DisableAll();
            _harmony?.UnpatchSelf();
            if (_cachedSun != null) Destroy(_cachedSun);
        }
    }
}
