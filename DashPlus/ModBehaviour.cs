using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using UnityEngine.InputSystem;

namespace DashPlus
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        [Header("闪避参数直接设置")]
        [Tooltip("闪避距离倍数，1.0=原始距离")]
        public float dashDistanceMultiplier = 1;
        [Tooltip("体力消耗，原始10")]
        public float staminaCost = 10;
        [Tooltip("冷却时间(秒)，原始0.5")]
        public float coolTime = 0.5f;

        [Header("奔跑参数直接设置")]
        [Tooltip("步行速度倍数，1.0=原始速度")]
        public float walkSpeedMultiplier = 1;
        [Tooltip("奔跑速度倍数，1.0=原始速度")]
        public float runSpeedMultiplier = 1;
        [Tooltip("体力消耗率倍数，1.0=原始消耗率")]
        public float staminaDrainRateMultiplier = 1;
        [Tooltip("体力恢复率倍数，1.0=原始恢复率")]
        public float staminaRecoverRateMultiplier = 1;
        [Tooltip("体力恢复延迟倍数，1.0=原始延迟")]
        public float staminaRecoverTimeMultiplier = 1;

        [Header("移动手感设置")]
        [Tooltip("禁用移动惯性，开启后角色移动没有惯性打滑效果")]
        public bool disableMovementInertia = false;

        [Header("负重设置")]
        [Tooltip("启用无限负重，无视重量限制")]
        public bool enableInfiniteWeight = false;

        [Header("视野设置")]
        [Tooltip("视野倍数，1.0=原始视野")]
        public float fovMultiplier = 1.0f;
        [Tooltip("启用自定义视野")]
        public bool enableCustomFOV = false;

        [Header("闪避换弹设置")]
        [Tooltip("允许闪避时自动换弹")]
        public bool enableDashReload = false;

        [Header("射击打断换弹设置")]
        [Tooltip("允许射击键打断换弹")]
        public bool enableShootInterruptReload = false;

        [Header("击杀回血设置")]
        [Tooltip("启用击杀回血功能")]
        public bool enableKillHeal = false;
        [Tooltip("回血比例(基于敌人最大血量的百分比)")]
        public int healPercentage = 5;
        [Tooltip("最大回血量上限")]
        public float maxHealAmount = 50.0f;

        [Header("调试设置")]
        [Tooltip("是否输出调试日志")] public bool enableLogging = false;

        // 模态输入对话框状态管理
        private bool showInputDialog = false;
        private string? inputDialogTitle = "";
        private string? inputDialogPrompt = "";
        private float inputDialogValue = 0f;
        private float inputDialogMinValue = 0f;
        private float inputDialogMaxValue = 0f;
        private int currentEditingParameter = -1; // 当前编辑的参数索引
        private string inputDialogText = ""; // 输入框的文本
        private Rect inputDialogRect = new Rect(0, 0, 320, 180); // 对话框位置和大小
        private Texture2D? whiteTexture; // 用于半透明背景的白色纹理

        // 防止R键重复触发的静态变量
        private static bool rKeyPressed = false;
        private static int lastResetFrame = -1;

        private bool hasOriginalValues;
        private AnimationCurve? originalSpeedCurve;
        private float originalStaminaCost;
        private float originalCoolTime;

        // 奔跑参数原始值
        private float originalWalkSpeed;
        private float originalRunSpeed;
        private float originalStaminaDrainRate;
        private float originalStaminaRecoverRate;
        private float originalStaminaRecoverTime;

        // 移动惯性原始值
        private float originalWalkAcc;
        private float originalRunAcc;

        // 负重原始值
        private float originalMaxWeight;

        // 视野原始值
        private float originalDefaultFOV;
        private float originalAdsFOV;

        // GUI控制
        private bool showGUI = false;
        private Rect guiRect = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 200, 500, 400);
        private GameObject? guiPanel; // GUI面板GameObject，用于InputManager

        // 标签页控制
        private int selectedTab = 0; // 0: 闪避, 1: 奔跑, 2: 视野, 3: 回血, 4: 其他设置
        private readonly string[] tabNames = { "闪避 / Dash", "奔跑 / Run", "视野 / FOV", "回血 / Heal", "其他 / Others" };

        // 参数默认值常量
        private static readonly float DEFAULT_DASH_DISTANCE_MULTIPLIER = 1.0f;
        private static readonly float DEFAULT_STAMINA_COST = 10f;
        private static readonly float DEFAULT_COOL_TIME = 0.5f;
        private static readonly int DEFAULT_DASH_RELOAD_PERCENTAGE = 0;
        private static readonly float DEFAULT_WALK_SPEED_MULTIPLIER = 1.0f;
        private static readonly float DEFAULT_RUN_SPEED_MULTIPLIER = 1.0f;
        private static readonly float DEFAULT_STAMINA_DRAIN_RATE_MULTIPLIER = 1.0f;
        private static readonly float DEFAULT_STAMINA_RECOVER_RATE_MULTIPLIER = 1.0f;
        private static readonly float DEFAULT_STAMINA_RECOVER_TIME_MULTIPLIER = 1.0f;
        private static readonly float DEFAULT_FOV_MULTIPLIER = 1.0f;
        private static readonly int DEFAULT_HEAL_PERCENTAGE = 5;
        private static readonly float DEFAULT_MAX_HEAL_AMOUNT = 50.0f;

        // FOV滚轮调整相关
        private bool isScrollingFOV = false;
        private float lastScrollTime = 0f;
        private const float SCROLL_END_DELAY = 0.5f; // 滚轮停止后延迟保存时间

        // 滚轮输入拦截系统
        private bool scrollWheelInputBlocked = false;
        private InputAction? scrollWheelAction; // 使用object类型，通过反射调用InputAction方法

        // FOV平滑过渡系统
        private float currentFOVValue = 1.0f; // 当前实际应用的FOV值
        private float targetFOVValue = 1.0f; // 目标FOV值
        private float fovVelocity = 0f; // FOV变化速度（用于惯性效果）
        private const float FOV_SMOOTH_TIME = 0.15f; // FOV平滑过渡时间
        private bool needsFOVUpdate = false; // 是否需要更新FOV

        // 闪避换弹系统
        private bool wasDashing = false; // 上一帧是否在闪避
        private bool dashReloadTriggered = false; // 本次闪避是否已触发换弹

        // 时间累积换弹系统
        private bool dashReloadIntent = false; // 闪避期间是否有换弹意图
        private float dashStartTime = 0f; // 闪避开始时间
        private float originalReloadTime = 0f; // 武器原始换弹时间
        private int dashReloadPercentage = 0; // 闪避换弹百分比 (0-100)

        // 射击打断换弹优化
        private bool isCurrentlyReloading = false; // 是否当前在换弹状态
        private bool reloadInterruptChecked = false; // 本次换弹是否已经检查过打断
        private bool isEmptyClipAutoReload = false; // 是否是空弹夹自动换弹
        private float reloadStartTime = 0f; // 换弹开始时间
        private bool lastFireInputState = false; // 上一帧的开火输入状态
        private object? cachedInputManager; // 缓存的inputManager对象
        private const float MIN_INTERRUPT_DELAY = 0.1f; // 最小延迟100ms

        // 准心隐藏系统
        private bool aimMarkerHidden = true; // 准心是否已隐藏
        private AimMarker? cachedAimMarker; // 缓存的AimMarker组件
        private LevelManager? lastKnownLevelManager; // 上次已知的LevelManager

        protected override void OnAfterSetup()
        {
            base.OnAfterSetup();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            // 订阅LevelManager场景完全加载完成事件
            LevelManager.OnAfterLevelInitialized += OnLevelFullyLoaded;

            // 订阅击杀事件
            Health.OnDead += OnEnemyKilled;

            // 创建白色纹理用于半透明背景
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

            LoadSettings();
            // 初始化滚轮输入拦截系统
            InitializeScrollWheelInterception();
        }

        /// <summary>
        /// 初始化滚轮输入拦截系统
        /// 获取PlayerInput的ScrollWheel输入动作引用，用于后续的输入拦截
        /// </summary>
        void InitializeScrollWheelInterception()
        {
            try
            {
                // 查找PlayerInput组件
                var playerInput = FindObjectOfType<PlayerInput>();
                if (playerInput != null)
                {
                    var inputActions = playerInput.actions;
                    scrollWheelAction = inputActions["ScrollWheel"];

                    if (scrollWheelAction != null)
                    {
                        LogMessage("滚轮输入拦截系统初始化成功");
                    }
                    else
                    {
                        LogMessage("警告：无法找到ScrollWheel输入动作");
                    }
                }
                else
                {
                    LogMessage("警告：无法找到PlayerInput组件，滚轮拦截功能将不可用");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"滚轮输入拦截系统初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理滚轮输入拦截系统
        /// 在Mod卸载时重新启用滚轮输入，确保游戏功能正常
        /// </summary>
        void CleanupScrollWheelInterception()
        {
            try
            {
                // 如果滚轮输入仍被拦截，重新启用它
                if (scrollWheelInputBlocked && scrollWheelAction != null)
                {
                    scrollWheelAction.Enable();
                    scrollWheelInputBlocked = false;
                    LogMessage("Mod卸载时已清理滚轮输入拦截");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"清理滚轮输入拦截系统时发生异常: {ex.Message}");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl) && enableLogging)
            {
                TestGetHashCode();
            }
            
            // 检查快捷键：Ctrl+G 显示/隐藏GUI
            if (Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftControl))
            {
                ToggleGUI();
            }
            
            // 每帧检测ESC键状态，拦截原生ESC菜单
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (showGUI)
                {
                    // 如果游戏暂停菜单已经显示，先关闭它
                    if (PauseMenu.Instance != null && PauseMenu.Instance.Shown)
                    {
                        PauseMenu.Hide();
                        LogMessage("ESC拦截：关闭游戏暂停菜单");
                    } 
                    ToggleGUI();
                }
                // 否则让ESC键正常工作，显示游戏原生菜单
            }

            // 检查快捷键：Ctrl+滚轮调整FOV
            if (Input.GetKey(KeyCode.LeftControl) && enableCustomFOV)
            {
                float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scrollWheel) > 0.01f)
                {
                    if (!EnsureGUIExists())
                    {
                        return;
                    }

                    // 拦截滚轮输入，阻止其他系统响应
                    if (!scrollWheelInputBlocked && scrollWheelAction != null)
                    {
                        scrollWheelAction.Disable();
                        scrollWheelInputBlocked = true;
                        LogMessage("滚轮输入已拦截 - 其他系统将无法响应滚轮输入");
                    }

                    // 开始滚动或继续滚动
                    if (!isScrollingFOV)
                    {
                        isScrollingFOV = true;
                        targetFOVValue = fovMultiplier;
                    }

                    lastScrollTime = Time.time;

                    // 调整目标值（不是当前值）
                    targetFOVValue = Mathf.Clamp(targetFOVValue - scrollWheel * 0.5f, 0.2f, 3.0f);
                    needsFOVUpdate = true;

                    // 同步更新设置值（用于UI显示和保存）
                    fovMultiplier = targetFOVValue;
                }
            }

            // 检查滚轮是否停止，如果停止则保存设置并重新启用滚轮输入
            if (isScrollingFOV && Time.time - lastScrollTime > SCROLL_END_DELAY)
            {
                isScrollingFOV = false;
                SaveSettings();
                LogMessage($"FOV倍数调整为: {targetFOVValue:F1}x");

                // 重新启用滚轮输入，允许其他系统响应
                if (scrollWheelInputBlocked && scrollWheelAction != null)
                {
                    scrollWheelAction.Enable();
                    scrollWheelInputBlocked = false;
                    LogMessage("滚轮输入拦截已解除 - 其他系统可以正常响应滚轮输入");
                }
            }

            // 安全检查：如果Ctrl键释放但滚轮输入仍被拦截，立即重新启用
            if (scrollWheelInputBlocked && !Input.GetKey(KeyCode.LeftControl))
            {
                if (scrollWheelAction != null)
                {
                    scrollWheelAction.Enable();
                    scrollWheelInputBlocked = false;
                    LogMessage("Ctrl键已释放 - 立即解除滚轮输入拦截");
                }
            }

            // 检查快捷键：Ctrl+鼠标中键 还原默认FOV
            if (Input.GetKeyDown(KeyCode.Mouse2) && Input.GetKey(KeyCode.LeftControl) && enableCustomFOV)
            {
                ResetFOVToDefault();
            }

            // FOV平滑过渡系统 - 每帧都执行平滑更新
            if (needsFOVUpdate)
            {
                // 使用SmoothDamp实现平滑过渡
                currentFOVValue = Mathf.SmoothDamp(currentFOVValue, targetFOVValue, ref fovVelocity, FOV_SMOOTH_TIME);

                // 当接近目标值时，停止更新
                if (Mathf.Abs(currentFOVValue - targetFOVValue) < 0.001f)
                {
                    currentFOVValue = targetFOVValue;
                    fovVelocity = 0f;
                    needsFOVUpdate = false;
                }

                // 应用当前平滑后的FOV值
                ApplySmoothFOV();
            }

            // 闪避自动换弹系统
            if (enableDashReload && hasOriginalValues)
            {
                HandleDashReload();
            }

            // 射击打断换弹系统
            if (enableShootInterruptReload && hasOriginalValues)
            {
                HandleShootInterruptReload();
            }
        }

        /// <summary>
        /// 切换GUI显示状态并管理输入控制
        /// </summary>
        void ToggleGUI()
        {
            // 确保GUI面板存在
            if (!EnsureGUIExists())
            {
                return;
            }

            showGUI = !showGUI;
            guiPanel?.SetActive(showGUI);

            if (showGUI)
            {
                // GUI显示时：禁用游戏输入，显示鼠标
                try
                {
                    InputManager.DisableInput(guiPanel);
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    // 隐藏准心
                    HideAimMarker();

                    LogMessage("GUI已显示 - 游戏输入已暂停");
                }
                catch (Exception ex)
                {
                    LogMessage($"显示GUI时发生异常: {ex.Message}");
                    // 发生异常时重置状态
                    showGUI = false;
                    guiPanel?.SetActive(false);
                    // 恢复准心显示
                    ShowAimMarker();
                }
            }
            else
            {
                // GUI隐藏时：恢复游戏输入
                try
                {
                    InputManager.ActiveInput(guiPanel);

                    // 显示准心
                    ShowAimMarker();

                    LogMessage("GUI已隐藏 - 游戏输入已恢复");

                    // 关闭GUI时保存设置
                    SaveSettings();
                }
                catch (Exception ex)
                {
                    LogMessage($"隐藏GUI时发生异常: {ex.Message}");
                    // 发生异常时重置状态
                    showGUI = true;
                    guiPanel?.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 确保GUI面板存在，如果不存在则尝试创建
        /// 统一处理GUI面板创建逻辑，避免代码重复
        /// </summary>
        bool EnsureGUIExists()
        {
            // 如果GUI面板已存在，直接返回成功
            if (guiPanel != null)
            {
                return true;
            }

            // 尝试创建GUI面板
            if (!TryCreateGUI())
            {
                LogMessage("GUI创建失败：当前环境不支持");
                return false;
            }
            return true;
        }


        /// <summary>
        /// 创建GUI面板GameObject，用于InputManager输入管理
        /// </summary>
        void CreateGUIPanel()
        {
            if (guiPanel != null)
            {
                return;
            }

            guiPanel = new GameObject("DashPlus_GUIPanel");
            guiPanel.SetActive(false); // 初始状态为隐藏

            LogMessage("GUI面板已创建");
        }

        void OnActiveSceneChanged(Scene fromScene, Scene toScene)
        {
            LogMessage($"场景切换: {fromScene.name} -> {toScene.name}");

            // 在场景切换时强制关闭所有GUI界面
            if (showGUI || showInputDialog)
            {
                showGUI = false;
                showInputDialog = false; // 关闭输入对话框

                // 恢复输入控制（此时guiPanel肯定还存在）
                if (guiPanel != null)
                {
                    InputManager.ActiveInput(guiPanel);
                    guiPanel.SetActive(false);
                }

                // 清理对话框状态
                inputDialogTitle = "";
                inputDialogPrompt = "";
                inputDialogText = "";

                //默认先显示准心
                aimMarkerHidden = true;
                ShowAimMarker();
                
                // 保存设置
                SaveSettings();
                LogMessage("场景切换时已强制关闭所有GUI界面");
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (LevelManager.Instance == null)
            {
                hasOriginalValues = false;
                LogMessage($"当前场景为{scene.name}  {hasOriginalValues}");
            }
            else
            {
                LogMessage($"当前场景为{scene.name}  {hasOriginalValues}");
            }
        }

        /// <summary>
        /// LevelManager场景完全加载完成回调（在"Done!"之前触发）
        /// 这是确保所有游戏对象完全初始化后的最佳时机
        /// </summary>
        void OnLevelFullyLoaded()
        {
            cachedInputManager = null;
            hasOriginalValues = false;
            //默认先显示准心
            ShowAimMarker();
            LogMessage("LevelManager场景完全加载完成 - 所有游戏对象已就绪");

            // 在游戏对象完全初始化后创建GUI面板
            CreateGUIPanel();

            ApplyModIfExists();
        }

        private void TestGetHashCode()
        {
            var main = CharacterMainControl.Main;
            if (main?.CharacterItem == null) return;
            LogMessage("----------------------");
            LogMessage($"hash:{main.CharacterItem.GetStat("WalkSpeed".GetHashCode()).BaseValue} 走路:{main.CharacterWalkSpeed} 保存值:{originalWalkSpeed}");
            LogMessage($"hash:{main.CharacterItem.GetStat("RunSpeed".GetHashCode()).BaseValue} 奔跑:{main.CharacterRunSpeed} 保存值:{originalRunSpeed}");
            LogMessage($"hash:{main.CharacterItem.GetStat("StaminaDrainRate".GetHashCode()).BaseValue} 减益:{main.StaminaDrainRate} 保存值:{originalStaminaDrainRate}");
            LogMessage($"hash:{main.CharacterItem.GetStat("StaminaRecoverRate".GetHashCode()).BaseValue} 恢复:{main.StaminaRecoverRate} 保存值:{originalStaminaRecoverRate}");
            LogMessage($"hash:{main.CharacterItem.GetStat("StaminaRecoverTime".GetHashCode()).BaseValue} 恢复时间:{main.StaminaRecoverTime} 保存值:{originalStaminaRecoverTime}");
            LogMessage($"hash:{main.CharacterItem.GetStat("WalkAcc".GetHashCode()).BaseValue} 移动惯性:{main.CharacterWalkAcc} 保存值:{originalWalkAcc}");
            LogMessage($"hash:{main.CharacterItem.GetStat("RunAcc".GetHashCode()).BaseValue} 奔跑惯性:{main.CharacterRunAcc} 保存值:{originalRunAcc}");
            LogMessage($"hash:{main.CharacterItem.GetStat("MaxWeight".GetHashCode()).BaseValue} 负重:{main.MaxWeight} 保存值:{originalMaxWeight}");
            LogMessage("----------------------");
        }

        /// <summary>
        /// 延迟保存CharacterItem相关参数的原始值
        /// </summary>
        private void DelayedSaveOriginalValues()
        {
            StartCoroutine(SaveOriginalValuesCoroutine());
        }

        /// <summary>
        /// 协程：延迟0.2秒后保存所有原始值
        /// </summary>
        private IEnumerator SaveOriginalValuesCoroutine()
        {
            yield return new WaitForSeconds(0.2f);

            var main = CharacterMainControl.Main;
            if (main == null) yield break;
            // 保存所有CharacterItem相关参数的原始值 - 使用 GetStat 的 BaseValue 以保持一致性
            if (main.CharacterItem != null)
            {
                SaveOriginalValuesFromCharacterItem(main);
            }
            else
            {
                // 备用方案：直接从 CharacterMainControl 获取所有参数
                SaveOriginalValuesFromMainControl(main);
            }

            // 保存视野原始值（无论哪种方案都需要）
            SaveOriginalFOVValues();

            hasOriginalValues = true;
            
            ApplyMod(main);

            LogMessage(
                $"闪避原始值: 曲线key数={originalSpeedCurve?.keys.Length}, 体力={originalStaminaCost}, 冷却={originalCoolTime:F2}s");
            LogMessage(
                $"奔跑原始值: 步速={originalWalkSpeed:F2}, 奔速={originalRunSpeed:F2}, 消耗率={originalStaminaDrainRate:F2}, 恢复率={originalStaminaRecoverRate:F2}, 恢复延迟={originalStaminaRecoverTime:F2}");
        }

        /// <summary>
        /// 从CharacterItem保存原始值
        /// </summary>
        private void SaveOriginalValuesFromCharacterItem(CharacterMainControl main)
        {
            // 奔跑参数
            var walkStat = main.CharacterItem.GetStat("WalkSpeed".GetHashCode());
            var runStat = main.CharacterItem.GetStat("RunSpeed".GetHashCode());
            var drainStat = main.CharacterItem.GetStat("StaminaDrainRate".GetHashCode());
            var recoverStat = main.CharacterItem.GetStat("StaminaRecoverRate".GetHashCode());
            var recoverTimeStat = main.CharacterItem.GetStat("StaminaRecoverTime".GetHashCode());

            // 移动惯性参数
            var walkAccStat = main.CharacterItem.GetStat("WalkAcc".GetHashCode());
            var runAccStat = main.CharacterItem.GetStat("RunAcc".GetHashCode());

            // 负重参数
            var maxWeightStat = main.CharacterItem.GetStat("MaxWeight".GetHashCode());

            // 奔跑参数原始值
            originalWalkSpeed = walkStat.BaseValue;
            originalRunSpeed = runStat.BaseValue;
            originalStaminaDrainRate = drainStat.BaseValue;
            originalStaminaRecoverRate = recoverStat.BaseValue;
            originalStaminaRecoverTime = recoverTimeStat.BaseValue;

            // 移动惯性原始值
            originalWalkAcc = walkAccStat.BaseValue;
            originalRunAcc = runAccStat.BaseValue;

            // 负重原始值
            originalMaxWeight = maxWeightStat.BaseValue;
            
            TestGetHashCode();
        }

        /// <summary>
        /// 从MainControl保存原始值（备用方案）
        /// </summary>
        private void SaveOriginalValuesFromMainControl(CharacterMainControl main)
        {
            originalWalkSpeed = main.CharacterWalkSpeed;
            originalRunSpeed = main.CharacterRunSpeed;
            originalStaminaDrainRate = main.StaminaDrainRate;
            originalStaminaRecoverRate = main.StaminaRecoverRate;
            originalStaminaRecoverTime = main.StaminaRecoverTime;
            originalWalkAcc = main.CharacterWalkAcc;
            originalRunAcc = main.CharacterRunAcc;
            originalMaxWeight = main.MaxWeight;
        }

        /// <summary>
        /// 保存视野原始值
        /// </summary>
        private void SaveOriginalFOVValues()
        {
            var gameCamera = GameCamera.Instance;
            if (gameCamera != null)
            {
                originalDefaultFOV = gameCamera.defaultFOV;
                originalAdsFOV = gameCamera.adsFOV;
            }
        }

        void ApplyModIfExists()
        {
            var main = CharacterMainControl.Main;
            if (main?.dashAction == null) return;

            // 第一次遇到角色时保存原始值
            if (!hasOriginalValues)
            {
                // 保存闪避参数原始值
                originalSpeedCurve = main.dashAction.speedCurve;
                originalStaminaCost = main.dashAction.staminaCost;
                originalCoolTime = main.dashAction.coolTime;

                // 初始化FOV平滑系统
                currentFOVValue = fovMultiplier;
                targetFOVValue = fovMultiplier;

                // 延迟0.2秒保存原始值，确保游戏对象完全加载
                DelayedSaveOriginalValues();
                return;
            }
            ApplyMod(main);
        }

        void ApplyMod(CharacterMainControl main)
        {
            var dash = main.dashAction;
            if (dash == null) return;

            // 应用闪避参数修改
            ApplyDashMod(main, dash);

            // 应用奔跑参数修改
            ApplyRunMod(main);

            // 应用移动手感修改
            ApplyInertiaMod(main);

            // 应用负重修改
            ApplyWeightMod(main);

            // 应用视野修改
            ApplyFOVMod();
        }

        void ApplyDashMod(CharacterMainControl main, CA_Dash dash)
        {
            // 修改speedCurve来控制闪避距离
            if (originalSpeedCurve != null)
            {
                if (dashDistanceMultiplier == 1.0f)
                {
                    // 重置为原始曲线
                    dash.speedCurve = originalSpeedCurve;
                }
                else
                {
                    // 应用修改后的曲线
                    AnimationCurve newCurve = new AnimationCurve();
                    for (int i = 0; i < originalSpeedCurve.keys.Length; i++)
                    {
                        Keyframe key = originalSpeedCurve.keys[i];
                        newCurve.AddKey(new Keyframe(key.time, key.value * dashDistanceMultiplier, key.inTangent,
                            key.outTangent));
                    }

                    dash.speedCurve = newCurve;
                    LogMessage(
                        $"SpeedCurve修改: 原始keys={originalSpeedCurve.keys.Length}, 倍数={dashDistanceMultiplier}");
                }
            }

            // 直接设置体力消耗 - 总是应用当前值
            dash.staminaCost = staminaCost;

            // 直接设置冷却时间 - 总是应用当前值
            dash.coolTime = coolTime;

            LogMessage(
                $"闪避已应用: 距离倍数={dashDistanceMultiplier}x, 体力={dash.staminaCost:F1}, 冷却={dash.coolTime:F2}s");
        }

        void ApplyRunMod(CharacterMainControl main)
        {
            if (main.CharacterItem == null) return;

            // 修改步行速度
            var walkStat = main.CharacterItem.GetStat("WalkSpeed".GetHashCode());
            if (walkStat != null && originalWalkSpeed > 0)
            {
                float targetWalkSpeed = walkSpeedMultiplier == 1.0f ? originalWalkSpeed : originalWalkSpeed * walkSpeedMultiplier;
                if (walkStat.BaseValue != targetWalkSpeed)
                {
                    walkStat.BaseValue = targetWalkSpeed;
                    LogMessage($"步行速度修改: {originalWalkSpeed:F2} -> {targetWalkSpeed:F2} (倍数={walkSpeedMultiplier})");
                }
            }

            // 修改奔跑速度
            var runStat = main.CharacterItem.GetStat("RunSpeed".GetHashCode());
            if (runStat != null && originalRunSpeed > 0)
            {
                float targetRunSpeed = runSpeedMultiplier == 1.0f ? originalRunSpeed : originalRunSpeed * runSpeedMultiplier;
                if (runStat.BaseValue != targetRunSpeed)
                {
                    runStat.BaseValue = targetRunSpeed;
                    LogMessage($"奔跑速度修改: {originalRunSpeed:F2} -> {targetRunSpeed:F2} (倍数={runSpeedMultiplier})");
                }
            }

            // 修改体力消耗率
            var drainStat = main.CharacterItem.GetStat("StaminaDrainRate".GetHashCode());
            if (drainStat != null && originalStaminaDrainRate > 0)
            {
                float targetDrainRate = staminaDrainRateMultiplier == 1.0f ? originalStaminaDrainRate : originalStaminaDrainRate * staminaDrainRateMultiplier;
                if (drainStat.BaseValue != targetDrainRate)
                {
                    drainStat.BaseValue = targetDrainRate;
                    LogMessage($"体力消耗率修改: {originalStaminaDrainRate:F2} -> {targetDrainRate:F2} (倍数={staminaDrainRateMultiplier})");
                }
            }

            // 修改体力恢复率
            var recoverStat = main.CharacterItem.GetStat("StaminaRecoverRate".GetHashCode());
            if (recoverStat != null && originalStaminaRecoverRate > 0)
            {
                float targetRecoverRate = staminaRecoverRateMultiplier == 1.0f ? originalStaminaRecoverRate : originalStaminaRecoverRate * staminaRecoverRateMultiplier;
                if (recoverStat.BaseValue != targetRecoverRate)
                {
                    recoverStat.BaseValue = targetRecoverRate;
                    LogMessage($"体力恢复率修改: {originalStaminaRecoverRate:F2} -> {targetRecoverRate:F2} (倍数={staminaRecoverRateMultiplier})");
                }
            }

            // 修改体力恢复延迟
            var recoverTimeStat = main.CharacterItem.GetStat("StaminaRecoverTime".GetHashCode());
            if (recoverTimeStat != null && originalStaminaRecoverTime > 0)
            {
                float targetRecoverTime = staminaRecoverTimeMultiplier == 1.0f ? originalStaminaRecoverTime : originalStaminaRecoverTime * staminaRecoverTimeMultiplier;
                if (recoverTimeStat.BaseValue != targetRecoverTime)
                {
                    recoverTimeStat.BaseValue = targetRecoverTime;
                    LogMessage($"体力恢复延迟修改: {originalStaminaRecoverTime:F2} -> {targetRecoverTime:F2} (倍数={staminaRecoverTimeMultiplier})");
                }
            }
        }

        void ApplyInertiaMod(CharacterMainControl main)
        {
            if (main.CharacterItem == null) return;

            // 获取加速度统计对象
            var walkAccStat = main.CharacterItem.GetStat("WalkAcc".GetHashCode());
            var runAccStat = main.CharacterItem.GetStat("RunAcc".GetHashCode());

            if (disableMovementInertia)
            {
                // 禁用惯性：设置极高的加速度值，让速度变化几乎是瞬间的
                float instantAcc = 9999f; // 超高加速度，实现瞬间移动

                if (walkAccStat != null && originalWalkAcc > 0)
                {
                    walkAccStat.BaseValue = instantAcc;
                    LogMessage($"步行惯性已禁用: {originalWalkAcc:F2} -> {instantAcc:F2}");
                }

                if (runAccStat != null && originalRunAcc > 0)
                {
                    runAccStat.BaseValue = instantAcc;
                    LogMessage($"奔跑惯性已禁用: {originalRunAcc:F2} -> {instantAcc:F2}");
                }
            }
            else
            {
                // 恢复原始加速度值
                if (walkAccStat != null && originalWalkAcc > 0)
                {
                    walkAccStat.BaseValue = originalWalkAcc;
                    LogMessage($"步行惯性已恢复: {originalWalkAcc:F2}");
                }

                if (runAccStat != null && originalRunAcc > 0)
                {
                    runAccStat.BaseValue = originalRunAcc;
                    LogMessage($"奔跑惯性已恢复: {originalRunAcc:F2}");
                }
            }
        }

        void ApplyWeightMod(CharacterMainControl main)
        {
            if (main.CharacterItem == null) return;

            // 获取负重统计对象
            var maxWeightStat = main.CharacterItem.GetStat("MaxWeight".GetHashCode());

            if (maxWeightStat != null && originalMaxWeight > 0)
            {
                if (enableInfiniteWeight)
                {
                    // 启用无限负重：设置一个极大的值
                    float infiniteWeight = 9999999f;
                    if (maxWeightStat.BaseValue != infiniteWeight)
                    {
                        maxWeightStat.BaseValue = infiniteWeight;
                        LogMessage($"无限负重已启用: {originalMaxWeight:F2} -> {infiniteWeight:F2}");
                    }
                }
                else
                {
                    // 恢复原始负重值
                    if (maxWeightStat.BaseValue != originalMaxWeight)
                    {
                        maxWeightStat.BaseValue = originalMaxWeight;
                        LogMessage($"负重已恢复: {originalMaxWeight:F2}");
                    }
                }
            }
        }

        void ApplyFOVMod()
        {
            var gameCamera = GameCamera.Instance;
            if (gameCamera == null || originalDefaultFOV <= 0) return;

            if (enableCustomFOV)
            {
                // 同步目标值
                targetFOVValue = fovMultiplier;
                if (!needsFOVUpdate)
                {
                    currentFOVValue = fovMultiplier;
                }

                // 应用视野倍数
                float targetDefaultFOV = fovMultiplier == 1.0f ? originalDefaultFOV : originalDefaultFOV * fovMultiplier;
                float targetAdsFOV = fovMultiplier == 1.0f ? originalAdsFOV : originalAdsFOV * fovMultiplier;

                if (gameCamera.defaultFOV != targetDefaultFOV)
                {
                    gameCamera.defaultFOV = targetDefaultFOV;
                    LogMessage($"默认视野修改: {originalDefaultFOV:F2} -> {targetDefaultFOV:F2} (倍数={fovMultiplier})");
                }

                if (gameCamera.adsFOV != targetAdsFOV)
                {
                    gameCamera.adsFOV = targetAdsFOV;
                    LogMessage($"瞄准视野修改: {originalAdsFOV:F2} -> {targetAdsFOV:F2} (倍数={fovMultiplier})");
                }
            }
            else
            {
                // 恢复原始视野值
                if (gameCamera.defaultFOV != originalDefaultFOV)
                {
                    gameCamera.defaultFOV = originalDefaultFOV;
                    LogMessage($"默认视野已恢复: {originalDefaultFOV:F2}");
                }

                if (gameCamera.adsFOV != originalAdsFOV)
                {
                    gameCamera.adsFOV = originalAdsFOV;
                    LogMessage($"瞄准视野已恢复: {originalAdsFOV:F2}");
                }
            }
        }

        void ApplySmoothFOV()
        {
            var gameCamera = GameCamera.Instance;
            if (gameCamera == null || originalDefaultFOV <= 0 || !enableCustomFOV) return;

            // 应用平滑后的视野倍数
            float smoothDefaultFOV = currentFOVValue == 1.0f ? originalDefaultFOV : originalDefaultFOV * currentFOVValue;
            float smoothAdsFOV = currentFOVValue == 1.0f ? originalAdsFOV : originalAdsFOV * currentFOVValue;

            gameCamera.defaultFOV = smoothDefaultFOV;
            gameCamera.adsFOV = smoothAdsFOV;
        }

        /// <summary>
        /// 还原FOV到默认值
        /// </summary>
        void ResetFOVToDefault()
        {
            if (!EnsureGUIExists())
            {
                return;
            }

            // 设置为目标默认值
            targetFOVValue = DEFAULT_FOV_MULTIPLIER;
            fovMultiplier = DEFAULT_FOV_MULTIPLIER;
            needsFOVUpdate = true;

            // 保存设置
            SaveSettings();

            LogMessage($"FOV已还原默认值: {DEFAULT_FOV_MULTIPLIER:F1}x");
        }

        void LoadSettings()
        {
            // 闪避参数
            dashDistanceMultiplier = PlayerPrefs.GetFloat("DashPlus_DashDistance", 1.0f);
            staminaCost = PlayerPrefs.GetFloat("DashPlus_Stamina", 10f);
            coolTime = PlayerPrefs.GetFloat("DashPlus_CoolTime", 0.5f);

            // 闪避换弹设置
            enableDashReload = PlayerPrefs.GetInt("DashPlus_DashReload", 0) == 1;
            dashReloadPercentage = PlayerPrefs.GetInt("DashPlus_DashReloadPercentage", 0);
            enableShootInterruptReload = PlayerPrefs.GetInt("DashPlus_ShootInterruptReload", 0) == 1;

            // 击杀回血设置
            enableKillHeal = PlayerPrefs.GetInt("DashPlus_KillHeal", 0) == 1;
            healPercentage = PlayerPrefs.GetInt("DashPlus_HealPercentage", 5);
            maxHealAmount = PlayerPrefs.GetFloat("DashPlus_MaxHealAmount", 50.0f);

            // 奔跑参数
            walkSpeedMultiplier = PlayerPrefs.GetFloat("DashPlus_WalkSpeed", 1.0f);
            runSpeedMultiplier = PlayerPrefs.GetFloat("DashPlus_RunSpeed", 1.0f);
            staminaDrainRateMultiplier = PlayerPrefs.GetFloat("DashPlus_StaminaDrain", 1.0f);
            staminaRecoverRateMultiplier = PlayerPrefs.GetFloat("DashPlus_StaminaRecover", 1.0f);
            staminaRecoverTimeMultiplier = PlayerPrefs.GetFloat("DashPlus_StaminaRecoverTime", 1.0f);

            // 移动惯性参数
            disableMovementInertia = PlayerPrefs.GetInt("DashPlus_DisableInertia", 0) == 1;

            // 负重参数
            enableInfiniteWeight = PlayerPrefs.GetInt("DashPlus_InfiniteWeight", 0) == 1;

            // 视野参数
            enableCustomFOV = PlayerPrefs.GetInt("DashPlus_CustomFOV", 0) == 1;
            fovMultiplier = PlayerPrefs.GetFloat("DashPlus_FOV", 1.0f);

            enableLogging = PlayerPrefs.GetInt("DashPlus_Logging", 0) == 1;
            LogMessage($"设置已加载:\n" +
                      $"  闪避: 距离={dashDistanceMultiplier}x, 体力={staminaCost}, 冷却={coolTime:F2}s\n" +
                      $"  奔跑: 步行={walkSpeedMultiplier}x, 奔跑={runSpeedMultiplier}x, 消耗={staminaDrainRateMultiplier}x, 恢复={staminaRecoverRateMultiplier}x, 恢复延迟={staminaRecoverTimeMultiplier}x\n" +
                      $"  惯性: 禁用={disableMovementInertia}\n" +
                      $"  负重: 无限={enableInfiniteWeight}\n" +
                      $"  视野: 自定义={enableCustomFOV}, 倍数={fovMultiplier:F1}x\n" +
                      $"  回血: 启用={enableKillHeal}, 比例={healPercentage:F1}%, 最大={maxHealAmount:F1}\n" +
                      $"  换弹: 闪避换弹={enableDashReload}({dashReloadPercentage}%), 射击打断={enableShootInterruptReload}\n" +
                      $"  调试: 日志={enableLogging}");
        }

        void SaveSettings()
        {
            // 闪避参数
            PlayerPrefs.SetFloat("DashPlus_DashDistance", dashDistanceMultiplier);
            PlayerPrefs.SetFloat("DashPlus_Stamina", staminaCost);
            PlayerPrefs.SetFloat("DashPlus_CoolTime", coolTime);

            // 闪避换弹设置
            PlayerPrefs.SetInt("DashPlus_DashReload", enableDashReload ? 1 : 0);
            PlayerPrefs.SetInt("DashPlus_DashReloadPercentage", dashReloadPercentage);
            PlayerPrefs.SetInt("DashPlus_ShootInterruptReload", enableShootInterruptReload ? 1 : 0);

            // 击杀回血设置
            PlayerPrefs.SetInt("DashPlus_KillHeal", enableKillHeal ? 1 : 0);
            PlayerPrefs.SetInt("DashPlus_HealPercentage", healPercentage);
            PlayerPrefs.SetFloat("DashPlus_MaxHealAmount", maxHealAmount);

            // 奔跑参数
            PlayerPrefs.SetFloat("DashPlus_WalkSpeed", walkSpeedMultiplier);
            PlayerPrefs.SetFloat("DashPlus_RunSpeed", runSpeedMultiplier);
            PlayerPrefs.SetFloat("DashPlus_StaminaDrain", staminaDrainRateMultiplier);
            PlayerPrefs.SetFloat("DashPlus_StaminaRecover", staminaRecoverRateMultiplier);
            PlayerPrefs.SetFloat("DashPlus_StaminaRecoverTime", staminaRecoverTimeMultiplier);

            // 移动惯性参数
            PlayerPrefs.SetInt("DashPlus_DisableInertia", disableMovementInertia ? 1 : 0);

            // 负重参数
            PlayerPrefs.SetInt("DashPlus_InfiniteWeight", enableInfiniteWeight ? 1 : 0);

            // 视野参数
            PlayerPrefs.SetInt("DashPlus_CustomFOV", enableCustomFOV ? 1 : 0);
            PlayerPrefs.SetFloat("DashPlus_FOV", fovMultiplier);

            PlayerPrefs.SetInt("DashPlus_Logging", enableLogging ? 1 : 0);
            PlayerPrefs.Save();
            LogMessage("设置已保存");
        }

        void OnApplicationQuit()
        {
            // 游戏退出时保存设置
            SaveSettings();
        }

        void OnGUI()
        {
            if (!showGUI && !showInputDialog) return;

            GUI.skin.window.fontSize = 14;
            GUI.skin.label.fontSize = 14;
            GUI.skin.horizontalSlider.fixedHeight = 20;
            GUI.skin.horizontalSliderThumb.fixedHeight = 25;
            GUI.skin.horizontalSliderThumb.fixedWidth = 25;

            // 绘制模态输入对话框（如果需要显示）
            DrawModalDialog();

            // 只有在没有显示输入对话框时才绘制主窗口
            if (showGUI && !showInputDialog)
            {
                int windowId = 12345;
                Rect oldRect = guiRect;
                guiRect = GUI.Window(windowId, guiRect, DoWindow, "DashPlus 增强控制面板");

                // 边界检测：确保窗口始终保持在屏幕可见范围内
                if (oldRect.x != guiRect.x || oldRect.y != guiRect.y)
                {
                    // 窗口位置发生变化，应用边界约束
                    guiRect.x = Mathf.Clamp(guiRect.x, 0, Screen.width - guiRect.width);
                    guiRect.y = Mathf.Clamp(guiRect.y, 0, Screen.height - guiRect.height);
                }
            }
        }

        void DoWindow(int windowId)
        {
            // 设置焦点到主窗口，确保ESC键能被正确处理
            GUI.SetNextControlName("DashPlusMainWindow");
            GUI.FocusControl("DashPlusMainWindow");

            // 右上角关闭按钮
            if (GUI.Button(new Rect(guiRect.width - 25, 5, 20, 20), "×"))
            {
                ToggleGUI(); // 使用ToggleGUI确保正确的输入管理
            }

            // 增加标题栏下方空间，让标题区域更宽敞
            GUILayout.Space(15);

            GUILayout.BeginVertical();

            // 标签栏
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                bool isSelected = (selectedTab == i);
                Color originalColor = GUI.backgroundColor;

                if (isSelected)
                {
                    GUI.backgroundColor = Color.gray;
                }

                if (GUILayout.Button(tabNames[i], GUILayout.Height(30)))
                {
                    selectedTab = i;
                }

                GUI.backgroundColor = originalColor;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 根据选中的标签页显示不同内容
            switch (selectedTab)
            {
                case 0: // 闪避参数
                    DrawDashTab();
                    break;
                case 1: // 奔跑参数
                    DrawRunTab();
                    break;
                case 2: // 视野设置
                    DrawFOVTab();
                    break;
                case 3: // 回血设置
                    DrawHealTab();
                    break;
                case 4: // 其他设置
                    DrawSettingsTab();
                    break;
            }

            // 通用按钮区域
            GUILayout.Space(10);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUILayout.Space(10);

            // 恢复默认按钮
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("恢复默认设置(所有参数) / Reset to Default(All Parameters)", GUILayout.Width(300), GUILayout.Height(40)))
            {
                ResetAllParameters();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label("Ctrl+G 隐藏/显示此面板 / Hide/Show Panel", GUI.skin.box);
            GUILayout.Label("点击滑动条右侧数字精确编辑 / Click slider value to edit precisely", GUI.skin.box);
            GUILayout.Label("悬浮滑动条上按R重置该参数 / Hover on slider and press R to reset", GUI.skin.box);
            GUILayout.Space(5);
            GUILayout.EndVertical();

            // 自动调整窗口高度
            if (Event.current.type == EventType.Repaint)
            {
                Vector2 currentSize = GUILayoutUtility.GetLastRect().size;
                float targetHeight = Mathf.Max(350f, currentSize.y + 40f); // 最小高度350px，加上边距
                if (Mathf.Abs(guiRect.height - targetHeight) > 1f)
                {
                    guiRect = new Rect(guiRect.x, guiRect.y, guiRect.width, targetHeight);
                }
            }

            // 拖动功能 - 仅在标题栏区域可拖动，避免与内部控件冲突
            GUI.DragWindow(new Rect(0, 0, guiRect.width, 30));
        }

        /// <summary>
        /// 尝试创建GUI面板，通过检测Spawning bodies状态判断是否合适
        /// 这个方法作为OnAfterLevelInitialized的备用机制
        /// </summary>
        bool TryCreateGUI()
        {
            // 统一检查游戏系统是否就绪且角色可以安全操作
            if (!IsGameReadyForGUI())
            {
                return false;
            }

            // 检查当前是否在主菜单或其他不支持Mod的界面
            if (IsInUnsupportedScene())
            {
                LogMessage("当前场景不支持Mod界面（如主菜单）");
                return false;
            }

            // 所有检查通过，创建GUI
            try
            {
                // 重置状态以确保正确初始化
                // cachedInputManager = null;
                // hasOriginalValues = false;

                // 创建GUI面板
                CreateGUIPanel();

                LogMessage("通过备用机制成功创建GUI面板");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"创建GUI时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 统一检查游戏系统是否就绪且角色可以安全操作
        /// 整合了基本系统检查和角色可操作性检查
        /// </summary>
        bool IsGameReadyForGUI()
        {
            try
            {
                // 1. 检查LevelManager是否存在
                if (LevelManager.Instance == null)
                {
                    LogMessage("LevelManager不存在，游戏系统未就绪");
                    return false;
                }

                // 2. 检查关卡是否初始化完成（这是关键条件）
                if (!LevelManager.AfterInit)
                {
                    LogMessage($"关卡初始化未完成 - LevelInitializing: {LevelManager.LevelInitializing}, LevelInited: {LevelManager.LevelInited}");
                    return false;
                }

                // 3. 检查是否正在加载子场景（通过LevelManager状态间接判断）
                if (LevelManager.LevelInitializing)
                {
                    LogMessage("关卡正在初始化中，暂时无法创建GUI");
                    return false;
                }

                // 4. 检查主角色是否存在且激活
                var mainCharacter = LevelManager.Instance.MainCharacter;
                if (mainCharacter == null || !mainCharacter.gameObject.activeInHierarchy)
                {
                    LogMessage("主角色不存在或未激活，无法创建GUI");
                    return false;
                }

                // 5. 检查核心角色组件是否完全初始化
                var main = CharacterMainControl.Main;
                if (main == null || main.CharacterItem == null || main.dashAction == null)
                {
                    LogMessage("角色组件未完全初始化，无法创建GUI");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"检查游戏就绪状态时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查当前是否在不支持Mod的界面（如主菜单）
        /// </summary>
        bool IsInUnsupportedScene()
        {
            try
            {
                // 检查当前场景名称
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                // 主菜单场景通常的名称
                string[] unsupportedScenes = { "MainMenu", "Menu", "Loading", "Intro" };

                foreach (string unsupported in unsupportedScenes)
                {
                    if (currentSceneName.Contains(unsupported, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // 检查角色是否在游戏中（是否有可操控的角色）
                var main = CharacterMainControl.Main;
                if (main == null || main.CharacterItem == null)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"检查场景支持性时发生异常: {ex.Message}");
                // 发生异常时保守处理，认为不支持
                return true;
            }
        }

        /// <summary>
        /// 绘制带编辑按钮的滑动条控件
        /// </summary>
        /// <param name="label">参数标签（中英文）</param>
        /// <param name="value">当前值</param>
        /// <param name="minValue">最小值</param>
        /// <param name="maxValue">最大值</param>
        /// <param name="format">数值格式</param>
        /// <param name="parameterIndex">参数索引，用于编辑对话框</param>
        /// <param name="parameterName">参数名称，用于编辑对话框标题</param>
        /// <returns>修改后的值</returns>
        float DrawSliderWithEditButton(string label, float value, float minValue, float maxValue, string format = "F1", int parameterIndex = -1, string parameterName = "")
        {
            GUILayout.BeginHorizontal();

            // 参数标签（恢复原始宽度）
            GUILayout.Label(label, GUILayout.Width(180));

            // 滑动条（恢复原始宽度）
            Rect sliderRect = GUILayoutUtility.GetRect(200, 20);
            float newValue = GUI.HorizontalSlider(sliderRect, value, minValue, maxValue);

            // 添加间距
            GUILayout.Space(5);

            // 检测鼠标悬停+R键重置 - 使用静态变量防止重复触发
            bool shouldResetToF1Default = false;
            // 只有在GUI.enabled为true（滑动条可用）时才检测R键重置
            // 并且使用静态变量确保每帧只触发一次
            int currentFrame = Time.frameCount;
            if (GUI.enabled && sliderRect.Contains(Event.current.mousePosition) &&
                Input.GetKeyDown(KeyCode.R) && !rKeyPressed && currentFrame != lastResetFrame)
            {
                if (parameterIndex >= 0)
                {
                    ResetSingleParameter(parameterIndex, parameterName);
                    shouldResetToF1Default = true;
                    rKeyPressed = true;
                    lastResetFrame = currentFrame;
                }
            }

            // 在R键释放时重置状态
            if (Input.GetKeyUp(KeyCode.R))
            {
                rKeyPressed = false;
            }

            // 值显示（支持点击）- 为整数参数添加%符号
            string valueText;
            if (parameterIndex == 3 || parameterIndex == 10) // 换弹加速百分比或回血比例
            {
                valueText = $"{(int)value}%";
            }
            else
            {
                valueText = value.ToString(format);
            }

            // 判断是否为默认值，设置不同颜色
            bool isDefaultValue = false;
            if (parameterIndex >= 0)
            {
                float defaultValue = GetDefaultValue(parameterIndex);
                // 对于整数参数，需要转换比较
                if (parameterIndex == 3 || parameterIndex == 10) // 百分比参数
                {
                    isDefaultValue = Math.Abs((int)value - (int)defaultValue) < 0.01f;
                }
                else
                {
                    isDefaultValue = Math.Abs(value - defaultValue) < 0.001f;
                }
            }

            // 获取按钮矩形区域，与滑动条对齐
            Rect buttonRect = GUILayoutUtility.GetRect(25, 20);
            // 向上偏移2像素
            buttonRect.y -= 2;

            // 根据是否为默认值设置颜色
            Color originalColor = GUI.color;
            if (isDefaultValue)
            {
                GUI.color = Color.white; // 默认值显示为白色
            }
            else
            {
                GUI.color = Color.green; // 修改值显示为绿色
            }

            // 手动绘制按钮，精确控制位置（无背景）
            bool isValueClicked = GUI.Button(buttonRect, valueText, GUI.skin.label);

            // 恢复原始颜色
            GUI.color = originalColor;

            GUILayout.EndHorizontal();

            // 检查是否点击了值显示
            if (isValueClicked && parameterIndex >= 0 && GUI.enabled)
            {
                ShowInputDialog(parameterName, label, value, minValue, maxValue, parameterIndex);
            }

            // 如果需要重置到默认值，返回默认值而不是newValue
            return shouldResetToF1Default ? GetDefaultValue(parameterIndex) : newValue;
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        void ShowInputDialog(string title, string label, float currentValue, float minValue, float maxValue, int parameterIndex)
        {
            showInputDialog = true;
            // 去掉冒号，用提示内容作为标题
            inputDialogTitle = label;

            // 根据参数类型决定范围显示格式
            string rangeFormat;
            string valueFormat;
            if (parameterIndex == 3 || parameterIndex == 10) // 换弹加速百分比或回血比例
            {
                rangeFormat = $"范围 / Range: {(int)minValue} - {(int)maxValue}";
                valueFormat = currentValue.ToString("F0");
            }
            else
            {
                rangeFormat = $"范围 / Range: {minValue:F1} - {maxValue:F1}";
                valueFormat = currentValue.ToString("F1");
            }

            inputDialogPrompt = rangeFormat;
            inputDialogValue = currentValue;
            inputDialogMinValue = minValue;
            inputDialogMaxValue = maxValue;
            currentEditingParameter = parameterIndex;
            inputDialogText = valueFormat;

            // 将对话框居中显示
            inputDialogRect.x = Screen.width / 2 - inputDialogRect.width / 2;
            inputDialogRect.y = Screen.height / 2 - inputDialogRect.height / 2;
        }

        /// <summary>
        /// 绘制模态输入对话框
        /// </summary>
        void DrawModalDialog()
        {
            if (!showInputDialog) return;

            // 保存当前GUI状态
            GUI.enabled = true;

            // 绘制半透明背景
            GUI.color = new Color(0, 0, 0, 0.7f); // 更深的半透明背景
            if (whiteTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
            }
            GUI.color = Color.white;

            // 绘制对话框窗口，并应用边界检测
            Rect oldRect = inputDialogRect;
            inputDialogRect = GUI.Window(99999, inputDialogRect, DrawDialogContent, inputDialogTitle);

            // 边界检测：确保窗口始终保持在屏幕可见范围内
            if (oldRect.x != inputDialogRect.x || oldRect.y != inputDialogRect.y)
            {
                // 窗口位置发生变化，应用边界约束
                inputDialogRect.x = Mathf.Clamp(inputDialogRect.x, 0, Screen.width - inputDialogRect.width);
                inputDialogRect.y = Mathf.Clamp(inputDialogRect.y, 0, Screen.height - inputDialogRect.height);
            }
        }

        /// <summary>
        /// 绘制对话框内容
        /// </summary>
        void DrawDialogContent(int windowId)
        {
            // 处理键盘输入
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    ConfirmInputDialog();
                    Event.current.Use();
                    return;
                }

                if (Event.current.keyCode == KeyCode.Escape)
                {
                    StartCoroutine(HandleESCKeyInterception());
                    CancelInputDialog();
                    Event.current.Use();
                    return;
                }
            }

  
            GUILayout.Space(8);

            // 范围提示（作为主要信息显示）
            GUILayout.Label(inputDialogPrompt, GUI.skin.box);
            GUILayout.Space(8);

            // 输入框
            GUI.SetNextControlName("InputField");
            inputDialogText = GUILayout.TextField(inputDialogText, GUILayout.Height(25));

            // 自动聚焦到输入框
            GUI.FocusControl("InputField");

            GUILayout.Space(12);

            // 按钮区域
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("确定 / OK", GUILayout.Width(100), GUILayout.Height(30)))
            {
                ConfirmInputDialog();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("取消 / Cancel", GUILayout.Width(100), GUILayout.Height(30)))
            {
                CancelInputDialog();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // 窗口可拖动 - 仅在标题栏区域可拖动，避免与控件冲突
            GUI.DragWindow(new Rect(0, 0, inputDialogRect.width, 20));
        }

        /// <summary>
        /// 确认输入对话框
        /// </summary>
        void ConfirmInputDialog()
        {
            if (float.TryParse(inputDialogText, out float newValue))
            {
                // 限制数值范围
                newValue = Mathf.Clamp(newValue, inputDialogMinValue, inputDialogMaxValue);

                // 根据参数索引更新对应的值
                UpdateParameterValue(currentEditingParameter, newValue);

                LogMessage($"参数已更新: {inputDialogTitle} = {newValue:F1}");
            }
            else
            {
                LogMessage($"输入值无效: {inputDialogText}");
            }

            CancelInputDialog();
        }

        /// <summary>
        /// 取消输入对话框
        /// </summary>
        void CancelInputDialog()
        {
            showInputDialog = false;
            currentEditingParameter = -1;
            inputDialogTitle = "";
            inputDialogPrompt = "";
            inputDialogText = "";
        }

        /// <summary>
        /// 根据参数索引更新参数值
        /// </summary>
        void UpdateParameterValue(int parameterIndex, float newValue)
        {
            switch (parameterIndex)
            {
                case 0: // 闪避距离倍数
                    dashDistanceMultiplier = newValue;
                    break;
                case 1: // 体力消耗
                    staminaCost = newValue;
                    break;
                case 2: // 冷却时间
                    coolTime = newValue;
                    break;
                case 3: // 换弹加速百分比 - 整数，四舍五入
                    dashReloadPercentage = (int)Mathf.Round(newValue);
                    break;
                case 4: // 步行速度倍数
                    walkSpeedMultiplier = newValue;
                    break;
                case 5: // 奔跑速度倍数
                    runSpeedMultiplier = newValue;
                    break;
                case 6: // 体力消耗率倍数
                    staminaDrainRateMultiplier = newValue;
                    break;
                case 7: // 体力恢复率倍数
                    staminaRecoverRateMultiplier = newValue;
                    break;
                case 8: // 体力恢复延迟倍数
                    staminaRecoverTimeMultiplier = newValue;
                    break;
                case 9: // 视野倍数
                    fovMultiplier = newValue;
                    break;
                case 10: // 回血比例 - 整数，四舍五入
                    healPercentage = (int)Mathf.Round(newValue);
                    break;
                case 11: // 最大回血量
                    maxHealAmount = newValue;
                    break;
                default:
                    LogMessage($"未知的参数索引: {parameterIndex}");
                    return;
            }

            // 应用修改
            ApplyModIfExists();
        }

        /// <summary>
        /// 获取布尔值的默认值
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>默认值</returns>
        bool GetBoolDefaultValue(string parameterName)
        {
            switch (parameterName)
            {
                case "闪避换弹": return false;
                case "开火打断换弹": return false;
                case "禁用移动惯性": return false;
                case "无限负重": return false;
                case "自定义视野": return false;
                case "击杀回血": return false;
                case "调试日志": return false;
                default: return false;
            }
        }

        /// <summary>
        /// 绘制带颜色的布尔值开关
        /// </summary>
        /// <param name="currentValue">当前值</param>
        /// <param name="parameterName">参数名称</param>
        /// <param name="options">GUILayout选项</param>
        /// <returns>新的开关值</returns>
        bool DrawColoredToggle(bool currentValue, string parameterName, params GUILayoutOption[] options)
        {
            bool defaultValue = GetBoolDefaultValue(parameterName);

            // 调试日志特殊处理，不改变颜色
            if (parameterName == "调试日志")
            {
                return GUILayout.Toggle(currentValue, currentValue ? "开启 / ON" : "关闭 / OFF", options);
            }

            // 使用自定义样式创建带颜色的开关
            string toggleText = currentValue ? "开启 / ON" : "关闭 / OFF";

            // 保存原始颜色
            Color originalColor = GUI.color;
            if (currentValue == defaultValue)
            {
                GUI.color = Color.white; // 默认值显示为白色
            }
            else
            {
                GUI.color = Color.green; // 修改值显示为绿色
            }

            // 绘制带颜色的开关
            bool newValue = GUILayout.Toggle(currentValue, toggleText, options);

            // 恢复颜色
            GUI.color = originalColor;

            return newValue;
        }

        /// <summary>
        /// 重置单个参数为默认值
        /// </summary>
        /// <param name="parameterIndex">参数索引</param>
        /// <param name="parameterName">参数名称，用于日志输出</param>
        void ResetSingleParameter(int parameterIndex, string parameterName)
        {
            float defaultValue = GetDefaultValue(parameterIndex);
            UpdateParameterValue(parameterIndex, defaultValue);
            LogMessage($"参数已重置: {parameterName} = {defaultValue:F1}");
        }

        /// <summary>
        /// 获取参数的默认值
        /// </summary>
        /// <param name="parameterIndex">参数索引</param>
        /// <returns>默认值</returns>
        float GetDefaultValue(int parameterIndex)
        {
            switch (parameterIndex)
            {
                case 0: // 闪避距离倍数
                    return DEFAULT_DASH_DISTANCE_MULTIPLIER;
                case 1: // 体力消耗
                    return DEFAULT_STAMINA_COST;
                case 2: // 冷却时间
                    return DEFAULT_COOL_TIME;
                case 3: // 换弹加速百分比
                    return DEFAULT_DASH_RELOAD_PERCENTAGE;
                case 4: // 步行速度倍数
                    return DEFAULT_WALK_SPEED_MULTIPLIER;
                case 5: // 奔跑速度倍数
                    return DEFAULT_RUN_SPEED_MULTIPLIER;
                case 6: // 体力消耗率倍数
                    return DEFAULT_STAMINA_DRAIN_RATE_MULTIPLIER;
                case 7: // 体力恢复率倍数
                    return DEFAULT_STAMINA_RECOVER_RATE_MULTIPLIER;
                case 8: // 体力恢复延迟倍数
                    return DEFAULT_STAMINA_RECOVER_TIME_MULTIPLIER;
                case 9: // 视野倍数
                    return DEFAULT_FOV_MULTIPLIER;
                case 10: // 回血比例
                    return DEFAULT_HEAL_PERCENTAGE;
                case 11: // 最大回血量
                    return DEFAULT_MAX_HEAL_AMOUNT;
                default:
                    LogMessage($"未知的参数索引: {parameterIndex}");
                    return 0f;
            }
        }

        void DrawDashTab()
        {
            GUILayout.Label("=== 闪避参数 / Dash Parameters ===", GUI.skin.box);
            GUILayout.Space(5);

            // 闪避距离倍数
            float newDashMultiplier = DrawSliderWithEditButton(
                "闪避距离倍数 / Dash Distance",
                dashDistanceMultiplier, 0.1f, 5.0f, "F1", 0, "闪避距离倍数"
            );

            if (newDashMultiplier != dashDistanceMultiplier)
            {
                dashDistanceMultiplier = newDashMultiplier;
                ApplyModIfExists();
            }

            // 体力消耗
            float newStamina = DrawSliderWithEditButton(
                "体力消耗 / Stamina Cost",
                staminaCost, 0f, 50f, "F1", 1, "体力消耗"
            );

            if (newStamina != staminaCost)
            {
                staminaCost = newStamina;
                ApplyModIfExists();
            }

            // 冷却时间
            float newCoolTime = DrawSliderWithEditButton(
                "冷却时间(秒) / Cooldown (s)",
                coolTime, 0f, 5f, "F2", 2, "冷却时间"
            );

            if (newCoolTime != coolTime)
            {
                coolTime = newCoolTime;
                ApplyModIfExists();
            }

            // 闪避换弹开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("闪避换弹 / Dash Reload:", GUILayout.Width(180));
            bool newDashReload = DrawColoredToggle(enableDashReload, "闪避换弹", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newDashReload != enableDashReload)
            {
                enableDashReload = newDashReload;
                LogMessage($"闪避换弹功能: {(enableDashReload ? "启用" : "禁用")}");
            }

            // 换弹加速百分比滑动条
            // 根据闪避换弹开关状态设置GUI是否可用
            GUI.enabled = enableDashReload;

            float newPercentage = DrawSliderWithEditButton(
                "换弹加速 / Reload Speed",
                dashReloadPercentage, 0f, 100f, "F0", 3, "换弹加速百分比"
            );

            GUI.enabled = true; // 恢复GUI状态

            if (Math.Abs(newPercentage - dashReloadPercentage) > 0.5f)
            {
                dashReloadPercentage = (int)newPercentage;
                LogMessage($"闪避换弹加速: {dashReloadPercentage}%");
            }

            GUILayout.Space(10);
            GUILayout.Label("=== 其他参数 / Other Parameters ===", GUI.skin.box);
            GUILayout.Space(5);

            // 射击打断换弹开关 UI上使用"开火"，代码内部使用"射击"
            GUILayout.BeginHorizontal();
            GUILayout.Label("开火打断换弹 / Shoot Interrupt:", GUILayout.Width(180));
            bool newShootInterrupt = DrawColoredToggle(enableShootInterruptReload, "开火打断换弹", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newShootInterrupt != enableShootInterruptReload)
            {
                enableShootInterruptReload = newShootInterrupt;
                LogMessage($"射击打断换弹功能: {(enableShootInterruptReload ? "启用" : "禁用")}");
            }
        }

        void DrawRunTab()
        {
            GUILayout.Label("=== 奔跑参数 / Run Parameters ===", GUI.skin.box);
            GUILayout.Space(5);

            // 步行速度倍数
            float newWalkMultiplier = DrawSliderWithEditButton(
                "步行速度倍数 / Walk Speed",
                walkSpeedMultiplier, 1f, 5.0f, "F1", 4, "步行速度倍数"
            );

            if (newWalkMultiplier != walkSpeedMultiplier)
            {
                walkSpeedMultiplier = newWalkMultiplier;
                ApplyModIfExists();
            }

            // 奔跑速度倍数
            float newRunMultiplier = DrawSliderWithEditButton(
                "奔跑速度倍数 / Run Speed",
                runSpeedMultiplier, 1f, 5.0f, "F1", 5, "奔跑速度倍数"
            );

            if (newRunMultiplier != runSpeedMultiplier)
            {
                runSpeedMultiplier = newRunMultiplier;
                ApplyModIfExists();
            }

            // 体力消耗率倍数
            float newDrainMultiplier = DrawSliderWithEditButton(
                "体力消耗率倍数 / Stamina Drain",
                staminaDrainRateMultiplier, 0, 5.0f, "F1", 6, "体力消耗率倍数"
            );

            if (newDrainMultiplier != staminaDrainRateMultiplier)
            {
                staminaDrainRateMultiplier = newDrainMultiplier;
                ApplyModIfExists();
            }

            // 体力恢复率倍数
            float newRecoverMultiplier = DrawSliderWithEditButton(
                "体力恢复率倍数 / Stamina Recover",
                staminaRecoverRateMultiplier, 1f, 5.0f, "F1", 7, "体力恢复率倍数"
            );

            if (newRecoverMultiplier != staminaRecoverRateMultiplier)
            {
                staminaRecoverRateMultiplier = newRecoverMultiplier;
                ApplyModIfExists();
            }

            // 体力恢复延迟倍数
            float newRecoverTimeMultiplier = DrawSliderWithEditButton(
                "体力恢复延迟倍数 / Recover Delay",
                staminaRecoverTimeMultiplier, 0, 5.0f, "F1", 8, "体力恢复延迟倍数"
            );

            if (newRecoverTimeMultiplier != staminaRecoverTimeMultiplier)
            {
                staminaRecoverTimeMultiplier = newRecoverTimeMultiplier;
                ApplyModIfExists();
            }
        }

        void DrawSettingsTab()
        {
            GUILayout.Label("=== 其他设置 / Other Settings ===", GUI.skin.box);
            GUILayout.Space(5);

            // 惯性开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("禁用移动惯性 / Disable Inertia:", GUILayout.Width(200));
            bool newDisableInertia = DrawColoredToggle(disableMovementInertia, "禁用移动惯性", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newDisableInertia != disableMovementInertia)
            {
                disableMovementInertia = newDisableInertia;
                ApplyModIfExists();
            }

            // 无限负重开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("无限负重 / Infinite Weight:", GUILayout.Width(200));
            bool newInfiniteWeight = DrawColoredToggle(enableInfiniteWeight, "无限负重", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newInfiniteWeight != enableInfiniteWeight)
            {
                enableInfiniteWeight = newInfiniteWeight;
                ApplyModIfExists();
            }

            GUILayout.Space(10);

            GUILayout.Label("=== 调试设置 / Debug Settings ===", GUI.skin.box);
            GUILayout.Space(5);

            // 日志开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("调试日志 / Debug Logging:", GUILayout.Width(200));
            bool newLogging = DrawColoredToggle(enableLogging, "调试日志", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newLogging != enableLogging)
            {
                enableLogging = newLogging;
                LogMessage($"日志输出已{(enableLogging ? "开启" : "关闭")}");
            }
        }

        void DrawFOVTab()
        {
            GUILayout.Label("=== 视野设置 / FOV Settings ===", GUI.skin.box);
            GUILayout.Space(5);

            // 自定义视野开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("自定义视野 / Custom FOV:", GUILayout.Width(200));
            bool newCustomFOV = DrawColoredToggle(enableCustomFOV, "自定义视野", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newCustomFOV != enableCustomFOV)
            {
                enableCustomFOV = newCustomFOV;
                ApplyModIfExists();
            }

            GUILayout.Space(10);

            // 视野倍数滑块 - 仅在启用自定义视野时可用
            GUI.enabled = enableCustomFOV; // 禁用状态下变灰
            float newFOVMultiplier = DrawSliderWithEditButton(
                "视野倍数 / FOV Multiplier",
                fovMultiplier, 0.2f, 3.0f, "F1", 9, "视野倍数"
            );
            GUI.enabled = true; // 恢复启用状态

            if (newFOVMultiplier != fovMultiplier && enableCustomFOV)
            {
                fovMultiplier = newFOVMultiplier;
                ApplyModIfExists();
            }

            GUILayout.Space(10);

            // 操作提示 - 根据自定义视野状态决定是否变灰
            GUI.enabled = enableCustomFOV; // 启用状态与自定义视野开关一致
            GUILayout.Label("提示：Ctrl+鼠标滚轮调整视野，Ctrl+鼠标中键还原默认", GUI.skin.box);
            GUILayout.Label("Tip: Ctrl+Mouse Wheel to adjust, Ctrl+Mouse Middle Button to reset", GUI.skin.box);
            GUI.enabled = true; // 恢复启用状态
        }

        void DrawHealTab()
        {
            GUILayout.Label("=== 击杀回血设置 / Kill Heal Settings ===", GUI.skin.box);
            GUILayout.Space(5);

            // 击杀回血开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("启用击杀回血 / Enable Kill Heal:", GUILayout.Width(200));
            bool newKillHeal = DrawColoredToggle(enableKillHeal, "击杀回血", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newKillHeal != enableKillHeal)
            {
                enableKillHeal = newKillHeal;
                LogMessage($"击杀回血功能: {(enableKillHeal ? "启用" : "禁用")}");
            }

            GUILayout.Space(10);

            // 回血比例滑块 - 仅在启用击杀回血时可用
            GUI.enabled = enableKillHeal;
            float newHealPercentage = DrawSliderWithEditButton(
                "回血比例 / Heal Percentage",
                healPercentage, 0, 100, "F0", 10, "回血比例"
            );

            if (Math.Abs(newHealPercentage - healPercentage) > 0.5f)
            {
                healPercentage = (int)newHealPercentage;
                LogMessage($"回血比例调整为: {healPercentage}%");
            }

            // 最大回血量滑块
            float newMaxHeal = DrawSliderWithEditButton(
                "最大回血量 / Max Heal Amount",
                maxHealAmount, 1f, 200f, "F1", 11, "最大回血量"
            );

            if (Math.Abs(newMaxHeal - maxHealAmount) > 0.5f)
            {
                maxHealAmount = newMaxHeal;
                LogMessage($"最大回血量调整为: {maxHealAmount:F1}");
            }

            GUI.enabled = true; // 恢复启用状态

            GUILayout.Space(10);

            // 说明文字
            GUILayout.Label("--- 功能说明 / Description ---", GUI.skin.box);
            GUILayout.Space(5);

            GUI.enabled = enableKillHeal;
            GUILayout.Label("• 击杀敌人时回复血量", GUI.skin.box);
            GUILayout.Label("• 回血量 = 敌人最大血量 × 设定比例", GUI.skin.box);
            GUILayout.Label("• 受最大回血量限制", GUI.skin.box);
            GUILayout.Label("• Heal health when killing enemies", GUI.skin.box);
            GUILayout.Label("• Heal amount = enemy max health × percentage", GUI.skin.box);
            GUILayout.Label("• Limited by max heal amount", GUI.skin.box);
            GUI.enabled = true;
        }

        void ResetAllParameters()
        {
            // 重置闪避参数
            dashDistanceMultiplier = 1.0f;
            staminaCost = 10f;
            coolTime = 0.5f;

            // 重置奔跑参数
            walkSpeedMultiplier = 1.0f;
            runSpeedMultiplier = 1.0f;
            staminaDrainRateMultiplier = 1.0f;
            staminaRecoverRateMultiplier = 1.0f;
            staminaRecoverTimeMultiplier = 1.0f;

            // 重置移动手感参数
            disableMovementInertia = false;

            // 重置负重参数
            enableInfiniteWeight = false;

            // 重置视野参数
            enableCustomFOV = false;
            fovMultiplier = 1.0f;

            // 重置闪避换弹参数
            enableDashReload = false;
            dashReloadPercentage = 0;
            enableShootInterruptReload = false;

            // 重置击杀回血参数
            enableKillHeal = false;
            healPercentage = 5;
            maxHealAmount = 50.0f;

            SaveSettings();
            ApplyModIfExists();
            LogMessage("所有参数已恢复默认设置");
        }

        protected override void OnBeforeDeactivate()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            // 取消订阅关卡加载完成事件
            LevelManager.OnAfterLevelInitialized -= OnLevelFullyLoaded;

            // 取消订阅击杀事件
            Health.OnDead -= OnEnemyKilled;

            // 清理滚轮输入拦截系统
            CleanupScrollWheelInterception();

            // 如果GUI正在显示，先恢复输入控制
            if (guiPanel != null) InputManager.ActiveInput(guiPanel);

            // 恢复原始值
            if (hasOriginalValues && CharacterMainControl.Main?.dashAction != null)
            {
                var main = CharacterMainControl.Main;
                var dash = main.dashAction;

                // 恢复闪避参数原始值
                if (originalSpeedCurve != null && dashDistanceMultiplier != 1.0f)
                {
                    dash.speedCurve = originalSpeedCurve;
                }

                dash.staminaCost = originalStaminaCost;
                dash.coolTime = originalCoolTime;

                // 恢复奔跑参数原始值
                if (main.CharacterItem != null)
                {
                    if (walkSpeedMultiplier != 1.0f && originalWalkSpeed > 0)
                    {
                        var walkStat = main.CharacterItem.GetStat("WalkSpeed".GetHashCode());
                        if (walkStat != null)
                        {
                            walkStat.BaseValue = originalWalkSpeed;
                            LogMessage($"步行速度已恢复: {originalWalkSpeed:F2}");
                        }
                    }

                    if (runSpeedMultiplier != 1.0f && originalRunSpeed > 0)
                    {
                        var runStat = main.CharacterItem.GetStat("RunSpeed".GetHashCode());
                        if (runStat != null)
                        {
                            runStat.BaseValue = originalRunSpeed;
                            LogMessage($"奔跑速度已恢复: {originalRunSpeed:F2}");
                        }
                    }

                    if (staminaDrainRateMultiplier != 1.0f && originalStaminaDrainRate > 0)
                    {
                        var drainStat = main.CharacterItem.GetStat("StaminaDrainRate".GetHashCode());
                        if (drainStat != null)
                        {
                            drainStat.BaseValue = originalStaminaDrainRate;
                            LogMessage($"体力消耗率已恢复: {originalStaminaDrainRate:F2}");
                        }
                    }

                    if (staminaRecoverRateMultiplier != 1.0f && originalStaminaRecoverRate > 0)
                    {
                        var recoverStat = main.CharacterItem.GetStat("StaminaRecoverRate".GetHashCode());
                        if (recoverStat != null)
                        {
                            recoverStat.BaseValue = originalStaminaRecoverRate;
                            LogMessage($"体力恢复率已恢复: {originalStaminaRecoverRate:F2}");
                        }
                    }

                    if (staminaRecoverTimeMultiplier != 1.0f && originalStaminaRecoverTime > 0)
                    {
                        var recoverTimeStat = main.CharacterItem.GetStat("StaminaRecoverTime".GetHashCode());
                        if (recoverTimeStat != null)
                        {
                            recoverTimeStat.BaseValue = originalStaminaRecoverTime;
                            LogMessage($"体力恢复延迟已恢复: {originalStaminaRecoverTime:F2}");
                        }
                    }

                    // 恢复移动惯性原始值
                    if (disableMovementInertia && originalWalkAcc > 0)
                    {
                        var walkAccStat = main.CharacterItem.GetStat("WalkAcc".GetHashCode());
                        if (walkAccStat != null)
                        {
                            walkAccStat.BaseValue = originalWalkAcc;
                            LogMessage($"步行加速度已恢复: {originalWalkAcc:F2}");
                        }
                    }

                    if (disableMovementInertia && originalRunAcc > 0)
                    {
                        var runAccStat = main.CharacterItem.GetStat("RunAcc".GetHashCode());
                        if (runAccStat != null)
                        {
                            runAccStat.BaseValue = originalRunAcc;
                            LogMessage($"奔跑加速度已恢复: {originalRunAcc:F2}");
                        }
                    }

                    // 恢复负重原始值
                    if (enableInfiniteWeight && originalMaxWeight > 0)
                    {
                        var maxWeightStat = main.CharacterItem.GetStat("MaxWeight".GetHashCode());
                        if (maxWeightStat != null)
                        {
                            maxWeightStat.BaseValue = originalMaxWeight;
                            LogMessage($"最大负重已恢复: {originalMaxWeight:F2}");
                        }
                    }
                }

                // 恢复视野原始值
                var gameCamera = GameCamera.Instance;
                if (gameCamera != null && enableCustomFOV && originalDefaultFOV > 0)
                {
                    if (gameCamera.defaultFOV != originalDefaultFOV)
                    {
                        gameCamera.defaultFOV = originalDefaultFOV;
                        LogMessage($"默认视野已恢复: {originalDefaultFOV:F2}");
                    }

                    if (gameCamera.adsFOV != originalAdsFOV)
                    {
                        gameCamera.adsFOV = originalAdsFOV;
                        LogMessage($"瞄准视野已恢复: {originalAdsFOV:F2}");
                    }
                }

                LogMessage("所有参数已恢复原始值");
            }
            LogMessage("再见鸭！");

            base.OnBeforeDeactivate();
        }

        void HandleDashReload()
        {
            var main = CharacterMainControl.Main;
            if (main == null || main.dashAction == null) return;

            bool isDashing = main.dashAction.Running;

            // 检测闪避开始
            if (isDashing && !wasDashing)
            {
                dashReloadTriggered = false; // 重置本次闪避的换弹触发标志
                dashReloadIntent = false; // 重置换弹意图
                dashStartTime = Time.time; // 记录闪避开始时间
                SaveWeaponReloadTime(main); // 保存武器换弹时间
            }

            // 闪避期间尝试换弹（但只记录意图，不执行）
            if (isDashing && !dashReloadTriggered)
            {
                if (CanReloadDuringDash())
                {
                    dashReloadIntent = true; // 记录换弹意图
                    LogMessage("闪避期间记录换弹意图");
                }
                else
                {
                    LogMessage("闪避期间无法换弹");
                }
                dashReloadTriggered = true;
            }

            // 闪避结束，执行时间累积换弹
            if (!isDashing && wasDashing)
            {
                dashReloadTriggered = false;
                if (dashReloadIntent)
                {
                    ExecuteAccumulatedReload(main);
                }
                dashReloadIntent = false; // 重置意图
            }

            wasDashing = isDashing;
        }

        void SaveWeaponReloadTime(CharacterMainControl main)
        {
            var gun = main.agentHolder?.CurrentHoldGun;
            if (gun == null) return;

            try
            {
                var reloadTimeProperty = gun.GetType().GetProperty("ReloadTime");
                if (reloadTimeProperty != null)
                {
                    originalReloadTime = (float)reloadTimeProperty.GetValue(gun);
                    LogMessage($"保存武器换弹时间: {originalReloadTime:F2}s");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"保存换弹时间异常: {ex.Message}");
            }
        }

        bool CanReloadDuringDash()
        {
            var main = CharacterMainControl.Main;
            if (main == null) return false;

            // 检查是否有装备枪械 - 使用 agentHolder.CurrentHoldGun 来检查
            var gun = main.agentHolder?.CurrentHoldGun;
            if (gun == null)
            {
                LogMessage("未装备枪械，无法换弹");
                return false;
            }

            // 检查枪械状态是否允许换弹
            // 使用反射获取 GunState 属性
            var gunStateProperty = gun.GetType().GetProperty("GunState");
            if (gunStateProperty == null)
            {
                LogMessage("无法获取枪械状态信息");
                return false;
            }

            var gunState = gunStateProperty.GetValue(gun);
            string stateName = gunState.ToString();

            // 允许换弹的状态：ready, empty, shootCooling
            if (stateName != "ready" && stateName != "empty" && stateName != "shootCooling")
            {
                LogMessage($"枪械状态不允许换弹: {stateName}");
                return false;
            }

            // 检查是否已经在换弹
            var isReloadingMethod = gun.GetType().GetMethod("IsReloading");
            if (isReloadingMethod != null && (bool)isReloadingMethod.Invoke(gun, null))
            {
                LogMessage("已经在换弹中");
                return false;
            }

            return true;
        }

        void ExecuteAccumulatedReload(CharacterMainControl main)
        {
            var gun = main.agentHolder?.CurrentHoldGun;
            if (gun == null) return;

            try
            {
                // 使用动作系统启动换弹，确保可中断性
                if (main.reloadAction != null && main.reloadAction.IsReady())
                {
                    // 启动换弹动作
                    main.StartAction(main.reloadAction);
                    // 在下一帧应用时间加速（确保动作系统已正确初始化）
                    StartCoroutine(ApplyTimeAccumulatedReductionDelayed(gun));
                }
                else
                {
                    LogMessage("换弹动作未准备好，无法执行时间累积换弹");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"时间累积换弹异常: {ex.Message}");
            }
        }

        void ApplyTimeAccumulatedReduction(object gun)
        {
            try
            {
                // 获取换弹时间和状态计时器
                var reloadTimeProperty = gun.GetType().GetProperty("ReloadTime");
                var stateTimerField = gun.GetType().GetField("stateTimer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (reloadTimeProperty == null || stateTimerField == null) return;

                float reloadTime = originalReloadTime;

                // 使用百分比计算最终的加速时间
                // 0% = 原始累积时间 (elapsedTime), 100% = 完全跳过时间 (reloadTime)
                float elapsedTime = Time.time - dashStartTime;
                float acceleratedTime = elapsedTime + (reloadTime - elapsedTime) * (dashReloadPercentage / 100f);

                stateTimerField.SetValue(gun, acceleratedTime);

                LogMessage($"时间累积换弹加速: 跳到 {acceleratedTime:F2}s，加速 {dashReloadPercentage}%，原始累积 {elapsedTime:F2}s");
            }
            catch (Exception ex)
            {
                LogMessage($"时间累积换弹加速异常: {ex.Message}");
            }
        }

        IEnumerator ApplyTimeAccumulatedReductionDelayed(object gun)
        {
            // 等待一帧，确保动作系统已正确初始化换弹状态
            yield return null;

            // 现在安全地应用时间加速
            ApplyTimeAccumulatedReduction(gun);
        }

        // 射击打断换弹处理（优化版本）
        void HandleShootInterruptReload()
        {
            try
            {
                var main = CharacterMainControl.Main;
                if (main == null || main.agentHolder?.CurrentHoldGun == null) return;

                var gun = main.agentHolder.CurrentHoldGun;

                // 检查是否在换弹状态
                var isReloadingMethod = gun.GetType().GetMethod("IsReloading");
                if (isReloadingMethod == null) return;

                bool isReloading = (bool)isReloadingMethod.Invoke(gun, null);

                // 检测换弹状态变化
                if (isReloading && !isCurrentlyReloading)
                {
                    // 开始换弹，重置检查标志
                    isCurrentlyReloading = true;
                    reloadStartTime = Time.time; // 记录换弹开始时间
                    reloadInterruptChecked = false;
                    isEmptyClipAutoReload = false; // 重置空弹夹标志
                    LogMessage("开始换弹，准备检测打断");
                }
                else if (!isReloading && isCurrentlyReloading)
                {
                    // 换弹结束，清理状态
                    isCurrentlyReloading = false;
                    reloadStartTime = 0f;
                    reloadInterruptChecked = false;
                    isEmptyClipAutoReload = false;
                    LogMessage("换弹结束");
                    return;
                }

                // 如果不在换弹状态，直接返回
                if (!isCurrentlyReloading) return;

                // 如果已经检查过本次换弹，直接返回
                if (reloadInterruptChecked) return;

                // 检查弹夹是否有子弹，标记是否为空弹夹自动换弹
                if (!isEmptyClipAutoReload && !reloadInterruptChecked)
                {
                    var bulletCountProperty = gun.GetType().GetProperty("BulletCount");
                    if (bulletCountProperty != null)
                    {
                        int bulletCount = (int)bulletCountProperty.GetValue(gun);
                        if (bulletCount <= 0)
                        {
                            // 标记这是空弹夹自动换弹，但不跳过打断检查
                            isEmptyClipAutoReload = true;
                            LogMessage("检测到空弹夹自动换弹");
                        }
                    }
                }

                // 如果是空弹夹自动换弹，延迟一小段时间再检查打断
                // 这样可以避免立即打断原版的自动换弹
                if (isEmptyClipAutoReload)
                {
                    float timeSinceReload = Time.time - reloadStartTime;
                    if (timeSinceReload < MIN_INTERRUPT_DELAY)
                    {
                        // 延迟期间需要更新lastFireInputState来避免误判
                        lastFireInputState = HasFireInputFromInputManagerOptimized();
                        return;
                    }
                }

                // 检查是否有开火输入
                bool currentFireInput = HasFireInputFromInputManagerOptimized();

                // 检测新的开火输入（从false变为true），而不是持续按着
                bool hasNewFireInput = currentFireInput && !lastFireInputState;
                lastFireInputState = currentFireInput; // 更新状态

                if (hasNewFireInput)
                {
                    CallOriginalStopActionOptimized(main);
                    LogMessage("射击键打断换弹成功");
                    reloadInterruptChecked = true; // 标记已检查过，避免重复检查
                }
            }
            catch (Exception ex)
            {
                LogMessage($"射击打断换弹异常: {ex.Message}");
            }
        }

        // 从InputManager检查开火输入（优化版本）
        bool HasFireInputFromInputManagerOptimized()
        {
            try
            {
                // 如果还没有缓存inputManager，先获取并缓存
                if (cachedInputManager == null)
                {
                    var characterInputControlType = Type.GetType("CharacterInputControl, TeamSoda.Duckov.Core");
                    if (characterInputControlType == null) return false;

                    var instanceProperty = characterInputControlType.GetProperty("Instance");
                    var characterInputControl = instanceProperty?.GetValue(null);
                    if (characterInputControl == null) return false;

                    var inputManagerField = characterInputControlType.GetField("inputManager",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    cachedInputManager = inputManagerField?.GetValue(characterInputControl);
                }

                if (cachedInputManager == null) return false;

                // 获取triggerInput状态（是私有字段，不是属性）
                var triggerInputField = cachedInputManager.GetType().GetField("triggerInput",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (triggerInputField == null) return false;

                bool triggerInput = (bool)triggerInputField.GetValue(cachedInputManager);
                return triggerInput;
            }
            catch (Exception ex)
            {
                LogMessage($"检查开火输入异常: {ex.Message}");
                return false;
            }
        }

        // 调用原版的StopAction（优化版本）
        void CallOriginalStopActionOptimized(CharacterMainControl main)
        {
            try
            {
                // 使用缓存的inputManager
                if (cachedInputManager != null)
                {
                    var stopActionMethod = cachedInputManager.GetType().GetMethod("StopAction");
                    if (stopActionMethod != null)
                    {
                        stopActionMethod.Invoke(cachedInputManager, null);
                        LogMessage("StopAction调用成功（优化版本）");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"调用StopAction异常: {ex.Message}");
            }
        }

        // 击杀回血回调方法
        void OnEnemyKilled(Health killedHealth, DamageInfo damageInfo)
        {
            if (!enableKillHeal) return;

            // 检查是否为玩家击杀
            if (damageInfo.fromCharacter == null || !damageInfo.fromCharacter.IsMainCharacter)
            {
                return;
            }

            // 检查受害者是否为敌人（不是玩家队伍）
            if (killedHealth.team == Teams.player)
            {
                return;
            }

            // 获取玩家角色
            var mainCharacter = CharacterMainControl.Main;
            if (mainCharacter == null)
            {
                return;
            }

            // 计算回血量
            float healAmount = killedHealth.MaxHealth * (healPercentage / 100.0f);
            healAmount = Mathf.Min(healAmount, maxHealAmount);

            if (healAmount <= 0)
            {
                return;
            }

            // 给玩家回血
            var playerHealth = mainCharacter.GetComponent<Health>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                float currentHealth = playerHealth.CurrentHealth;
                float maxHealth = playerHealth.MaxHealth;

                // 确保不会超过最大血量
                healAmount = Mathf.Min(healAmount, maxHealth - currentHealth);

                if (healAmount > 0)
                {
                    playerHealth.AddHealth(healAmount);
                    LogMessage($"击杀回血: +{healAmount:F1} HP (当前: {currentHealth:F1}/{maxHealth:F1})");
                }
            }
        }

        void LogMessage(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[DashPlus] {message}");
            }
        }

        private IEnumerator HandleESCKeyInterception()
        {
            yield return null;
            if (PauseMenu.Instance != null && PauseMenu.Instance.Shown)
            {
                PauseMenu.Hide();
                LogMessage("ESC拦截：关闭游戏暂停菜单");
            }
        }

        /// <summary>
        /// 隐藏准心
        /// </summary>
        void HideAimMarker()
        {
            if (aimMarkerHidden)
            {
                LogMessage("准心已经隐藏，跳过重复操作");
                return;
            }

            try
            {
                var aimMarker = FindCurrentAimMarker();
                if (aimMarker != null)
                {
                    aimMarker.rootCanvasGroup.alpha = 0f;
                    cachedAimMarker = aimMarker;
                    aimMarkerHidden = true;
                    LogMessage("准心已隐藏");
                }
                else
                {
                    LogMessage("无法找到准心组件");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"隐藏准心时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示准心
        /// </summary>
        void ShowAimMarker()
        {
            if (!aimMarkerHidden)
            {
                LogMessage("准心已经显示，跳过重复操作");
                return;
            }

            try
            {
                var aimMarker = FindCurrentAimMarker();
                if (aimMarker != null || cachedAimMarker != null)
                {
                    // 优先使用缓存的准星，如果不存在则重新查找
                    var target = aimMarker ?? cachedAimMarker;
                    if (target != null)
                    {
                        target.rootCanvasGroup.alpha = 1f;
                        aimMarkerHidden = false;
                        LogMessage("准心已显示");
                    }
                }
                else
                {
                    LogMessage("无法找到准心组件");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"显示准心时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 查找当前场景中的 AimMarker 组件
        /// </summary>
        AimMarker? FindCurrentAimMarker()
        {
            // 检查 LevelManager 是否发生变化
            if (LevelManager.Instance != lastKnownLevelManager)
            {
                lastKnownLevelManager = LevelManager.Instance;
                cachedAimMarker = null; // 清除缓存，强制重新查找
                LogMessage("LevelManager 已变更，清除准星缓存");
            }

            // 如果有缓存的准星且仍然有效，直接返回
            if (cachedAimMarker != null && cachedAimMarker.gameObject != null)
            {
                return cachedAimMarker;
            }

            // 重新查找准星
            if (LevelManager.Instance != null)
            {
                var aimMarkers = LevelManager.Instance.GetComponentsInChildren<AimMarker>(true);
                if (aimMarkers.Length > 0)
                {
                    cachedAimMarker = aimMarkers[0];
                    LogMessage($"找到准星组件: {cachedAimMarker.gameObject.name}");
                    return cachedAimMarker;
                }
            }

            // 备用方案：全局查找
            var globalAimMarkers = FindObjectsOfType<AimMarker>();
            if (globalAimMarkers.Length > 0)
            {
                cachedAimMarker = globalAimMarkers[0];
                LogMessage($"通过全局查找找到准星组件: {cachedAimMarker.gameObject.name}");
                return cachedAimMarker;
            }

            LogMessage("未找到任何准星组件");
            return null;
        }
    }
}
