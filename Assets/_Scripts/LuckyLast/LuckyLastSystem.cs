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
        [SerializeField] private float duration = 8f;

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

        public static event Action<int, int> OnChargesChanged;
        public static event Action<float> OnLuckyLastActivated;
        public static event Action OnLuckyLastEnded;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            playerController = GetComponent<PlayerController>();
            weaponManager = GetComponent<WeaponManager>();

            currentCharges = startingCharges;
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.LethalHitInterceptor = CheckLethalDamage;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.LethalHitInterceptor = null;
            }
        }

        public void ResetSystem()
        {
            currentCharges = startingCharges;
            isLuckyLastActive = false;
            activeTimer = 0f;
            if (activeAuraInstance != null)
            {
                activeAuraInstance.SetActive(false);
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

        private bool CheckLethalDamage(DamageData damage)
        {
            if (currentCharges > 0 && !isLuckyLastActive)
            {
                // Intercept lethal hit!
                currentCharges--;
                OnChargesChanged?.Invoke(currentCharges, maxCharges);

                ActivateLuckyLast();
                return true;
            }

            return false; // No charges or already active -> player dies
        }

        private void ActivateLuckyLast()
        {
            isLuckyLastActive = true;
            activeTimer = duration;

            // Set HP to exactly 1 HP
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
                // Take 99 damage to leave at 1 HP
                playerHealth.TakeDamage(new DamageData(playerHealth.MaxHealth - 1f));
            }

            // Apply buffs
            if (weaponManager != null)
            {
                weaponManager.SetDamageMultiplier(weaponManager.GlobalDamageMultiplier + damageBonus);
                weaponManager.SetCooldownMultiplier(weaponManager.GlobalCooldownMultiplier * cooldownReduction);
            }

            if (playerController != null)
            {
                playerController.SetSpeedMultiplier(playerController.MoveSpeed / 6.8f + speedBonus);
            }

            // Screen shake & UI
            if (TopDownCameraFollow.Instance != null)
            {
                TopDownCameraFollow.Instance.TriggerShake(0.4f, 0.4f);
            }

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
