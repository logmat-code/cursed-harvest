using System;
using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class LuckyLastSystem : MonoBehaviour
    {
        [Header("Lucky Last Settings")]
        [SerializeField] private int startingCharges = 1;
        [SerializeField] private int maxCharges = 2;
        [SerializeField] private float duration = 12f;

        [Header("Buff Multipliers")]
        [SerializeField] private float damageBonus = 0.50f;
        [SerializeField] private float speedBonus = 0.25f;
        [SerializeField] private float cooldownReduction = 0.50f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject luckyLastAuraPrefab;

        private int currentCharges;
        private bool isLuckyLastActive;
        private float activeTimer;

        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private WeaponManager weaponManager;
        private GameObject activeAuraInstance;

        public int CurrentCharges => currentCharges;
        public int MaxCharges => maxCharges;
        public bool IsActive => isLuckyLastActive;
        public float RemainingTime => activeTimer;
        public float Duration => duration;
        public float TotalDuration => duration + ((MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetLuckyLastDurationBonus() : 0f);
        public void SetDuration(float newDuration) => duration = Mathf.Max(1.0f, newDuration);

        public static bool IsAnyActive { get; private set; }

        public static event Action<int, int> OnChargesChanged;
        public static event Action<float> OnLuckyLastActivated;
        public static event Action OnLuckyLastEnded;

        private PlayerHealth Health => playerHealth != null ? playerHealth : (playerHealth = GetComponent<PlayerHealth>());
        private PlayerController Controller => playerController != null ? playerController : (playerController = GetComponent<PlayerController>());
        private WeaponManager Weapons => weaponManager != null ? weaponManager : (weaponManager = GetComponent<WeaponManager>());

        private void Awake()
        {
            currentCharges = startingCharges;
            IsAnyActive = false;
            if (Health != null) Health.LethalHitInterceptor = CheckLethalDamage;
        }

        private void OnEnable()
        {
            if (Health != null)
            {
                Health.LethalHitInterceptor = CheckLethalDamage;
            }
        }

        private void OnDisable()
        {
            IsAnyActive = false;
            if (Health != null)
            {
                Health.LethalHitInterceptor = null;
            }
        }

        public void ResetSystem()
        {
            currentCharges = startingCharges;
            isLuckyLastActive = false;
            IsAnyActive = false;
            activeTimer = 0f;
            if (activeAuraInstance != null)
            {
                activeAuraInstance.SetActive(false);
            }
            if (Weapons != null)
            {
                Weapons.SetTemporaryDamageBonus(0f);
                Weapons.SetTemporaryCooldownMultiplier(1f);
            }
            if (Controller != null)
            {
                Controller.SetTemporarySpeedBonus(0f);
            }
            if (Health != null)
            {
                Health.LethalHitInterceptor = CheckLethalDamage;
            }
            OnChargesChanged?.Invoke(currentCharges, maxCharges);
        }

        public bool AddCharge()
        {
            if (currentCharges < maxCharges)
            {
                currentCharges++;
                OnChargesChanged?.Invoke(currentCharges, maxCharges);
                return true;
            }
            return false;
        }

        public bool CheckLethalDamage(DamageData damage)
        {
            if (isLuckyLastActive)
            {
                // Protected from dying while Lucky Last is active!
                // Keep player alive at minimum 1 HP
                if (Health != null)
                {
                    float remaining = Mathf.Max(1f, Health.CurrentHealth - damage.amount);
                    Health.SetCurrentHealth(remaining);
                }
                return true;
            }

            if (currentCharges > 0)
            {
                // Intercept lethal hit!
                currentCharges--;
                OnChargesChanged?.Invoke(currentCharges, maxCharges);

                float postHitHp = Health != null ? Mathf.Max(0f, Health.CurrentHealth - damage.amount) : 0f;
                ActivateLuckyLast(postHitHp);
                return true;
            }

            return false; // No charges and not in Lucky Last -> player dies
        }

        private void ActivateLuckyLast(float postHitHp = 0f)
        {
            isLuckyLastActive = true;
            IsAnyActive = true;
            float metaDur = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetLuckyLastDurationBonus() : 0f;
            float totalDuration = duration + metaDur;
            activeTimer = totalDuration;

            // World 1 assist + Meta recovery HP
            if (Health != null)
            {
                float extraRecoveryHP = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetLuckyLastRecoveryHPBonus() : 0f;
                float minHp = WorldDifficultyConfig.Current.luckyLastMinimumHP + extraRecoveryHP;
                Health.SetCurrentHealth(Mathf.Max(postHitHp, minHp));
            }

            // Apply buffs cleanly via temporary setters
            if (Weapons != null)
            {
                Weapons.SetTemporaryDamageBonus(damageBonus);
                Weapons.SetTemporaryCooldownMultiplier(cooldownReduction);
            }

            if (Controller != null)
            {
                Controller.SetTemporarySpeedBonus(speedBonus);
            }

            // Screen shake & UI
            if (TopDownCameraFollow.Instance != null)
            {
                TopDownCameraFollow.Instance.TriggerShake(0.5f, 0.55f);
            }

            AudioManager.Instance?.PlaySound(SoundType.LuckyLast, 1.0f, 0f);

            if (luckyLastAuraPrefab != null && activeAuraInstance == null)
            {
                activeAuraInstance = Instantiate(luckyLastAuraPrefab, transform);
            }
            if (activeAuraInstance != null)
            {
                activeAuraInstance.SetActive(true);
            }

            OnLuckyLastActivated?.Invoke(totalDuration);
            StartCoroutine(LuckyLastRoutine());
        }

        private IEnumerator LuckyLastRoutine()
        {
            while (activeTimer > 0f)
            {
                activeTimer -= Time.deltaTime;
                yield return null;
            }

            // End Lucky Last
            isLuckyLastActive = false;
            IsAnyActive = false;
            activeTimer = 0f;

            // Revert buffs cleanly
            if (Weapons != null)
            {
                Weapons.SetTemporaryDamageBonus(0f);
                Weapons.SetTemporaryCooldownMultiplier(1f);
            }

            if (Controller != null)
            {
                Controller.SetTemporarySpeedBonus(0f);
            }

            if (activeAuraInstance != null)
            {
                activeAuraInstance.SetActive(false);
            }

            OnLuckyLastEnded?.Invoke();
        }
    }
}
