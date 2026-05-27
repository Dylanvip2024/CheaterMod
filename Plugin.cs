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
        private const string PluginVersion = "1.0.3";

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
                    // --- Combat 分类 ---
                    new RapidFire(),      // 自定义射速
                    new NoJam(),          // 不卡壳
                    new NoRecoil(),       // 无后座
                    new AutoReload(),     // 自动装弹
                    new AutoBolt(),       // 自动拉栓
                    new MouseAimbot(),    // 鼠标吸附
                    new KillAura(),       // 杀戮光环
                    // --- Render 分类 ---
                    new ItemESP(),        // 物品透视
                    new CreatureESP(),    // 生物透视
                    new TrapESP(),        // 陷阱警告
                    new FullBright(),     // 全亮模式
                    new VisionExpand(),   // 局部光照扩大
                    new CameraZoom(),     // 视距拉远
                    new ThrowTrajectory(),// 投掷抛物线
                    // --- Player 分类 ---
                    new Freecam(),        // 灵魂出窍
                    new LongHands(),      // 长手模式
                    new ThroughWall(),    // 隔墙取物
                    new AutoBandage(),    // 包扎大师
                    new ShrapnelMaker(),  // 破片制造者
                    new InstantAmputation(),// 秒截肢
                    // --- Movement 分类 ---
                    new Flight(),         // 超级飞侠
                    new JumpBoost(),      // 跳跃增强
                    new SpeedModifier(),   // 速度修改
                    new AntiWeight(),      // 反负重
                    new AirJump(),         // 空气跳跃
                    new Jetpack(),         // 喷气背包
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
                    new HumanBoombox(),    // 人形音响
                    new MinimapModule(),   // 小地图
                    new LimbStatusModule() // 肢体状态 HUD
                );

                _clickGUI = new ClickGUIManager(_moduleManager);

                // ★ 加载持久化配置（覆盖模块状态、面板位置、Settings）
                ConfigManager.Load(_moduleManager, _clickGUI);

                Logger.LogInfo("========================================");
                Logger.LogInfo("[" + PluginName + "] 加载完毕！");
                Logger.LogInfo("  已注册模块数: " + _moduleManager.GetAllModules().Count);
                Logger.LogInfo("  ClickGUI: 默认按 [F6] 打开菜单");
                Logger.LogInfo("========================================");

                // ★ 挂载 UGUI Logo 管理器
                gameObject.AddComponent<LogoOverlayManager>();
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
            // 1. 绘制各个模块的 GUI 元素（如抛物线等）
            _moduleManager?.OnGUIModules();

            // 2. 绘制 ESP 渲染层
            if (_mainCam != null)
            {
                if (Settings.IsItemEspEnabled)     ESPRenderer.DrawItems(_mainCam);
                if (Settings.IsCreatureEspEnabled) ESPRenderer.DrawCreatures(_mainCam);
                if (Settings.IsTrapEspEnabled)     ESPRenderer.DrawTraps(_mainCam);
            }

            // 3. 最后绘制 ClickGUI 菜单，确保它永远在最顶层，不被任何东西遮挡
            _clickGUI?.OnGUI();
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
            // 1. 去除原消息中的富文本颜色标签，防止干扰谷歌翻译
            string cleanMsg = System.Text.RegularExpressions.Regex.Replace(originalMsg, "<.*?>", string.Empty);
            if (string.IsNullOrWhiteSpace(cleanMsg)) yield break;

            int sIdx = Mathf.Clamp(Settings.TranslateSourceIndex, 0, Settings.TranslateLangCodes.Length - 1);
            int tIdx = Mathf.Clamp(Settings.TranslateTargetIndex, 1, Settings.TranslateLangCodes.Length - 1);

            string sl = Settings.TranslateLangCodes[sIdx];
            string tl = Settings.TranslateLangCodes[tIdx];

            // 2. 使用更兼容的 Uri.EscapeDataString 替代原版的 EscapeURL
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sl}&tl={tl}&dt=t&q="
                       + System.Uri.EscapeDataString(cleanMsg);

            using (var req = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                // 伪装请求头，防止被谷歌封锁
                req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                yield return req.SendWebRequest();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // 3. 使用 Newtonsoft.Json 进行安全解析，彻底告别正则崩溃
                        var arr = Newtonsoft.Json.Linq.JArray.Parse(req.downloadHandler.text);
                        string translatedText = "";
                        foreach (var chunk in arr[0])
                        {
                            translatedText += chunk[0].ToString();
                        }

                        // 如果翻译出来的结果和原文不同，才发送到聊天框
                        if (!string.IsNullOrEmpty(translatedText) && translatedText.Trim() != cleanMsg.Trim())
                        {
                            Chat.LogMessage(plrname, "<color=#00FFFF>[译]</color> " + translatedText, true);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[机翻解析失败]: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 向外翻译：将玩家输入内容翻译为目标语言，通过回调返回结果。
        /// AutoTranslatePatch 在调用此方法后，会通过 onCompleted 回调拿到翻译结果。
        /// </summary>
        public void StartOutgoingTranslation(string originalMsg, System.Action<string> onCompleted)
        {
            StartCoroutine(TranslateOutgoingCoro(originalMsg, onCompleted));
        }

        private IEnumerator TranslateOutgoingCoro(string originalMsg, System.Action<string> onCompleted)
        {
            string sl = "auto";

            // 【修复翻译方向】：外发翻译的目标语言，应该是设置里的“源语言”。
            // 如果源语言是“自动”，那么我们无法翻译为自动，默认指定翻译为英语(2)
            int targetIdx = Settings.TranslateSourceIndex;
            if (targetIdx == 0) targetIdx = 2; // 默认英文

            string tl = Settings.TranslateLangCodes[Mathf.Clamp(targetIdx, 1, Settings.TranslateLangCodes.Length - 1)];

            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sl}&tl={tl}&dt=t&q="
                       + System.Uri.EscapeDataString(originalMsg);

            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var arr = Newtonsoft.Json.Linq.JArray.Parse(req.downloadHandler.text);
                        string result = "";
                        foreach (var chunk in arr[0]) result += chunk[0].ToString();
                        onCompleted?.Invoke(result);
                    }
                    catch
                    {
                        onCompleted?.Invoke(null);
                    }
                }
                else
                {
                    onCompleted?.Invoke(null);
                }
            }
        }

        private static string ExtractGoogleTranslate(string json)
        {
            try
            {
                // 4. 利用 Newtonsoft.Json 进行稳定的数组反序列化，彻底替代脆弱的正则表达式
                var arr = Newtonsoft.Json.Linq.JArray.Parse(json);
                string result = "";
                foreach (var chunk in arr[0])
                {
                    result += chunk[0].ToString();
                }
                return result;
            }
            catch
            {
                return "";
            }
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
