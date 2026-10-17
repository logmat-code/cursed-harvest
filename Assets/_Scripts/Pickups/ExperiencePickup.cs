using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class ExperiencePickup : MonoBehaviour, IPoolable, ICollectible
    {
        [Header("XP Settings")]
        [SerializeField] private float xpValue = 10f;
        [SerializeField] private float baseMagnetRadius = 3.5f;
        [SerializeField] private float moveSpeed = 16f;
        [SerializeField] private float collectDistance = 0.6f;

        [Header("Idle Animation")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 3f;

        private Transform playerTarget;
        private ExperienceManager xpManager;
        private PooledObjectTracker poolTracker;
        private Vector3 spawnPosition;
        private float spawnTime;
        private bool isAttracted;
        private Coroutine vacuumCoroutine;

        private static float lastXpSoundTime = -10f;

        public float XpValue => xpValue;
        public bool IsActive => gameObject.activeInHierarchy;

        private void Awake()
        {
            poolTracker = GetComponent<PooledObjectTracker>();
        }

        public void Initialize(float value)
        {
            xpValue = value;
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
            CollectibleRegistry.Register(this);

            if (playerTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                    xpManager = player.GetComponent<ExperienceManager>();
                }
            }
        }

        public void OnReturnToPool()
        {
            isAttracted = false;
            CollectibleRegistry.Unregister(this);
            if (vacuumCoroutine != null)
            {
                StopCoroutine(vacuumCoroutine);
                vacuumCoroutine = null;
            }
        }

        public void VacuumTo(Transform playerTransform, float flyDuration = 0.8f)
        {
            if (!gameObject.activeInHierarchy) return;
            if (playerTransform != null)
            {
                playerTarget = playerTransform;
                if (xpManager == null) xpManager = playerTransform.GetComponent<ExperienceManager>();
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
                    Collect();
                    vacuumCoroutine = null;
                    yield break;
                }
                yield return null;
            }
            Collect();
            vacuumCoroutine = null;
        }

        private void Update()
        {
            if (vacuumCoroutine != null) return; // Controlled by vacuum coroutine

            if (playerTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                    xpManager = player.GetComponent<ExperienceManager>();
                }
                else
                {
                    return;
                }
            }

            Vector3 targetPos = playerTarget.position + Vector3.up * 0.8f;
            Vector3 toPlayer = targetPos - transform.position;
            float dist = toPlayer.magnitude;

            float currentMagnetRadius = baseMagnetRadius * (xpManager != null ? xpManager.MagnetRadiusMultiplier : 1f);

            if (!isAttracted && dist <= currentMagnetRadius)
            {
                isAttracted = true;
            }

            if (isAttracted)
            {
                // Smooth acceleration towards player
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                if (dist <= collectDistance)
                {
                    Collect();
                }
            }
            else
            {
                // Idle spin & bob
                float bobY = Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
                transform.position = spawnPosition + new Vector3(0f, bobY, 0f);
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void Collect()
        {
            if (xpManager != null)
            {
                xpManager.AddXP(xpValue);
            }

            if (Time.time >= lastXpSoundTime + 0.05f)
            {
                lastXpSoundTime = Time.time;
                AudioManager.Instance?.PlaySound(SoundType.PickupXP, 0.7f, 0.08f);
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
    }
}
