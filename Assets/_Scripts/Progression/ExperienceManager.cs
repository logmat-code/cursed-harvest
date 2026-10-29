using System;
using UnityEngine;

namespace CursedHarvest
{
    public class ExperienceManager : MonoBehaviour
    {
        [Header("XP Progression")]
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private float baseXP = 20f;          // legacy (unused by new curve)
        [SerializeField] private float xpGrowthRate = 1.35f;  // legacy (unused by new curve)
        private const float startRequiredXP = 85f;

        [Header("Pickup Magnet")]
        [SerializeField] private float magnetRadiusMultiplier = 1f;

        private int currentLevel;
        private float currentXP;
        private float requiredXP;

        public int CurrentLevel => currentLevel;
        public float CurrentXP => currentXP;
        public float RequiredXP => requiredXP;
        public float ProgressPercent => Mathf.Clamp01(currentXP / requiredXP);
        public float MagnetRadiusMultiplier => magnetRadiusMultiplier;

        public event Action<float, float, float> OnXPChanged;
        public event Action<int> OnLevelUp;

        private void Awake()
        {
            ResetProgression();
        }

        public void ResetProgression()
        {
            currentLevel = startingLevel;
            currentXP = 0f;
            requiredXP = CalculateRequiredXP(currentLevel);
            magnetRadiusMultiplier = 1f;
            OnXPChanged?.Invoke(currentXP, requiredXP, ProgressPercent);
        }

        public void SetMagnetMultiplier(float multiplier)
        {
            magnetRadiusMultiplier = Mathf.Max(0.5f, multiplier);
        }

        public void AddXP(float amount)
        {
            if (amount <= 0f) return;

            currentXP += amount;

            while (currentXP >= requiredXP)
            {
                currentXP -= requiredXP;
                currentLevel++;
                requiredXP = CalculateRequiredXP(currentLevel);
                AudioManager.Instance?.PlaySound(SoundType.LevelUp, 1.0f, 0f);
                MobileSettingsAndLifecycleManager.Instance?.TriggerHaptic(HapticPattern.LevelUp);
                OnLevelUp?.Invoke(currentLevel);
            }

            OnXPChanged?.Invoke(currentXP, requiredXP, ProgressPercent);
        }

        private float CalculateRequiredXP(int level)
        {
            // v1.1.2 progressive curve (was baseXP * 1.35^(L-1): too many early upgrades).
            // L1=85, L3=148, L5=253, L8=455, L10=620, L12=~830, L15=~1160
            // Early: ~45-60s, mid: ~60-90s, late: ~90-120s per level at the XP income of waves 1-15.
            int n = Mathf.Max(0, level - 1);
            return Mathf.Round(startRequiredXP + 28f * n + 3.5f * n * n);
        }
    }
}
