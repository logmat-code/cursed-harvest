using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class HealthRestorePickup : MonoBehaviour, IPoolable, ICollectible
    {
        [Header("Heal Settings")]
        [SerializeField] private float healAmount = 20f;
        [SerializeField] private float collectDistance = 0.85f;
        [SerializeField] private float magnetRadius = 0.5f; // Small touch magnet only, no accidental vacuum!
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private float lifetime = 18f;
        [SerializeField] private float fadeStartLifetime = 14f;

        [Header("Idle Animation")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 3f;

        private Transform playerTarget;
        private PlayerHealth playerHealth;
        private PooledObjectTracker poolTracker;
        private Vector3 spawnPosition;
        private float spawnTime;
        private bool isAttracted;
        private Renderer[] renderers;
        private Light glowLight;
        private Coroutine vacuumCoroutine;
        private static float lastFullHealthNoticeTime = -10f;

        public float HealAmount => healAmount;
        public bool IsActive => gameObject.activeInHierarchy;

        private void Awake()
        {
            poolTracker = GetComponent<PooledObjectTracker>();
            renderers = GetComponentsInChildren<Renderer>(true);
            glowLight = GetComponentInChildren<Light>(true);
        }

        public void Initialize(float amount)
        {
            healAmount = amount;
        }

        private void OnEnable()
        {
            CollectibleRegistry.Register(this);
        }

        private void OnDisable()
        {
            CollectibleRegistry.Unregister(this);
            if (vacuumCoroutine != null)
            {
                StopCoroutine(vacuumCoroutine);
                vacuumCoroutine = null;
            }
        }

        public void OnSpawnFromPool()
        {
            spawnPosition = transform.position;
            spawnTime = Time.time;
            isAttracted = false;
            SetRenderersVisible(true);
            CollectibleRegistry.Register(this);

            if (playerTarget == null)
            {
                FindPlayer();
            }
        }

        public void OnReturnToPool()
        {
            isAttracted = false;
            SetRenderersVisible(true);
            CollectibleRegistry.Unregister(this);
            if (vacuumCoroutine != null)
            {
                StopCoroutine(vacuumCoroutine);
                vacuumCoroutine = null;
            }
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        public void VacuumTo(Transform playerTransform, float flyDuration = 0.8f)
        {
            if (!gameObject.activeInHierarchy) return;
            if (playerTransform != null)
            {
                playerTarget = playerTransform;
                playerHealth = playerTransform.GetComponent<PlayerHealth>();
            }
            if (vacuumCoroutine != null) StopCoroutine(vacuumCoroutine);
            vacuumCoroutine = StartCoroutine(VacuumRoutine(playerTransform, flyDuration));
        }

        private IEnumerator VacuumRoutine(Transform target, float duration)
        {
            isAttracted = true;
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t;
                Vector3 targetPos = target.position + Vector3.up * 0.8f;
                transform.position = Vector3.Lerp(startPos, targetPos, ease);

                if (Vector3.Distance(transform.position, targetPos) <= collectDistance)
                {
                    TryCollect(isVacuum: true);
                    vacuumCoroutine = null;
                    yield break;
                }
                yield return null;
            }
            TryCollect(isVacuum: true);
            vacuumCoroutine = null;
        }

        private void Update()
        {
            if (vacuumCoroutine != null) return; // Controlled by vacuum coroutine

            // Lifetime check
            float elapsed = Time.time - spawnTime;
            if (elapsed >= lifetime)
            {
                Despawn();
                return;
            }

            // Expiration blinking
            if (elapsed >= fadeStartLifetime)
            {
                bool blink = (Mathf.FloorToInt(elapsed * 7f) % 2) == 0;
                SetRenderersVisible(blink);
            }
            else
            {
                SetRenderersVisible(true);
            }

            if (playerTarget == null || playerHealth == null)
            {
                FindPlayer();
                if (playerTarget == null) return;
            }

            if (!playerHealth.IsAlive) return;

            Vector3 targetPos = playerTarget.position + Vector3.up * 0.8f;
            Vector3 toPlayer = targetPos - transform.position;
            float dist = toPlayer.magnitude;

            // Only attract if player is missing health and within small touch magnet
            bool hasMissingHealth = playerHealth.CurrentHealth < playerHealth.MaxHealth;
            if (!isAttracted && dist <= magnetRadius && hasMissingHealth)
            {
                isAttracted = true;
            }

            if (isAttracted)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                if (dist <= collectDistance)
                {
                    TryCollect(isVacuum: false);
                }
            }
            else
            {
                // Touch distance check even if not magnetized
                if (dist <= collectDistance)
                {
                    TryCollect(isVacuum: false);
                }

                // Idle spin & bob
                float bobY = Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
                transform.position = spawnPosition + new Vector3(0f, bobY, 0f);
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null)
            {
                TryCollect(isVacuum: false);
            }
        }

        public float GetEffectiveHealAmount()
        {
            if (WorldDifficultyConfig.Current != null)
            {
                return WorldDifficultyConfig.Current.GetEffectiveHealAmount(LuckyLastSystem.IsAnyActive);
            }
            float survivalMult = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetSurvivalHealMultiplier() : 1f;
            return healAmount * survivalMult;
        }

        private void TryCollect(bool isVacuum = false)
        {
            if (playerHealth == null) FindPlayer();
            if (playerHealth == null || !playerHealth.IsAlive) return;

            // Full health protection
            if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
            {
                if (isVacuum)
                {
                    // Cleanly consume with debounced floating notice
                    if (Time.time >= lastFullHealthNoticeTime + 1.8f)
                    {
                        lastFullHealthNoticeTime = Time.time;
                        DamageNumberManager.Instance?.SpawnText(
                            playerTarget.position + Vector3.up * 1.2f,
                            "FULL HEALTH",
                            new Color(0.85f, 0.95f, 0.85f, 0.9f),
                            0.55f);
                    }
                    Despawn();
                    return;
                }

                isAttracted = false;
                if (Time.time >= lastFullHealthNoticeTime + 1.8f)
                {
                    lastFullHealthNoticeTime = Time.time;
                    DamageNumberManager.Instance?.SpawnText(
                        transform.position + Vector3.up * 0.9f,
                        "FULL HEALTH",
                        new Color(0.85f, 0.95f, 0.85f, 0.9f),
                        0.45f);
                }
                return;
            }

            // Heal player (+25 HP during Lucky Last, +20 HP otherwise)
            float effectiveHeal = GetEffectiveHealAmount();
            float actualHealed = playerHealth.Heal(effectiveHeal);
            if (actualHealed > 0f || isVacuum)
            {
                Despawn();
            }
        }

        private void SetRenderersVisible(bool visible)
        {
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) renderers[i].enabled = visible;
                }
            }
            if (glowLight != null) glowLight.enabled = visible;
        }

        private void Despawn()
        {
            if (poolTracker != null)
            {
                poolTracker.ReturnToPool();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
