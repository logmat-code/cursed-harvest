using UnityEngine;

namespace CursedHarvest
{
    public class ExperiencePickup : MonoBehaviour, IPoolable
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

        public float XpValue => xpValue;

        private void Awake()
        {
            poolTracker = GetComponent<PooledObjectTracker>();
        }

        public void Initialize(float value)
        {
            xpValue = value;
        }

        public void OnSpawnFromPool()
        {
            spawnPosition = transform.position;
            spawnTime = Time.time;
            isAttracted = false;

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
        }

        private void Update()
        {
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

            AudioManager.Instance?.PlaySound(SoundType.PickupXP, 0.7f, 0.08f);

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
