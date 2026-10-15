using System;
using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class EnemyHealth : MonoBehaviour, IDamageable, IPoolable
    {
        [Header("Health Configuration")]
        [SerializeField] private float maxHealth = 35f;
        [SerializeField] private float xpDropValue = 10f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer[] renderersToFlash;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private GameObject deathEffectPrefab;

        private float currentHealth = 35f;
        private bool isDead;
        private MaterialPropertyBlock propBlock;
        private Coroutine flashCoroutine;
        private EnemyController enemyController;
        private PooledObjectTracker poolTracker;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => !isDead && currentHealth > 0f;

        public static event Action<EnemyHealth, DamageData> OnAnyEnemyDied;
        public event Action<DamageData> OnDamaged;
        public event Action OnDied;

        private void Awake()
        {
            propBlock = new MaterialPropertyBlock();
            enemyController = GetComponent<EnemyController>();
            poolTracker = GetComponent<PooledObjectTracker>();

            if (renderersToFlash == null || renderersToFlash.Length == 0)
            {
                renderersToFlash = GetComponentsInChildren<Renderer>();
            }

            ResetHealth();
        }

        public void SetMaxHealth(float newMax)
        {
            maxHealth = Mathf.Max(1f, newMax);
            currentHealth = maxHealth;
        }

        public void OnSpawnFromPool()
        {
            ResetHealth();
        }

        public void OnReturnToPool()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            ClearFlash();
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            ClearFlash();
        }

        public void TakeDamage(DamageData damage)
        {
            if (isDead) return;

            currentHealth -= damage.amount;

            // Apply knockback
            if (enemyController != null && damage.knockbackForce > 0f)
            {
                enemyController.ApplyKnockback(damage.hitDirection, damage.knockbackForce);
            }

            PlayHitFlash();
            OnDamaged?.Invoke(damage);

            if (currentHealth <= 0f)
            {
                Die(damage);
            }
        }

        private void Die(DamageData damage)
        {
            if (isDead) return;
            isDead = true;
            currentHealth = 0f;

            if (enemyController == null) enemyController = GetComponent<EnemyController>();
            if (enemyController != null) EnemyRegistry.Unregister(enemyController);

            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }

            OnDied?.Invoke();
            OnAnyEnemyDied?.Invoke(this, damage);

            // Drop XP pickup
            if (GameManager.Instance != null && GameManager.Instance.XpPool != null)
            {
                GameObject xpObj = GameManager.Instance.XpPool.Get(transform.position + Vector3.up * 0.3f, Quaternion.identity);
                ExperiencePickup pickup = xpObj.GetComponent<ExperiencePickup>();
                if (pickup != null)
                {
                    pickup.Initialize(xpDropValue);
                }
            }

            if (poolTracker != null)
            {
                poolTracker.ReturnToPool();
            }
            else
            {
                gameObject.SetActive(false);
            }
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

            yield return new WaitForSeconds(0.08f);
            ClearFlash();
            flashCoroutine = null;
        }

        private void ClearFlash()
        {
            if (renderersToFlash == null) return;
            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                if (renderersToFlash[i] != null)
                {
                    renderersToFlash[i].SetPropertyBlock(null);
                }
            }
        }
    }
}
