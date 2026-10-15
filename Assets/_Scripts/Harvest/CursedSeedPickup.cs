using UnityEngine;

namespace CursedHarvest
{
    public class CursedSeedPickup : MonoBehaviour, IPoolable
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

        public static int PlayerSeedCount { get; private set; } = 1; // Starts with 1 seed

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
            PlayerSeedCount = 1;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateSeedCountHUD(PlayerSeedCount);
            }
        }

        private void Awake()
        {
            poolTracker = GetComponent<PooledObjectTracker>();
        }

        public void OnSpawnFromPool()
        {
            spawnPosition = transform.position;
            spawnTime = Time.time;
            isAttracted = false;

            if (playerTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTarget = player.transform;
            }
        }

        public void OnReturnToPool()
        {
            isAttracted = false;
        }

        private void Update()
        {
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
            AudioManager.Instance?.PlaySound(SoundType.PickupSeed, 0.85f, 0.05f);

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
