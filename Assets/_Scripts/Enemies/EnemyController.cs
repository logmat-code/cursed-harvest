using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public enum EnemyType
    {
        PumpkinGrunt,
        MaskedChaser,
        PlagueWalker,
        DemonCharger,
        CursedFarmer,
        EliteScarecrow,
        HarvestBrute,
        Boss
    }

    [RequireComponent(typeof(CharacterController))]
    public class EnemyController : MonoBehaviour, IPoolable
    {
        [Header("Enemy Configuration")]
        [SerializeField] private EnemyType enemyType = EnemyType.PumpkinGrunt;
        [SerializeField] private float baseMoveSpeed = 3.2f;
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float attackCooldown = 0.8f;
        [SerializeField] private float attackRadius = 1.1f;
        [SerializeField] private float separationRadius = 1.2f;
        [SerializeField] private float separationWeight = 1.2f;

        [Header("Charger Ability")]
        [SerializeField] private bool canCharge = false;
        [SerializeField] private float chargeTriggerDistance = 8f;
        [SerializeField] private float chargeTelegraphDuration = 0.65f;
        [SerializeField] private float chargeSpeed = 13f;
        [SerializeField] private float chargeDuration = 0.85f;
        [SerializeField] private float chargeCooldown = 4.0f;
        [SerializeField] private float chargeDamage = 25f;

        [Header("Elite Summoning")]
        [SerializeField] private bool canSummonMinions = false;
        [SerializeField] private float summonCooldown = 8.0f;

        [Header("Visual Juice")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float bobFrequency = 10f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float tiltAngle = 6f;

        private CharacterController characterController;
        private EnemyHealth enemyHealth;
        private Transform playerTarget;
        private PlayerHealth playerHealthTarget;

        private enum ChargeState { None, Telegraphing, Charging, Recovery }
        private ChargeState chargeState = ChargeState.None;
        private float chargeStateTimer;
        private float lastChargeTime = -10f;
        private Vector3 lockedChargeDirection;
        private float lastSummonTime = -5f;

        private float speedMultiplier = 1f;
        private float lastAttackTime = -10f;
        private Vector3 knockbackVelocity;
        private float walkCycleTimer;
        private Vector3 visualOriginalPos;
        private Quaternion visualOriginalRot;
        private readonly List<EnemyController> nearbyBuffer = new List<EnemyController>(16);

        public EnemyType Type => enemyType;
        public float MoveSpeed => baseMoveSpeed * speedMultiplier;
        public bool IsCharging => chargeState == ChargeState.Charging;
        public bool IsAlive
        {
            get
            {
                if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
                return enemyHealth != null && enemyHealth.IsAlive;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            enemyHealth = GetComponent<EnemyHealth>();

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            if (visualRoot != null)
            {
                visualOriginalPos = visualRoot.localPosition;
                visualOriginalRot = visualRoot.localRotation;
            }
        }

        public void SetTarget(Transform target)
        {
            playerTarget = target;
            if (playerTarget != null)
            {
                playerHealthTarget = playerTarget.GetComponent<PlayerHealth>();
            }
        }

        public void Initialize(Transform target)
        {
            SetTarget(target);
            OnSpawnFromPool();
        }

        public void TickMovement()
        {
            Update();
        }

        public void SetSpeedMultiplier(float mult)
        {
            speedMultiplier = Mathf.Max(0.1f, mult);
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0f;
            knockbackVelocity = direction.normalized * force;
        }

        public void OnSpawnFromPool()
        {
            EnemyRegistry.Register(this);
            knockbackVelocity = Vector3.zero;
            lastAttackTime = -attackCooldown;
            walkCycleTimer = Random.Range(0f, 10f); // Desync bobbing between enemies
            if (visualRoot != null)
            {
                visualRoot.localPosition = visualOriginalPos;
                visualRoot.localRotation = visualOriginalRot;
            }

            if (playerTarget == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    SetTarget(playerObj.transform);
                }
            }
        }

        public void OnReturnToPool()
        {
            EnemyRegistry.Unregister(this);
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Decay knockback
            if (knockbackVelocity.sqrMagnitude > 0.01f)
            {
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, Time.deltaTime * 8f);
            }

            if (playerTarget == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    SetTarget(playerObj.transform);
                }
                else
                {
                    return;
                }
            }

            Vector3 currentPos = transform.position;
            Vector3 targetPos = playerTarget.position;
            Vector3 toPlayer = targetPos - currentPos;
            toPlayer.y = 0f;
            float distToPlayer = toPlayer.magnitude;

            // Elite Minion Summoning
            if (canSummonMinions && Time.time >= lastSummonTime + summonCooldown)
            {
                lastSummonTime = Time.time;
                if (GameManager.Instance != null && GameManager.Instance.EnemyPool != null)
                {
                    for (int s = 0; s < 2; s++)
                    {
                        Vector2 offset = UnityEngine.Random.insideUnitCircle * 2.5f;
                        Vector3 sPos = transform.position + new Vector3(offset.x, 0.2f, offset.y);
                        GameManager.Instance.EnemyPool.Get(sPos, Quaternion.identity);
                    }
                }
            }

            // Demon Charger State Machine
            if (canCharge)
            {
                if (chargeState == ChargeState.Telegraphing)
                {
                    chargeStateTimer -= Time.deltaTime;
                    if (visualRoot != null)
                    {
                        visualRoot.localPosition = visualOriginalPos + UnityEngine.Random.insideUnitSphere * 0.08f;
                    }

                    if (chargeStateTimer <= 0f)
                    {
                        chargeState = ChargeState.Charging;
                        chargeStateTimer = chargeDuration;
                        if (visualRoot != null) visualRoot.localPosition = visualOriginalPos;
                    }
                    return;
                }
                else if (chargeState == ChargeState.Charging)
                {
                    chargeStateTimer -= Time.deltaTime;
                    Vector3 chargeVel = lockedChargeDirection * chargeSpeed;
                    chargeVel.y = -5f;

                    if (characterController != null && characterController.enabled)
                    {
                        characterController.Move(chargeVel * Time.deltaTime);
                    }

                    // Check hit during charge
                    if (distToPlayer <= attackRadius * 1.4f && playerHealthTarget != null && playerHealthTarget.IsAlive)
                    {
                        DamageData chargeDmg = new DamageData(chargeDamage, gameObject, lockedChargeDirection, 10f, transform.position);
                        playerHealthTarget.TakeDamage(chargeDmg);
                        chargeState = ChargeState.Recovery;
                        chargeStateTimer = 0.5f;
                        lastChargeTime = Time.time;
                        return;
                    }

                    if (chargeStateTimer <= 0f)
                    {
                        chargeState = ChargeState.Recovery;
                        chargeStateTimer = 0.4f;
                        lastChargeTime = Time.time;
                    }
                    return;
                }
                else if (chargeState == ChargeState.Recovery)
                {
                    chargeStateTimer -= Time.deltaTime;
                    if (chargeStateTimer <= 0f)
                    {
                        chargeState = ChargeState.None;
                    }
                    return;
                }
                else if (chargeState == ChargeState.None)
                {
                    if (distToPlayer <= chargeTriggerDistance && distToPlayer >= 3f && Time.time >= lastChargeTime + chargeCooldown)
                    {
                        lockedChargeDirection = toPlayer.normalized;
                        chargeState = ChargeState.Telegraphing;
                        chargeStateTimer = chargeTelegraphDuration;
                        return;
                    }
                }
            }

            // Check contact attack
            if (distToPlayer <= attackRadius)
            {
                TryAttackPlayer();
            }

            // Flocking: calculate separation vector from nearby enemies
            Vector3 separation = Vector3.zero;
            EnemyRegistry.GetEnemiesInRange(currentPos, separationRadius, nearbyBuffer);
            for (int i = 0; i < nearbyBuffer.Count; i++)
            {
                EnemyController other = nearbyBuffer[i];
                if (other != this && other != null && other.IsAlive)
                {
                    Vector3 away = currentPos - other.transform.position;
                    away.y = 0f;
                    float d = away.magnitude;
                    if (d > 0.001f && d < separationRadius)
                    {
                        separation += (away / d) * (1f - d / separationRadius);
                    }
                }
            }

            Vector3 moveDirection = (toPlayer.normalized + separation * separationWeight).normalized;
            Vector3 finalVelocity = moveDirection * MoveSpeed + knockbackVelocity;
            finalVelocity.y = -5f; // Keep grounded

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(finalVelocity * Time.deltaTime);
            }

            // Facing direction
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
                UpdateVisualJuice();
            }
        }

        private void TryAttackPlayer()
        {
            if (Time.time < lastAttackTime + attackCooldown) return;

            if (playerHealthTarget != null && playerHealthTarget.IsAlive)
            {
                lastAttackTime = Time.time;
                DamageData dmg = new DamageData(
                    contactDamage,
                    gameObject,
                    (playerTarget.position - transform.position).normalized,
                    2f,
                    transform.position
                );
                playerHealthTarget.TakeDamage(dmg);
            }
        }

        private void UpdateVisualJuice()
        {
            // Handled with procedural anatomical bone animation by EnemyLocomotion
        }
    }
}
