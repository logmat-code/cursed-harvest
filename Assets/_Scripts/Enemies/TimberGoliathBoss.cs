using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Mid-Boss for World 4 (Wave 10): TIMBER GOLIATH (Crusher of the Ancient Pines).
    /// Brutal woodcutter silhouette: giant two-handed logging axe, heavy lumbering stride.
    /// Attacks:
    /// 1. Log Sweep: 180° frontal axe cleave (4.2m range, 34 dmg, 0.75s telegraph arc).
    /// 2. Ground Splinter: Linear wood splinter fissure directed at player (8.5m length, 1.4m width, 28 dmg, 0.8s telegraph).
    /// 3. Enraged Charge: High-speed forward charge knocking player back (14 m/s, 35 dmg, 0.6s telegraph).
    /// 4. Timber Shockwave: Heavy radial slam when crossing 50% HP (7.5m radius, 26 dmg, 1.0s expanding ring).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class TimberGoliathBoss : BossController
    {
        [Header("Timber Goliath Combat Configuration")]
        [SerializeField] private float logSweepRange = 4.2f;
        [SerializeField] private float logSweepAngle = 180f;
        [SerializeField] private float logSweepDamage = 34f;
        [SerializeField] private float logSweepTelegraphDuration = 0.75f;
        [SerializeField] private float logSweepCooldown = 5.0f;

        [Header("Ground Splinter Fissure")]
        [SerializeField] private float groundSplinterRange = 8.5f;
        [SerializeField] private float groundSplinterWidth = 1.4f;
        [SerializeField] private float groundSplinterDamage = 28f;
        [SerializeField] private float groundSplinterTelegraphDuration = 0.8f;
        [SerializeField] private float groundSplinterCooldown = 6.2f;

        [Header("Enraged Charge")]
        [SerializeField] private float chargeSpeed = 14.0f;
        [SerializeField] private float chargeDuration = 0.9f;
        [SerializeField] private float chargeDamage = 35f;
        [SerializeField] private float chargeCooldown = 8.0f;
        [SerializeField] private float chargeTelegraphDuration = 0.6f;

        [Header("Timber Shockwave (50% HP Milestone)")]
        [SerializeField] private float shockwaveRadius = 7.5f;
        [SerializeField] private float shockwaveDamage = 26f;
        [SerializeField] private float shockwaveTelegraphDuration = 1.0f;

        [Header("Movement & Contact")]
        [SerializeField] private float bossMoveSpeed = 2.8f;
        [SerializeField] private float bossContactDamage = 32f;
        [SerializeField] private float bossContactCooldown = 0.8f;
        private float lastContactDamageTime = -10f;

        [Header("Telegraph Colors")]
        [SerializeField] private Color sweepColor = new Color(0.95f, 0.35f, 0.15f, 0.90f);
        [SerializeField] private Color splinterColor = new Color(0.85f, 0.25f, 0.10f, 0.90f);
        [SerializeField] private Color shockwaveColor = new Color(0.90f, 0.40f, 0.10f, 0.95f);

        private CharacterController cc;
        private EnemyHealth eh;
        private Transform playerTr;
        private PlayerHealth playerHp;

        private float lastSweepTime = -10f;
        private float lastSplinterTime = -10f;
        private float lastChargeTime = -10f;
        private bool isExecutingAction = false;
        private bool triggeredShockwave = false;

        private LineRenderer sweepArcLine;
        private LineRenderer splinterCorridorLine;
        private LineRenderer shockwaveRingLine;
        private LineRenderer chargeArrowLine;

        public float LogSweepRange => logSweepRange;
        public float LogSweepDamage => logSweepDamage;
        public float GroundSplinterRange => groundSplinterRange;
        public float ChargeDamage => chargeDamage;
        public float ShockwaveRadius => shockwaveRadius;
        public float MoveSpeed => bossMoveSpeed;

        private void Awake()
        {
            Initialize();
            SetupTelegraphRenderers();
        }

        public new void Initialize()
        {
            base.Initialize();
            cc = GetComponent<CharacterController>();
            eh = GetComponent<EnemyHealth>();

            if (eh != null)
            {
                eh.SetMaxHealth(2800f);
            }
        }

        private void Start()
        {
            AcquirePlayer();
        }

        private void AcquirePlayer()
        {
            if (playerTr == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTr = player.transform;
                    playerHp = player.GetComponent<PlayerHealth>();
                }
            }
        }

        private void SetupTelegraphRenderers()
        {
            sweepArcLine = CreateLineRenderer("Telegraph_LogSweepArc", sweepColor, 0.22f);
            splinterCorridorLine = CreateLineRenderer("Telegraph_GroundSplinter", splinterColor, 0.25f);
            shockwaveRingLine = CreateLineRenderer("Telegraph_TimberShockwave", shockwaveColor, 0.25f);
            chargeArrowLine = CreateLineRenderer("Telegraph_EnragedCharge", sweepColor, 0.28f);
        }

        private LineRenderer CreateLineRenderer(string objName, Color color, float width)
        {
            GameObject obj = new GameObject(objName);
            obj.transform.SetParent(transform, false);
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.loop = false;

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            lr.material = mat;
            lr.startColor = color;
            lr.endColor = color;
            lr.enabled = false;
            return lr;
        }

        private void Update()
        {
            if (eh != null && !eh.IsAlive) return;
            AcquirePlayer();
            if (playerTr == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTr.position);
            float hpPct = eh != null && eh.MaxHealth > 0 ? eh.CurrentHealth / eh.MaxHealth : 1f;

            // 50% HP Shockwave check
            if (!triggeredShockwave && hpPct <= 0.50f && !isExecutingAction)
            {
                triggeredShockwave = true;
                StartCoroutine(ExecuteTimberShockwaveRoutine());
                return;
            }

            if (isExecutingAction) return;

            // Combat AI Priority:
            // 1. Close range (< 4.5m) -> Log Sweep
            if (distToPlayer <= logSweepRange + 0.5f && Time.time >= lastSweepTime + logSweepCooldown)
            {
                StartCoroutine(ExecuteLogSweepRoutine());
            }
            // 2. Medium/Long range (5.0m - 12.0m) -> Ground Splinter
            else if (distToPlayer >= 4.5f && distToPlayer <= groundSplinterRange && Time.time >= lastSplinterTime + groundSplinterCooldown)
            {
                StartCoroutine(ExecuteGroundSplinterRoutine());
            }
            // 3. Long range (> 7.0m) -> Enraged Charge
            else if (distToPlayer >= 7.0f && Time.time >= lastChargeTime + chargeCooldown)
            {
                StartCoroutine(ExecuteEnragedChargeRoutine());
            }
            // 4. Default: Chase player
            else
            {
                Vector3 toPlayer = (playerTr.position - transform.position).normalized;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), Time.deltaTime * 6f);
                    cc.Move(toPlayer * bossMoveSpeed * Time.deltaTime + Vector3.down * 9.8f * Time.deltaTime);
                }
            }
        }

        private IEnumerator ExecuteLogSweepRoutine()
        {
            isExecutingAction = true;
            lastSweepTime = Time.time;

            Vector3 aimDir = (playerTr.position - transform.position).normalized;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(aimDir);

            // Display 180° arc telegraph
            if (sweepArcLine != null)
            {
                sweepArcLine.enabled = true;
                DrawArc(sweepArcLine, transform.position + Vector3.up * 0.1f, aimDir, logSweepAngle, logSweepRange, 32);
            }

            float timer = 0f;
            while (timer < logSweepTelegraphDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (sweepArcLine != null) sweepArcLine.enabled = false;

            // Execute Sweep Cleave
            float dist = Vector3.Distance(transform.position, playerTr.position);
            if (dist <= logSweepRange)
            {
                Vector3 toP = (playerTr.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toP);
                if (angle <= logSweepAngle * 0.5f && playerHp != null)
                {
                    playerHp.TakeDamage(new DamageData(logSweepDamage, gameObject, transform.forward * 12f, 0.4f, transform.position));
                }
            }

            yield return new WaitForSeconds(0.4f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteGroundSplinterRoutine()
        {
            isExecutingAction = true;
            lastSplinterTime = Time.time;

            Vector3 aimDir = (playerTr.position - transform.position).normalized;
            aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(aimDir);

            // Display linear splinter telegraph
            if (splinterCorridorLine != null)
            {
                splinterCorridorLine.enabled = true;
                DrawCorridor(splinterCorridorLine, transform.position + Vector3.up * 0.1f, aimDir, groundSplinterRange, groundSplinterWidth);
            }

            float timer = 0f;
            while (timer < groundSplinterTelegraphDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (splinterCorridorLine != null) splinterCorridorLine.enabled = false;

            // Execute Linear Splinter Fissure
            Vector3 toP = playerTr.position - transform.position;
            float forwardProj = Vector3.Dot(toP, transform.forward);
            float rightProj = Mathf.Abs(Vector3.Dot(toP, transform.right));

            if (forwardProj >= 0f && forwardProj <= groundSplinterRange && rightProj <= groundSplinterWidth * 0.5f && playerHp != null)
            {
                playerHp.TakeDamage(new DamageData(groundSplinterDamage, gameObject, Vector3.up * 6f + transform.forward * 8f, 0.4f, transform.position));
            }

            yield return new WaitForSeconds(0.45f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteEnragedChargeRoutine()
        {
            isExecutingAction = true;
            lastChargeTime = Time.time;

            Vector3 chargeDir = (playerTr.position - transform.position).normalized;
            chargeDir.y = 0f;
            if (chargeDir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(chargeDir);

            // Telegraph charge direction
            if (chargeArrowLine != null)
            {
                chargeArrowLine.enabled = true;
                chargeArrowLine.positionCount = 2;
                chargeArrowLine.SetPosition(0, transform.position + Vector3.up * 0.15f);
                chargeArrowLine.SetPosition(1, transform.position + Vector3.up * 0.15f + chargeDir * 14f);
            }

            yield return new WaitForSeconds(chargeTelegraphDuration);
            if (chargeArrowLine != null) chargeArrowLine.enabled = false;

            // Execute Charge
            float chargeTimer = 0f;
            bool hitPlayer = false;

            while (chargeTimer < chargeDuration)
            {
                chargeTimer += Time.deltaTime;
                cc.Move(chargeDir * chargeSpeed * Time.deltaTime + Vector3.down * 9.8f * Time.deltaTime);

                if (!hitPlayer && Vector3.Distance(transform.position, playerTr.position) < 2.2f)
                {
                    hitPlayer = true;
                    if (playerHp != null)
                    {
                        playerHp.TakeDamage(new DamageData(chargeDamage, gameObject, chargeDir * 16f + Vector3.up * 4f, 0.5f, transform.position));
                    }
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteTimberShockwaveRoutine()
        {
            isExecutingAction = true;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast("TIMBER SPLINTER!", "The Goliath shatters the logging ground!", 2.5f, new Color(1f, 0.4f, 0.1f));
            }

            if (shockwaveRingLine != null)
            {
                shockwaveRingLine.enabled = true;
                DrawCircle(shockwaveRingLine, transform.position + Vector3.up * 0.15f, shockwaveRadius, 48);
            }

            yield return new WaitForSeconds(shockwaveTelegraphDuration);
            if (shockwaveRingLine != null) shockwaveRingLine.enabled = false;

            // Damage player if inside radius
            float dist = Vector3.Distance(transform.position, playerTr.position);
            if (dist <= shockwaveRadius && playerHp != null)
            {
                Vector3 knockDir = (playerTr.position - transform.position).normalized;
                playerHp.TakeDamage(new DamageData(shockwaveDamage, gameObject, knockDir * 14f + Vector3.up * 6f, 0.5f, transform.position));
            }

            yield return new WaitForSeconds(0.6f);
            isExecutingAction = false;
        }

        // ==================== TELEGRAPH DRAWING HELPERS ====================
        private void DrawArc(LineRenderer lr, Vector3 center, Vector3 forward, float angle, float radius, int segments)
        {
            lr.positionCount = segments + 2;
            lr.SetPosition(0, center);

            float halfAngle = angle * 0.5f;
            float step = angle / segments;

            for (int i = 0; i <= segments; i++)
            {
                float a = -halfAngle + (step * i);
                Quaternion rot = Quaternion.Euler(0f, a, 0f);
                Vector3 dir = rot * forward;
                lr.SetPosition(i + 1, center + dir * radius);
            }
        }

        private void DrawCorridor(LineRenderer lr, Vector3 start, Vector3 forward, float length, float width)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 p1 = start - right * (width * 0.5f);
            Vector3 p2 = p1 + forward * length;
            Vector3 p3 = p2 + right * width;
            Vector3 p4 = start + right * (width * 0.5f);

            lr.positionCount = 5;
            lr.SetPosition(0, p1);
            lr.SetPosition(1, p2);
            lr.SetPosition(2, p3);
            lr.SetPosition(3, p4);
            lr.SetPosition(4, p1);
        }

        private void DrawCircle(LineRenderer lr, Vector3 center, float radius, int segments)
        {
            lr.positionCount = segments + 1;
            float step = (Mathf.PI * 2f) / segments;
            for (int i = 0; i <= segments; i++)
            {
                float a = step * i;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }
    }
}
