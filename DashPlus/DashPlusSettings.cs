using System;

namespace DashPlus
{
    [Serializable]
    public class DashPlusSettings
    {
        // 闪避参数
        public float dashDistanceMultiplier = 1.0f;
        public float staminaCost = 10f;
        public float coolTime = 0.5f;

        // 闪避换弹设置
        public bool enableDashReload = false;
        public int dashReloadPercentage = 0;
        public bool enableShootInterruptReload = false;

        // 击杀回血设置
        public bool enableKillHeal = false;
        public int healPercentage = 5;
        public float maxHealAmount = 50.0f;

        // 奔跑参数
        public float walkSpeedMultiplier = 1.0f;
        public float runSpeedMultiplier = 1.0f;
        public float staminaDrainRateMultiplier = 1.0f;
        public float staminaRecoverRateMultiplier = 1.0f;
        public float staminaRecoverTimeMultiplier = 1.0f;

        // 移动惯性参数
        public bool disableMovementInertia = false;

        // 负重参数
        public bool enableInfiniteWeight = false;

        // 视野参数
        public bool enableCustomFOV = false;
        public float fovMultiplier = 1.0f;

        // 调试设置
        public bool enableLogging = false;
    }
}