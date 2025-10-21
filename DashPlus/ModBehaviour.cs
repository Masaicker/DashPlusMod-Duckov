using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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

        [Header("调试设置")]
        [Tooltip("是否输出调试日志")] public bool enableLogging = false;

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

        // 标签页控制
        private int selectedTab = 0; // 0: 闪避, 1: 奔跑, 2: 视野, 3: 其他设置
        private readonly string[] tabNames = { "闪避 / Dash", "奔跑 / Run", "视野 / FOV", "其他 / Others" };

        // FOV滚轮调整相关
        private bool isScrollingFOV = false;
        private float lastScrollTime = 0f;
        private const float SCROLL_END_DELAY = 0.5f; // 滚轮停止后延迟保存时间

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

        protected override void OnAfterSetup()
        {
            base.OnAfterSetup();
            SceneManager.sceneLoaded += OnSceneLoaded;

            LoadSettings();
            Invoke(nameof(ApplyModIfExists), 1f);
        }

        void Update()
        {
            // 检查快捷键：Ctrl+G 显示/隐藏GUI
            if (Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftControl))
            {
                showGUI = !showGUI;
                LogMessage($"GUI {(showGUI ? "显示" : "隐藏")}");
            }

            // 检查快捷键：Ctrl+滚轮调整FOV
            if (Input.GetKey(KeyCode.LeftControl) && enableCustomFOV)
            {
                float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scrollWheel) > 0.01f)
                {
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

            // 检查滚轮是否停止，如果停止则保存设置
            if (isScrollingFOV && Time.time - lastScrollTime > SCROLL_END_DELAY)
            {
                isScrollingFOV = false;
                SaveSettings();
                LogMessage($"FOV倍数调整为: {targetFOVValue:F1}x");
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
            if (enableDashReload)
            {
                HandleDashReload();
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LogMessage($"场景切换: {scene.name}");
            ApplyModIfExists();
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

                // 保存所有CharacterItem相关参数的原始值 - 使用 GetStat 的 BaseValue 以保持一致性
                if (main.CharacterItem != null)
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
                    originalWalkSpeed = walkStat?.BaseValue ?? main.CharacterWalkSpeed;
                    originalRunSpeed = runStat?.BaseValue ?? main.CharacterRunSpeed;
                    originalStaminaDrainRate = drainStat?.BaseValue ?? main.StaminaDrainRate;
                    originalStaminaRecoverRate = recoverStat?.BaseValue ?? main.StaminaRecoverRate;
                    originalStaminaRecoverTime = recoverTimeStat?.BaseValue ?? main.StaminaRecoverTime;

                    // 移动惯性原始值
                    originalWalkAcc = walkAccStat?.BaseValue ?? main.CharacterWalkAcc;
                    originalRunAcc = runAccStat?.BaseValue ?? main.CharacterRunAcc;

                    // 负重原始值
                    originalMaxWeight = maxWeightStat?.BaseValue ?? main.MaxWeight;

                    // 视野原始值
                    var gameCamera = GameCamera.Instance;
                    if (gameCamera != null)
                    {
                        originalDefaultFOV = gameCamera.defaultFOV;
                        originalAdsFOV = gameCamera.adsFOV;
                    }
                }
                else
                {
                    // 备用方案：直接从 CharacterMainControl 获取所有参数
                    originalWalkSpeed = main.CharacterWalkSpeed;
                    originalRunSpeed = main.CharacterRunSpeed;
                    originalStaminaDrainRate = main.StaminaDrainRate;
                    originalStaminaRecoverRate = main.StaminaRecoverRate;
                    originalStaminaRecoverTime = main.StaminaRecoverTime;
                    originalWalkAcc = main.CharacterWalkAcc;
                    originalRunAcc = main.CharacterRunAcc;
                    originalMaxWeight = main.MaxWeight;

                    // 视野原始值备用方案
                    var gameCamera = GameCamera.Instance;
                    if (gameCamera != null)
                    {
                        originalDefaultFOV = gameCamera.defaultFOV;
                        originalAdsFOV = gameCamera.adsFOV;
                    }
                }

                hasOriginalValues = true;

                LogMessage(
                    $"闪避原始值: 曲线key数={originalSpeedCurve?.keys.Length}, 体力={originalStaminaCost}, 冷却={originalCoolTime:F2}s");
                LogMessage(
                    $"奔跑原始值: 步速={originalWalkSpeed:F2}, 奔速={originalRunSpeed:F2}, 消耗率={originalStaminaDrainRate:F2}, 恢复率={originalStaminaRecoverRate:F2}, 恢复延迟={originalStaminaRecoverTime:F2}");
                LogMessage(
                    $"移动惯性原始值: 步行加速度={originalWalkAcc:F2}, 奔跑加速度={originalRunAcc:F2}");
                LogMessage($"负重原始值: 最大负重={originalMaxWeight:F2}");
                LogMessage($"视野原始值: 默认={originalDefaultFOV:F2}, 瞄准={originalAdsFOV:F2}");
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

        void LoadSettings()
        {
            // 闪避参数
            dashDistanceMultiplier = PlayerPrefs.GetFloat("DashPlus_DashDistance", 1.0f);
            staminaCost = PlayerPrefs.GetFloat("DashPlus_Stamina", 10f);
            coolTime = PlayerPrefs.GetFloat("DashPlus_CoolTime", 0.5f);

            // 闪避换弹设置
            enableDashReload = PlayerPrefs.GetInt("DashPlus_DashReload", 0) == 1;
            dashReloadPercentage = PlayerPrefs.GetInt("DashPlus_DashReloadPercentage", 0);

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
            LogMessage($"设置已加载: 闪避(距离={dashDistanceMultiplier}x, 体力={staminaCost}, 冷却={coolTime:F2}s), 奔跑(步行={walkSpeedMultiplier}x, 奔跑={runSpeedMultiplier}x, 消耗={staminaDrainRateMultiplier}x, 恢复={staminaRecoverRateMultiplier}x, 恢复延迟={staminaRecoverTimeMultiplier}x), 惯性(禁用={disableMovementInertia}), 负重(无限={enableInfiniteWeight}), 视野(自定义={enableCustomFOV}, 倍数={fovMultiplier:F1}x), 日志={enableLogging}");
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

        void OnGUI()
        {
            if (!showGUI) return;

            GUI.skin.window.fontSize = 14;
            GUI.skin.label.fontSize = 14;
            GUI.skin.horizontalSlider.fixedHeight = 20;
            GUI.skin.horizontalSliderThumb.fixedHeight = 25;
            GUI.skin.horizontalSliderThumb.fixedWidth = 25;

            int windowId = 12345;
            guiRect = GUI.Window(windowId, guiRect, DoWindow, "DashPlus 增强控制面板");
        }

        void DoWindow(int windowId)
        {
            // 右上角关闭按钮
            if (GUI.Button(new Rect(guiRect.width - 25, 5, 20, 20), "×"))
            {
                showGUI = false;
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
                case 3: // 其他设置
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
            GUILayout.Label("建议在ESC暂停菜单中使用 / Recommended in ESC pause menu", GUI.skin.box);

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

            // 拖动功能
            GUI.DragWindow();
        }

        void DrawDashTab()
        {
            GUILayout.Label("=== 闪避参数 / Dash Parameters ===", GUI.skin.box);
            GUILayout.Space(5);

            // 闪避距离倍数
            GUILayout.BeginHorizontal();
            GUILayout.Label("闪避距离倍数 / Dash Distance:", GUILayout.Width(180));
            float newDashMultiplier = GUILayout.HorizontalSlider(dashDistanceMultiplier, 0.1f, 5.0f, GUILayout.Width(200));
            GUILayout.Label($"{dashDistanceMultiplier:F1}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newDashMultiplier != dashDistanceMultiplier)
            {
                dashDistanceMultiplier = newDashMultiplier;
                SaveSettings();
                ApplyModIfExists();
            }

            // 体力消耗
            GUILayout.BeginHorizontal();
            GUILayout.Label("体力消耗 / Stamina Cost:", GUILayout.Width(180));
            float newStamina = GUILayout.HorizontalSlider(staminaCost, 0f, 50f, GUILayout.Width(200));
            GUILayout.Label($"{staminaCost:F1}", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newStamina != staminaCost)
            {
                staminaCost = newStamina;
                SaveSettings();
                ApplyModIfExists();
            }

            // 冷却时间
            GUILayout.BeginHorizontal();
            GUILayout.Label("冷却时间(秒) / Cooldown (s):", GUILayout.Width(180));
            float newCoolTime = GUILayout.HorizontalSlider(coolTime, 0f, 5f, GUILayout.Width(200));
            GUILayout.Label($"{coolTime:F2}s", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newCoolTime != coolTime)
            {
                coolTime = newCoolTime;
                SaveSettings();
                ApplyModIfExists();
            }

            // 闪避换弹开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("闪避换弹 /Dash Reload:", GUILayout.Width(180));
            bool newDashReload = GUILayout.Toggle(enableDashReload, enableDashReload ? "开启 / ON" : "关闭 / OFF", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newDashReload != enableDashReload)
            {
                enableDashReload = newDashReload;
                SaveSettings();
                LogMessage($"闪避换弹功能: {(enableDashReload ? "启用" : "禁用")}");
            }

            // 换弹加速百分比滑动条
            GUILayout.BeginHorizontal();
            GUILayout.Label("换弹加速 /Reload Speed:", GUILayout.Width(180));

            // 根据闪避换弹开关状态设置GUI是否可用
            GUI.enabled = enableDashReload;

            float newPercentage = GUILayout.HorizontalSlider(dashReloadPercentage, 0f, 100f, GUILayout.Width(200));
            GUILayout.Label($"{dashReloadPercentage}%", GUILayout.Width(50));
            GUI.enabled = true; // 恢复GUI状态

            GUILayout.EndHorizontal();

            if (Math.Abs(newPercentage - dashReloadPercentage) > 0.5f)
            {
                dashReloadPercentage = (int)newPercentage;
                SaveSettings();
                LogMessage($"闪避换弹加速: {dashReloadPercentage}%");
            }
        }

        void DrawRunTab()
        {
            GUILayout.Label("=== 奔跑参数 / Run Parameters ===", GUI.skin.box);
            GUILayout.Space(5);

            // 步行速度倍数
            GUILayout.BeginHorizontal();
            GUILayout.Label("步行速度倍数 / Walk Speed:", GUILayout.Width(180));
            float newWalkMultiplier = GUILayout.HorizontalSlider(walkSpeedMultiplier, 1f, 5.0f, GUILayout.Width(200));
            GUILayout.Label($"{walkSpeedMultiplier:F1}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newWalkMultiplier != walkSpeedMultiplier)
            {
                walkSpeedMultiplier = newWalkMultiplier;
                SaveSettings();
                ApplyModIfExists();
            }

            // 奔跑速度倍数
            GUILayout.BeginHorizontal();
            GUILayout.Label("奔跑速度倍数 / Run Speed:", GUILayout.Width(180));
            float newRunMultiplier = GUILayout.HorizontalSlider(runSpeedMultiplier, 1f, 5.0f, GUILayout.Width(200));
            GUILayout.Label($"{runSpeedMultiplier:F1}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newRunMultiplier != runSpeedMultiplier)
            {
                runSpeedMultiplier = newRunMultiplier;
                SaveSettings();
                ApplyModIfExists();
            }

            // 体力消耗率倍数
            GUILayout.BeginHorizontal();
            GUILayout.Label("体力消耗率倍数 / Stamina Drain:", GUILayout.Width(180));
            float newDrainMultiplier = GUILayout.HorizontalSlider(staminaDrainRateMultiplier, 0, 5.0f, GUILayout.Width(200));
            GUILayout.Label($"{staminaDrainRateMultiplier:F1}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newDrainMultiplier != staminaDrainRateMultiplier)
            {
                staminaDrainRateMultiplier = newDrainMultiplier;
                SaveSettings();
                ApplyModIfExists();
            }

            // 体力恢复率倍数
            GUILayout.BeginHorizontal();
            GUILayout.Label("体力恢复率倍数 / Stamina Recover:", GUILayout.Width(180));
            float newRecoverMultiplier = GUILayout.HorizontalSlider(staminaRecoverRateMultiplier, 1f, 5.0f, GUILayout.Width(200));
            GUILayout.Label($"{staminaRecoverRateMultiplier:F1}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newRecoverMultiplier != staminaRecoverRateMultiplier)
            {
                staminaRecoverRateMultiplier = newRecoverMultiplier;
                SaveSettings();
                ApplyModIfExists();
            }

            // 体力恢复延迟倍数
            GUILayout.BeginHorizontal();
            GUILayout.Label("体力恢复延迟倍数 / Recover Delay:", GUILayout.Width(180));
            float newRecoverTimeMultiplier = GUILayout.HorizontalSlider(staminaRecoverTimeMultiplier, 0, 5.0f, GUILayout.Width(200));
            GUILayout.Label($"{staminaRecoverTimeMultiplier:F1}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (newRecoverTimeMultiplier != staminaRecoverTimeMultiplier)
            {
                staminaRecoverTimeMultiplier = newRecoverTimeMultiplier;
                SaveSettings();
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
            bool newDisableInertia = GUILayout.Toggle(disableMovementInertia, disableMovementInertia ? "开启 / ON" : "关闭 / OFF", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newDisableInertia != disableMovementInertia)
            {
                disableMovementInertia = newDisableInertia;
                SaveSettings();
                ApplyModIfExists();
            }

            // 无限负重开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("无限负重 / Infinite Weight:", GUILayout.Width(200));
            bool newInfiniteWeight = GUILayout.Toggle(enableInfiniteWeight, enableInfiniteWeight ? "开启 / ON" : "关闭 / OFF", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newInfiniteWeight != enableInfiniteWeight)
            {
                enableInfiniteWeight = newInfiniteWeight;
                SaveSettings();
                ApplyModIfExists();
            }

            GUILayout.Space(10);

            GUILayout.Label("=== 调试设置 / Debug Settings ===", GUI.skin.box);
            GUILayout.Space(5);

            // 日志开关
            GUILayout.BeginHorizontal();
            GUILayout.Label("调试日志 / Debug Logging:", GUILayout.Width(200));
            bool newLogging = GUILayout.Toggle(enableLogging, enableLogging ? "开启 / ON" : "关闭 / OFF", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newLogging != enableLogging)
            {
                enableLogging = newLogging;
                SaveSettings();
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
            bool newCustomFOV = GUILayout.Toggle(enableCustomFOV, enableCustomFOV ? "开启 / ON" : "关闭 / OFF", GUILayout.Width(120), GUILayout.Height(25));
            GUILayout.EndHorizontal();

            if (newCustomFOV != enableCustomFOV)
            {
                enableCustomFOV = newCustomFOV;
                SaveSettings();
                ApplyModIfExists();
            }

            GUILayout.Space(10);

            // 视野倍数滑块 - 仅在启用自定义视野时可用
            GUI.enabled = enableCustomFOV; // 禁用状态下变灰
            GUILayout.BeginHorizontal();
            GUILayout.Label("视野倍数 / FOV Multiplier:", GUILayout.Width(200));
            float newFOVMultiplier = GUILayout.HorizontalSlider(fovMultiplier, 0.2f, 3.0f, GUILayout.Width(200));
            GUILayout.Label($"{fovMultiplier:F1}x", GUILayout.Width(50));
            GUILayout.EndHorizontal();
            GUI.enabled = true; // 恢复启用状态

            if (newFOVMultiplier != fovMultiplier && enableCustomFOV)
            {
                fovMultiplier = newFOVMultiplier;
                SaveSettings();
                ApplyModIfExists();
            }

            GUILayout.Space(10);

            // 操作提示 - 根据自定义视野状态决定是否变灰
            GUI.enabled = enableCustomFOV; // 启用状态与自定义视野开关一致
            GUILayout.Label("提示：可使用 Ctrl+鼠标滚轮 调整视野", GUI.skin.box);
            GUILayout.Label("Tip: Use Ctrl+Mouse Wheel to adjust FOV", GUI.skin.box);
            GUI.enabled = true; // 恢复启用状态
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

            SaveSettings();
            ApplyModIfExists();
            LogMessage("所有参数已恢复默认设置");
        }

        protected override void OnBeforeDeactivate()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

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
            catch (System.Exception ex)
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
                float elapsedTime = Time.time - dashStartTime;
                float remainingTime = originalReloadTime - elapsedTime;
                // 确保最小换弹时间为0秒
                remainingTime = Mathf.Max(remainingTime, 0);

                LogMessage($"时间累积换弹: 已流逝 {elapsedTime:F2}s，剩余 {remainingTime:F2}s");


                // 使用动作系统启动换弹，确保可中断性
                if (main.reloadAction != null && main.reloadAction.IsReady())
                {
                    // 启动换弹动作
                    main.StartAction(main.reloadAction);
                    LogMessage($"时间累积换弹启动成功，需要 {remainingTime:F2}s完成");

                    // 在下一帧应用时间加速（确保动作系统已正确初始化）
                    StartCoroutine(ApplyTimeAccumulatedReductionDelayed(gun, remainingTime));
                }
                else
                {
                    LogMessage("换弹动作未准备好，无法执行时间累积换弹");
                }
            }
            catch (System.Exception ex)
            {
                LogMessage($"时间累积换弹异常: {ex.Message}");
            }
        }

        void ApplyTimeAccumulatedReduction(object gun, float remainingTime)
        {
            try
            {
                // 获取换弹时间和状态计时器
                var reloadTimeProperty = gun.GetType().GetProperty("ReloadTime");
                var stateTimerField = gun.GetType().GetField("stateTimer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (reloadTimeProperty == null || stateTimerField == null) return;

                float reloadTime = (float)reloadTimeProperty.GetValue(gun);

                // 使用百分比计算最终的加速时间
                // 0% = 原始累积时间 (elapsedTime), 100% = 完全跳过时间 (reloadTime)
                float elapsedTime = Time.time - dashStartTime;
                float acceleratedTime = elapsedTime + (reloadTime - elapsedTime) * (dashReloadPercentage / 100f);

                stateTimerField.SetValue(gun, acceleratedTime);

                LogMessage($"时间累积换弹加速: 跳到 {acceleratedTime:F2}s，加速 {dashReloadPercentage}%，原始累积 {elapsedTime:F2}s");
            }
            catch (System.Exception ex)
            {
                LogMessage($"时间累积换弹加速异常: {ex.Message}");
            }
        }

        System.Collections.IEnumerator ApplyTimeAccumulatedReductionDelayed(object gun, float remainingTime)
        {
            // 等待一帧，确保动作系统已正确初始化换弹状态
            yield return null;

            // 现在安全地应用时间加速
            ApplyTimeAccumulatedReduction(gun, remainingTime);
        }

        void LogMessage(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[DashPlus] {message}");
            }
        }
    }
}
