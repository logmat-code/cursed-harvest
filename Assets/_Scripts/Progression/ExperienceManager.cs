using System;
using UnityEngine;

namespace CursedHarvest
{
    public class ExperienceManager : MonoBehaviour
    {
        [Header("XP Progression")]
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private float baseXP = 20f;
        [SerializeField] private float xpGrowthRate = 1.35f;

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
                OnLevelUp?.Invoke(currentLevel);
            }

            OnXPChanged?.Invoke(currentXP, requiredXP, ProgressPercent);
        }

        private float CalculateRequiredXP(int level)
        {
            return Mathf.Round(baseXP * Mathf.Pow(xpGrowthRate, level - 1));
        }
    }
}
