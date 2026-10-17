using System;
using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    public class CursedSeedPickup : MonoBehaviour, IPoolable, ICollectible
    {
        [Header("Seed Settings")]
        [SerializeField] private float magnetRadius = 3.5f;
        [SerializeField] private float moveSpeed = 16f;
        [SerializeField] private float collectDistance = 0.6f;

        [Header("Idle Animation")]
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 4f;

        private Transform playerTarget;
        private PooledObjectTracker poolTracker;
        private Vector3 spawnPosition;
        private float spawnTime;
        private bool isAttracted;
        private Coroutine vacuumCoroutine;

        private static float lastSeedSoundTime = -10f;

        public static int PlayerSeedCount { get; private set; } = 0;
        private static bool hasCollectedFirstSeed = false;

        public static event Action OnFirstSeedCollected;

        public bool IsActive => gameObject.activeInHierarchy;

        public static void AddSeeds(int amount)
        {
            PlayerSeedCount = Mathf.Max(0, PlayerSeedCount + amount);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateSeedCountHUD(PlayerSeedCount);
            }
        }

        public static bool TryConsumeSeed()
        {
            if (PlayerSeedCount > 0)
            {
                PlayerSeedCount--;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateSeedCountHUD(PlayerSeedCount);
                }
                return true;
            }
            return false;
        }

        public static void ResetSeeds()
        {
            PlayerSeedCount = 0;
            hasCollectedFirstSeed = false;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateSeedCountHUD(PlayerSeedCount);
            }
        }

        private void Awake()
        {
            poolTracker = GetComponent<PooledObjectTracker>();
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
                if (player != null) playerTarget = player.transform;
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
            if (playerTransform != null) playerTarget = playerTransform;
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
                if (player != null) playerTarget = player.transform;
                else return;
            }

            Vector3 targetPos = playerTarget.position + Vector3.up * 0.8f;
            Vector3 toPlayer = targetPos - transform.position;
            float dist = toPlayer.magnitude;

            if (!isAttracted && dist <= magnetRadius)
            {
                isAttracted = true;
            }

            if (isAttracted)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                if (dist <= collectDistance)
                {
                    Collect();
                }
            }
            else
            {
                float bobY = Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobHeight;
                transform.position = spawnPosition + new Vector3(0f, bobY, 0f);
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        public void Collect()
        {
            AddSeeds(1);
            if (Time.time >= lastSeedSoundTime + 0.05f)
            {
                lastSeedSoundTime = Time.time;
                AudioManager.Instance?.PlaySound(SoundType.PickupSeed, 0.85f, 0.05f);
            }

            if (!hasCollectedFirstSeed)
            {
                hasCollectedFirstSeed = true;
                OnFirstSeedCollected?.Invoke();
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
