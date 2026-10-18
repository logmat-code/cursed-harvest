using System;
using System.Collections;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Bespoke Boss for World 2: THE ASHEN REAPER (Scourge of the Scorched Grove).
    /// Wields an agricultural clearing axe with area-denial attack patterns:
    /// 1. Ash Sweep: 120° frontal arc cleave (3.5m range, 32 dmg, 0.8s red arc telegraph).
    /// 2. Ember Ground Patch: Lobs charred ember leaving 3.0m diameter burning patch (8 dmg/sec for 5s, 0.6s telegraph).
    /// 3. Ash Burst: Radial shockwave at 70% and 40% HP thresholds (7.0m radius, 28 dmg, 1.0s expanding ring telegraph, pushback).
    /// 4. Summon Adds: Periodically summons Ashen stalkers from the orchard rows.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class AshenReaperBoss : BossController
    {
        [Header("Ashen Reaper Combat Configuration")]
        [SerializeField] private float ashSweepRange = 3.5f;
        [SerializeField] private float ashSweepAngle = 120f;
        [SerializeField] private float ashSweepDamage = 32f;
        [SerializeField] private float ashSweepTelegraphDuration = 0.8f;
        [SerializeField] private float ashSweepCooldown = 5.5f;

        [Header("Ember Ground Patch (Area Denial Hazard)")]
        [SerializeField] private float emberPatchCooldown = 7.0f;
        [SerializeField] private float emberTelegraphDuration = 0.6f;
        [SerializeField] private GameObject emberPatchPrefab;

        [Header("Ash Burst (Phase Shockwave)")]
        [SerializeField] private float ashBurstRadius = 7.0f;
        [SerializeField] private float ashBurstDamage = 28f;
        [SerializeField] private float ashBurstTelegraphDuration = 1.0f;

        [Header("Telegraph Visuals")]
        [SerializeField] private Color telegraphColor = new Color(1.0f, 0.20f, 0.08f, 0.90f);
        [SerializeField] private Color ashBurstColor = new Color(0.95f, 0.45f, 0.10f, 0.95f);

        private CharacterController cc;
        private EnemyHealth eh;
        private Transform playerTr;
        private PlayerHealth playerHp;

        private float lastSweepTime = -10f;
        private float lastEmberTime = -10f;
        private float lastSummonAddTimer = -10f;
        private bool isExecutingAction = false;

        private bool triggeredPhase1Burst = false; // Triggered at <= 70% HP
        private bool triggeredPhase2Burst = false; // Triggered at <= 40% HP

        private LineRenderer sweepArcLine;
        private LineRenderer emberCircleLine;
        private LineRenderer burstRingLine;
        private Transform emberTargetMarker;

        public float AshSweepRange => ashSweepRange;
        public float AshSweepDamage => ashSweepDamage;
        public float AshBurstRadius => ashBurstRadius;
        public float AshBurstDamage => ashBurstDamage;

        private void Awake()
        {
            Initialize();
            SetupTelegraphRenderers();
        }

        private void Start()
        {
            if (UIManager.Instance != null && eh != null)
            {
                UIManager.Instance.ShowBossBar("THE ASHEN REAPER", eh.CurrentHealth, eh.MaxHealth);
            }
        }

        public new void Initialize()
        {
            base.Initialize();
            cc = GetComponent<CharacterController>();
            eh = GetComponent<EnemyHealth>();

            if (eh != null)
            {
                eh.OnDamaged -= CheckPhaseTransitions;
                eh.OnDamaged += CheckPhaseTransitions;
            }

            FindTargetPlayer();
            SetupTelegraphRenderers();
        }

        private void OnDestroy()
        {
            if (eh != null)
            {
                eh.OnDamaged -= CheckPhaseTransitions;
            }
        }

        private void FindTargetPlayer()
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
            Material lineMat = new Material(Shader.Find("Sprites/Default"));

            // 1. Ash Sweep Arc LineRenderer
            GameObject arcObj = new GameObject("Telegraph_AshSweepArc");
            arcObj.transform.SetParent(transform, false);
            arcObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            sweepArcLine = arcObj.AddComponent<LineRenderer>();
            sweepArcLine.useWorldSpace = false;
            sweepArcLine.loop = true;
            sweepArcLine.positionCount = 26; // Center + arc points + close
            sweepArcLine.startWidth = 0.12f;
            sweepArcLine.endWidth = 0.12f;
            sweepArcLine.material = lineMat;
            sweepArcLine.startColor = telegraphColor;
            sweepArcLine.endColor = telegraphColor;
            sweepArcLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sweepArcLine.receiveShadows = false;
            sweepArcLine.enabled = false;

            // Pre-compute 120° frontal arc
            float halfAngle = ashSweepAngle * 0.5f;
            int arcPoints = 24;
            sweepArcLine.SetPosition(0, Vector3.zero);
            for (int i = 0; i <= arcPoints; i++)
            {
                float a = Mathf.Lerp(-halfAngle, halfAngle, (float)i / arcPoints) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(a) * ashSweepRange, 0f, Mathf.Cos(a) * ashSweepRange);
                sweepArcLine.SetPosition(i + 1, p);
            }

            // 2. Ember Ground Patch Landing Marker
            GameObject emberObj = new GameObject("Telegraph_EmberTarget");
            emberObj.transform.SetParent(null, false);
            emberTargetMarker = emberObj.transform;
            emberCircleLine = emberObj.AddComponent<LineRenderer>();
            emberCircleLine.useWorldSpace = false;
            emberCircleLine.loop = true;
            emberCircleLine.positionCount = 32;
            emberCircleLine.startWidth = 0.10f;
            emberCircleLine.endWidth = 0.10f;
            emberCircleLine.material = lineMat;
            emberCircleLine.startColor = telegraphColor;
            emberCircleLine.endColor = telegraphColor;
            emberCircleLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            emberCircleLine.receiveShadows = false;
            emberCircleLine.enabled = false;

            for (int i = 0; i < 32; i++)
            {
                float a = i * (360f / 32) * Mathf.Deg2Rad;
                emberCircleLine.SetPosition(i, new Vector3(Mathf.Sin(a) * 1.5f, 0f, Mathf.Cos(a) * 1.5f));
            }

            // 3. Ash Burst Expanding Ring
            GameObject burstObj = new GameObject("Telegraph_AshBurstRing");
            burstObj.transform.SetParent(transform, false);
            burstObj.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            burstRingLine = burstObj.AddComponent<LineRenderer>();
            burstRingLine.useWorldSpace = false;
            burstRingLine.loop = true;
            burstRingLine.positionCount = 48;
            burstRingLine.startWidth = 0.15f;
            burstRingLine.endWidth = 0.15f;
            burstRingLine.material = lineMat;
            burstRingLine.startColor = ashBurstColor;
            burstRingLine.endColor = ashBurstColor;
            burstRingLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            burstRingLine.receiveShadows = false;
            burstRingLine.enabled = false;
        }

        private void CheckPhaseTransitions(DamageData dmg)
        {
            if (eh == null || !eh.IsAlive) return;

            float hpPct = eh.CurrentHealth / eh.MaxHealth;

            if (!triggeredPhase1Burst && hpPct <= 0.70f)
            {
                triggeredPhase1Burst = true;
                StartCoroutine(ExecuteAshBurstRoutine());
            }
            else if (!triggeredPhase2Burst && hpPct <= 0.40f)
            {
                triggeredPhase2Burst = true;
                StartCoroutine(ExecuteAshBurstRoutine());
            }
        }

        private void Update()
        {
            if (eh != null && !eh.IsAlive) return;

            FindTargetPlayer();
            if (playerTr == null) return;

            if (isExecutingAction) return;

            Vector3 toPlayer = playerTr.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            // 1. Ash Sweep frontal cleave (priority when close)
            if (dist <= ashSweepRange * 1.05f && Time.time >= lastSweepTime + ashSweepCooldown)
            {
                StartCoroutine(ExecuteAshSweepRoutine());
                return;
            }

            // 2. Ember Ground Patch (area denial hazard)
            if (dist >= 3.0f && Time.time >= lastEmberTime + emberPatchCooldown)
            {
                StartCoroutine(ExecuteEmberPatchRoutine());
                return;
            }

            // 3. Summon Orchard Adds
            if (Time.time >= lastSummonAddTimer + 15.0f)
            {
                ExecuteSummonOrchardAdds();
                return;
            }

            // Standard Chase Locomotion
            Vector3 moveDir = toPlayer.normalized;
            Vector3 velocity = moveDir * 2.8f;
            velocity.y = -5f;

            if (cc != null && cc.enabled)
            {
                cc.Move(velocity * Time.deltaTime);
            }

            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
            }

            // Contact damage
            if (dist <= 2.2f && playerHp != null && playerHp.IsAlive)
            {
                DamageData contactDmg = new DamageData(GetEffectiveContactDamage(), gameObject, moveDir, 4f, transform.position);
                playerHp.TakeDamage(contactDmg);
            }
        }

        // ==================== ABILITY 1: ASH SWEEP ====================
        private IEnumerator ExecuteAshSweepRoutine()
        {
            isExecutingAction = true;
            lastSweepTime = Time.time;

            // Telegraph: Frontal 120° red arc for 0.8s
            if (sweepArcLine != null) sweepArcLine.enabled = true;

            float timer = 0f;
            while (timer < ashSweepTelegraphDuration)
            {
                timer += Time.deltaTime;
                // Face player during initial telegraph
                if (playerTr != null && timer < ashSweepTelegraphDuration * 0.5f)
                {
                    Vector3 dir = (playerTr.position - transform.position);
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    }
                }

                // Telegraph pulse
                if (sweepArcLine != null)
                {
                    float pulse = 0.8f + Mathf.PingPong(timer * 8f, 0.4f);
                    sweepArcLine.startColor = telegraphColor * pulse;
                    sweepArcLine.endColor = telegraphColor * pulse;
                }
                yield return null;
            }

            if (sweepArcLine != null) sweepArcLine.enabled = false;

            // Execute Sweep Cleave
            if (playerTr != null && playerHp != null && playerHp.IsAlive)
            {
                Vector3 toPlayer = playerTr.position - transform.position;
                toPlayer.y = 0f;
                float dist = toPlayer.magnitude;

                if (dist <= ashSweepRange)
                {
                    float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
                    if (angle <= ashSweepAngle * 0.5f)
                    {
                        float effDmg = ashSweepDamage * WorldDifficultyConfig.Current.bossDamageMultiplier;
                        Vector3 push = toPlayer.normalized + Vector3.up * 0.15f;
                        DamageData dmg = new DamageData(effDmg, gameObject, push, 12f, transform.position);
                        playerHp.TakeDamage(dmg);

                        if (TopDownCameraFollow.Instance != null)
                        {
                            TopDownCameraFollow.Instance.TriggerShake(0.18f, 0.12f);
                        }
                    }
                }
            }

            AudioManager.Instance?.PlaySound(SoundType.BossSlam, 1.15f, 0.05f);

            yield return new WaitForSeconds(0.25f);
            isExecutingAction = false;
        }

        // ==================== ABILITY 2: EMBER GROUND PATCH ====================
        private IEnumerator ExecuteEmberPatchRoutine()
        {
            isExecutingAction = true;
            lastEmberTime = Time.time;

            if (playerTr == null)
            {
                isExecutingAction = false;
                yield break;
            }

            // Target player position at time of telegraph start
            Vector3 targetPos = playerTr.position;
            targetPos.y = 0.05f;

            if (emberTargetMarker != null && emberCircleLine != null)
            {
                emberTargetMarker.position = targetPos;
                emberCircleLine.enabled = true;
            }

            float timer = 0f;
            while (timer < emberTelegraphDuration)
            {
                timer += Time.deltaTime;
                if (emberCircleLine != null)
                {
                    float pulse = 0.75f + Mathf.PingPong(timer * 10f, 0.5f);
                    emberCircleLine.startColor = telegraphColor * pulse;
                    emberCircleLine.endColor = telegraphColor * pulse;
                }
                yield return null;
            }

            if (emberCircleLine != null) emberCircleLine.enabled = false;

            // Spawn Ember Hazard Patch
            SpawnEmberPatch(targetPos);

            yield return new WaitForSeconds(0.2f);
            isExecutingAction = false;
        }

        private void SpawnEmberPatch(Vector3 pos)
        {
            GameObject patchObj = null;
            if (emberPatchPrefab != null)
            {
                patchObj = Instantiate(emberPatchPrefab, pos, Quaternion.identity);
            }
            else
            {
                patchObj = new GameObject("EmberHazardPatch_Instance");
                patchObj.transform.position = pos;
                EmberHazardPatch hazard = patchObj.AddComponent<EmberHazardPatch>();
                hazard.Initialize(5.0f, 1.5f, 8.0f);
            }

            AudioManager.Instance?.PlaySound(SoundType.FireTick, 0.9f, 0.05f);
        }

        // ==================== ABILITY 3: ASH BURST ====================
        private IEnumerator ExecuteAshBurstRoutine()
        {
            isExecutingAction = true;

            if (burstRingLine != null) burstRingLine.enabled = true;

            float timer = 0f;
            while (timer < ashBurstTelegraphDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / ashBurstTelegraphDuration;
                float currentRadius = Mathf.Lerp(1.0f, ashBurstRadius, progress);

                if (burstRingLine != null)
                {
                    for (int i = 0; i < 48; i++)
                    {
                        float a = i * (360f / 48) * Mathf.Deg2Rad;
                        burstRingLine.SetPosition(i, new Vector3(Mathf.Sin(a) * currentRadius, 0f, Mathf.Cos(a) * currentRadius));
                    }
                    burstRingLine.startColor = ashBurstColor;
                    burstRingLine.endColor = ashBurstColor;
                }
                yield return null;
            }

            if (burstRingLine != null) burstRingLine.enabled = false;

            // Detonate Shockwave
            if (playerTr != null && playerHp != null && playerHp.IsAlive)
            {
                float dist = Vector3.Distance(transform.position, playerTr.position);
                if (dist <= ashBurstRadius)
                {
                    float effDmg = ashBurstDamage * WorldDifficultyConfig.Current.bossDamageMultiplier;
                    Vector3 pushDir = (playerTr.position - transform.position).normalized;
                    pushDir.y = 0.2f;
                    DamageData dmg = new DamageData(effDmg, gameObject, pushDir, 14f, transform.position);
                    playerHp.TakeDamage(dmg);
                }
            }

            if (TopDownCameraFollow.Instance != null)
            {
                TopDownCameraFollow.Instance.TriggerShake(0.28f, 0.18f);
            }

            AudioManager.Instance?.PlaySound(SoundType.BossSlam, 1.3f, 0.05f);

            yield return new WaitForSeconds(0.35f);
            isExecutingAction = false;
        }

        // ==================== ABILITY 4: SUMMON ADDS ====================
        private void ExecuteSummonOrchardAdds()
        {
            lastSummonAddTimer = Time.time;

            if (GameManager.Instance != null && GameManager.Instance.EnemyPool != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(4f, 8f);
                    Vector3 pos = transform.position + new Vector3(circle.x, 0.3f, circle.y);
                    GameManager.Instance.EnemyPool.Get(pos, Quaternion.identity);
                }
            }
        }
    }
}
