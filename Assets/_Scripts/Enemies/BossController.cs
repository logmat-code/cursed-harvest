using System;
using UnityEngine;

namespace CursedHarvest
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class BossController : MonoBehaviour
    {
        [Header("Boss Identity")]
        [SerializeField] private string bossName = "THE LAST HARVEST";
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float contactDamage = 30f;
        [SerializeField] private float slamRadius = 6.5f;
        [SerializeField] private float slamDamage = 35f;

        [Header("Visual & Effects")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject slamVfxPrefab;
        [SerializeField] private GameObject bombPrefab;

        private CharacterController characterController;
        private EnemyHealth enemyHealth;
        private Transform playerTransform;
        private PlayerHealth playerHealth;

        private float lastSlamTime = -5f;
        private float lastMortarTime = -3f;
        private float lastSummonTime = -10f;
        private bool isAttacking;
        private float attackTimer;

        public string BossName => bossName;
        public EnemyHealth Health => enemyHealth;

        public static event Action<BossController> OnBossSpawned;
        public static event Action<BossController> OnBossDefeated;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            if (enemyHealth != null)
            {
                enemyHealth.OnDamaged -= OnDamaged;
                enemyHealth.OnDamaged += OnDamaged;
                enemyHealth.OnDied -= OnDefeated;
                enemyHealth.OnDied += OnDefeated;
            }

            FindPlayer();
        }

        private void Start()
        {
            if (UIManager.Instance != null && enemyHealth != null)
            {
                UIManager.Instance.ShowBossBar(bossName, enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
            }

            OnBossSpawned?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnDamaged -= OnDamaged;
                enemyHealth.OnDied -= OnDefeated;
            }
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerHealth = player.GetComponent<PlayerHealth>();
                }
            }
        }

        private void OnDamaged(DamageData dmg)
        {
            if (UIManager.Instance != null && enemyHealth != null)
            {
                UIManager.Instance.UpdateBossBar(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
            }
        }

        private void OnDefeated()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideBossBar();
                UIManager.Instance.ShowVictoryModal();
            }

            // Drop 5 Cursed Seeds
            if (GameManager.Instance != null && GameManager.Instance.SeedPool != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 c = UnityEngine.Random.insideUnitCircle * 3f;
                    GameManager.Instance.SeedPool.Get(transform.position + new Vector3(c.x, 0.4f, c.y), Quaternion.identity);
                }
            }

            OnBossDefeated?.Invoke(this);
        }

        private void Update()
        {
            if (enemyHealth != null && !enemyHealth.IsAlive) return;

            FindPlayer();
            if (playerTransform == null) return;

            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            // Attack Rotations
            if (Time.time >= lastSlamTime + 9f && dist <= slamRadius * 1.1f)
            {
                ExecuteGroundSlam();
                return;
            }

            if (Time.time >= lastMortarTime + 6f)
            {
                ExecuteMortarBarrage();
                return;
            }

            if (Time.time >= lastSummonTime + 16f)
            {
                ExecuteSummonSwarm();
                return;
            }

            // Standard Chase
            Vector3 moveDir = toPlayer.normalized;
            Vector3 velocity = moveDir * moveSpeed;
            velocity.y = -5f;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(velocity * Time.deltaTime);
            }

            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion rot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 8f * Time.deltaTime);
            }

            // Contact damage
            if (dist <= 2.2f && playerHealth != null && playerHealth.IsAlive)
            {
                DamageData contactDmg = new DamageData(contactDamage, gameObject, moveDir, 4f, transform.position);
                playerHealth.TakeDamage(contactDmg);
            }
        }

        private void ExecuteGroundSlam()
        {
            lastSlamTime = Time.time;

            if (slamVfxPrefab != null)
            {
                Instantiate(slamVfxPrefab, transform.position, Quaternion.identity);
            }

            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= slamRadius)
            {
                if (playerHealth != null && playerHealth.IsAlive)
                {
                    Vector3 blastDir = (playerTransform.position - transform.position).normalized;
                    blastDir.y = 0.2f;
                    DamageData slamDmg = new DamageData(slamDamage, gameObject, blastDir, 14f, transform.position);
                    playerHealth.TakeDamage(slamDmg);
                }
            }

            if (TopDownCameraFollow.Instance != null)
            {
                TopDownCameraFollow.Instance.TriggerShake(0.6f, 0.45f);
            }
        }

        private void ExecuteMortarBarrage()
        {
            lastMortarTime = Time.time;

            if (playerTransform == null) return;

            Vector3 origin = transform.position + Vector3.up * 2f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * 4f;
                Vector3 target = playerTransform.position + new Vector3(spread.x, 0f, spread.y);

                if (GameManager.Instance != null && GameManager.Instance.BoltPool != null)
                {
                    // If a dedicated bomb prefab is provided, instantiate it
                    if (bombPrefab != null)
                    {
                        GameObject bObj = Instantiate(bombPrefab, origin, Quaternion.identity);
                        PumpkinBombProjectile proj = bObj.GetComponent<PumpkinBombProjectile>();
                        if (proj != null)
                        {
                            int playerLayer = LayerMask.NameToLayer("Player");
                            LayerMask mask = playerLayer >= 0 ? (1 << playerLayer) : ~0;
                            proj.Initialize(origin, target, 14f, 25f, 6f, mask, slamVfxPrefab);
                        }
                    }
                }
            }
        }

        private void ExecuteSummonSwarm()
        {
            lastSummonTime = Time.time;

            if (GameManager.Instance != null && GameManager.Instance.EnemyPool != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(3f, 6f);
                    Vector3 pos = transform.position + new Vector3(circle.x, 0.3f, circle.y);
                    GameManager.Instance.EnemyPool.Get(pos, Quaternion.identity);
                }
            }
        }
    }
}
