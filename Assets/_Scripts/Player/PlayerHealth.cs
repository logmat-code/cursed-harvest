using System;
using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityDuration = 0.60f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer[] renderersToFlash;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color healFlashColor = new Color(0.35f, 0.95f, 0.45f, 1f);

        private float currentHealth = 100f;
        private float lastDamageTime = -10f;
        private bool isDead;
        private MaterialPropertyBlock propBlock;
        private Coroutine flashCoroutine;

        public float BaseMaxHealth => baseMaxHealth;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercent => Mathf.Clamp01(currentHealth / maxHealth);
        public float InvulnerabilityDuration => invulnerabilityDuration;
        public bool IsAlive => !isDead && currentHealth > 0;
        public bool IsInvulnerable => Time.time < lastDamageTime + invulnerabilityDuration;

        public void SetInvulnerabilityDuration(float duration)
        {
            invulnerabilityDuration = Mathf.Max(0f, duration);
        }

        public void ResetInvulnerability()
        {
            lastDamageTime = -10f;
        }

        /// <summary>
        /// Hook for Lucky Last or shields. Return true to intercept lethal damage.
        /// </summary>
        public Func<DamageData, bool> LethalHitInterceptor { get; set; }

        public event Action<float, float> OnHealthChanged;
        public event Action<DamageData> OnDamaged;
        public event Action OnPlayerDied;

        private void Awake()
        {
            if (baseMaxHealth <= 0f) baseMaxHealth = maxHealth > 0f ? maxHealth : 100f;
            invulnerabilityDuration = WorldDifficultyConfig.Current.hitInvulnerability;
            propBlock = new MaterialPropertyBlock();
            if (renderersToFlash == null || renderersToFlash.Length == 0)
            {
                renderersToFlash = GetComponentsInChildren<Renderer>();
            }
            ApplyMetaVitality();
        }

        private void Start()
        {
            ApplyMetaVitality();
        }

        public void ApplyMetaVitality()
        {
            if (baseMaxHealth <= 0f) baseMaxHealth = 100f;
            float metaVit = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetVitalityBonus() : 0f;
            float targetMax = baseMaxHealth + metaVit;
            if (Mathf.Abs(maxHealth - targetMax) > 0.001f || currentHealth > targetMax)
            {
                SetMaxHealth(targetMax, healDifference: true);
            }
            if (currentHealth <= 0f && !isDead)
            {
                currentHealth = maxHealth;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        public void ResetHealth()
        {
            isDead = false;
            ApplyMetaVitality();
            currentHealth = maxHealth;
            lastDamageTime = -10f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void SetCurrentHealth(float hp)
        {
            currentHealth = Mathf.Clamp(hp, 0f, maxHealth);
            isDead = currentHealth <= 0f;
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

        public float Heal(float amount)
        {
            if (isDead || amount <= 0f) return 0f;
            if (currentHealth >= maxHealth) return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            float actualHealed = currentHealth - previousHealth;

            if (actualHealed > 0f)
            {
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                PlayHealFeedback(actualHealed);
            }

            return actualHealed;
        }

        private void PlayHealFeedback(float amount)
        {
            PlayColorFlash(healFlashColor, 0.12f);
            DamageNumberManager.Instance?.SpawnHealing(transform.position + Vector3.up * 1.3f, amount);
            AudioManager.Instance?.PlaySound(SoundType.Heal, 0.85f, 0.05f);
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

            AudioManager.Instance?.PlaySound(SoundType.PlayerHit, 0.9f, 0.05f);
            TopDownCameraFollow.Instance?.TriggerShake(0.18f, 0.22f);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke(damage);
        }

        private void PlayHitFlash()
        {
            PlayColorFlash(hitFlashColor, 0.10f);
        }

        public void PlayColorFlash(Color flashColor, float duration = 0.10f)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(ColorFlashRoutine(flashColor, duration));
        }

        private IEnumerator ColorFlashRoutine(Color flashColor, float duration)
        {
            if (renderersToFlash == null) yield break;

            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                if (renderersToFlash[i] != null)
                {
                    renderersToFlash[i].GetPropertyBlock(propBlock);
                    propBlock.SetColor("_BaseColor", flashColor);
                    propBlock.SetColor("_Color", flashColor);
                    renderersToFlash[i].SetPropertyBlock(propBlock);
                }
            }

            yield return new WaitForSeconds(duration);

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
