using System;
using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityDuration = 0.25f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer[] renderersToFlash;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

        private float currentHealth = 100f;
        private float lastDamageTime = -10f;
        private bool isDead;
        private MaterialPropertyBlock propBlock;
        private Coroutine flashCoroutine;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsAlive => !isDead && currentHealth > 0;
        public bool IsInvulnerable => Time.time < lastDamageTime + invulnerabilityDuration;

        /// <summary>
        /// Hook for Lucky Last or shields. Return true to intercept lethal damage.
        /// </summary>
        public Func<DamageData, bool> LethalHitInterceptor { get; set; }

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageData> OnDamaged;
        public event Action OnPlayerDied;

        private void Awake()
        {
            propBlock = new MaterialPropertyBlock();
            if (renderersToFlash == null || renderersToFlash.Length == 0)
            {
                renderersToFlash = GetComponentsInChildren<Renderer>();
            }
            ResetHealth();
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            lastDamageTime = -10f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetMaxHealth(float newMaxHealth, bool healDifference = true)
        {
            float diff = newMaxHealth - maxHealth;
            maxHealth = Mathf.Max(1f, newMaxHealth);
            if (healDifference && diff > 0)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + diff);
            }
            else
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth);
            }
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(DamageData damage)
        {
            if (isDead || IsInvulnerable) return;

            // Check if damage is lethal
            if (currentHealth - damage.amount <= 0)
            {
                if (LethalHitInterceptor != null && LethalHitInterceptor(damage))
                {
                    // Intercepted by Lucky Last!
                    lastDamageTime = Time.time;
                    PlayHitFlash();
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                    return;
                }

                currentHealth = 0;
                isDead = true;
                OnHealthChanged?.Invoke(0, maxHealth);
                OnDamaged?.Invoke(damage);
                OnPlayerDied?.Invoke();
                return;
            }

            currentHealth -= damage.amount;
            lastDamageTime = Time.time;
            PlayHitFlash();

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke(damage);
        }

        private void PlayHitFlash()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            if (renderersToFlash == null) yield break;

            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                if (renderersToFlash[i] != null)
                {
                    renderersToFlash[i].GetPropertyBlock(propBlock);
                    propBlock.SetColor("_BaseColor", hitFlashColor);
                    propBlock.SetColor("_Color", hitFlashColor);
                    renderersToFlash[i].SetPropertyBlock(propBlock);
                }
            }

            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                if (renderersToFlash[i] != null)
                {
                    renderersToFlash[i].SetPropertyBlock(null);
                }
            }
            flashCoroutine = null;
        }
    }
}
