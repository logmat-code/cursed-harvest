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
            activeTimer = duration;

            // World 1 assist: Restore immediately up to minimum 15 HP (max(current, 15))
            if (Health != null)
            {
                float minHp = WorldDifficultyConfig.Current.luckyLastMinimumHP;
                Health.SetCurrentHealth(Mathf.Max(postHitHp, minHp));
            }

            // Apply buffs
            if (Weapons != null)
            {
                Weapons.SetDamageMultiplier(Weapons.GlobalDamageMultiplier + damageBonus);
                Weapons.SetCooldownMultiplier(Weapons.GlobalCooldownMultiplier * cooldownReduction);
            }

            if (Controller != null)
            {
                Controller.SetSpeedMultiplier(Controller.MoveSpeed / 6.8f + speedBonus);
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

            OnLuckyLastActivated?.Invoke(duration);
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

            // Revert buffs
            if (weaponManager != null)
            {
                weaponManager.SetDamageMultiplier(Mathf.Max(1f, weaponManager.GlobalDamageMultiplier - damageBonus));
                weaponManager.SetCooldownMultiplier(weaponManager.GlobalCooldownMultiplier / cooldownReduction);
            }

            if (playerController != null)
            {
                playerController.SetSpeedMultiplier(Mathf.Max(1f, playerController.MoveSpeed / 6.8f - speedBonus));
            }

            if (activeAuraInstance != null)
            {
                activeAuraInstance.SetActive(false);
            }

            OnLuckyLastEnded?.Invoke();
        }
    }
}
