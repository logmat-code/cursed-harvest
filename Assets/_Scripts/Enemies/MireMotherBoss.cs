using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Bespoke Boss for World 3: THE MIRE MOTHER (Queen of Pestilence).
    /// Silhouette: Plague Doctor Mask, robed witch stance, ritual staff.
    /// Zone-control & pestilence attack patterns:
    /// 1. Miasma Hazard Pool: Lobs virulent swamp puddles leaving 3.5m diameter toxic pool (6 dmg/sec for 6s, 0.7s telegraph).
    /// 2. Root Burst: Straight eruption of infected roots targeting player (9.0m length, 25 dmg, 0.8s linear telegraph).
    /// 3. Plague Wave: Expanding shockwave ring at 70% and 35% HP thresholds (8.0m radius, 26 dmg, 1.0s expanding ring).
    /// 4. Summon Mire Spawns: Summons swamp minions from the surrounding bog.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class MireMotherBoss : BossController
    {
        [Header("Mire Mother Combat Configuration")]
        [SerializeField] private float rootBurstRange = 9.0f;
        [SerializeField] private float rootBurstWidth = 1.4f;
        [SerializeField] private float rootBurstDamage = 25f;
        [SerializeField] private float rootBurstTelegraphDuration = 0.8f;
        [SerializeField] private float rootBurstCooldown = 5.2f;

        [Header("Miasma Ground Hazard")]
        [SerializeField] private float miasmaCooldown = 6.0f;
        [SerializeField] private float miasmaTelegraphDuration = 0.7f;
        [SerializeField] private GameObject miasmaPoolPrefab;

        [Header("Plague Wave (Phase Shockwave)")]
        [SerializeField] private float plagueWaveRadius = 8.0f;
        [SerializeField] private float plagueWaveDamage = 26f;
        [SerializeField] private float plagueWaveTelegraphDuration = 1.0f;

        [Header("Telegraph Visuals")]
        [SerializeField] private Color toxicTelegraphColor = new Color(0.20f, 0.95f, 0.35f, 0.90f);
        [SerializeField] private Color plagueWaveColor = new Color(0.55f, 0.95f, 0.20f, 0.95f);

        private CharacterController cc;
        private EnemyHealth eh;
        private Transform playerTr;
        private PlayerHealth playerHp;

        private float lastRootBurstTime = -10f;
        private float lastMiasmaTime = -10f;
        private float lastSummonTimer = -10f;
        private bool isExecutingAction = false;

        private bool triggeredPhase1Wave = false; // <= 70% HP
        private bool triggeredPhase2Wave = false; // <= 35% HP

        private LineRenderer rootBurstLine;
        private LineRenderer miasmaTargetLine;
        private LineRenderer plagueWaveRingLine;
        private Transform miasmaTargetMarker;

        public float RootBurstRange => rootBurstRange;
        public float RootBurstDamage => rootBurstDamage;
        public float PlagueWaveRadius => plagueWaveRadius;
        public float PlagueWaveDamage => plagueWaveDamage;
        public float MaxHealth => eh != null ? eh.MaxHealth : (GetComponent<EnemyHealth>() != null ? GetComponent<EnemyHealth>().MaxHealth : 2250f);
        public float MoveSpeed => 2.6f;

        private void Awake()
        {
            Initialize();
            SetupTelegraphRenderers();
        }

        private void Start()
        {
            if (UIManager.Instance != null && eh != null)
            {
                UIManager.Instance.ShowBossBar("THE MIRE MOTHER", eh.CurrentHealth, eh.MaxHealth);
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
            if (miasmaTargetMarker != null)
            {
                Destroy(miasmaTargetMarker.gameObject);
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

            // 1. Root Burst LineRenderer (Straight line pointing forward)
            GameObject rootObj = new GameObject("Telegraph_RootBurstLine");
            rootObj.transform.SetParent(transform, false);
            rootObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            rootBurstLine = rootObj.AddComponent<LineRenderer>();
            rootBurstLine.useWorldSpace = false;
            rootBurstLine.positionCount = 4;
            rootBurstLine.startWidth = rootBurstWidth;
            rootBurstLine.endWidth = rootBurstWidth;
            rootBurstLine.material = lineMat;
            rootBurstLine.startColor = toxicTelegraphColor;
            rootBurstLine.endColor = toxicTelegraphColor;
            rootBurstLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rootBurstLine.receiveShadows = false;
            rootBurstLine.enabled = false;

            rootBurstLine.SetPosition(0, Vector3.zero);
            rootBurstLine.SetPosition(1, Vector3.forward * (rootBurstRange * 0.33f));
            rootBurstLine.SetPosition(2, Vector3.forward * (rootBurstRange * 0.66f));
            rootBurstLine.SetPosition(3, Vector3.forward * rootBurstRange);

            // 2. Miasma Ground Target Marker (Circular ring)
            GameObject miasmaObj = new GameObject("Telegraph_MiasmaTarget");
            miasmaObj.transform.SetParent(null, false);
            miasmaTargetMarker = miasmaObj.transform;
            miasmaTargetLine = miasmaObj.AddComponent<LineRenderer>();
            miasmaTargetLine.useWorldSpace = false;
            miasmaTargetLine.loop = true;
            miasmaTargetLine.positionCount = 32;
            miasmaTargetLine.startWidth = 0.12f;
            miasmaTargetLine.endWidth = 0.12f;
            miasmaTargetLine.material = lineMat;
            miasmaTargetLine.startColor = toxicTelegraphColor;
            miasmaTargetLine.endColor = toxicTelegraphColor;
            miasmaTargetLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            miasmaTargetLine.receiveShadows = false;
            miasmaTargetLine.enabled = false;

            for (int i = 0; i < 32; i++)
            {
                float a = i * (360f / 32) * Mathf.Deg2Rad;
                miasmaTargetLine.SetPosition(i, new Vector3(Mathf.Sin(a) * 1.75f, 0f, Mathf.Cos(a) * 1.75f));
            }

            // 3. Plague Wave Expanding Ring
            GameObject waveObj = new GameObject("Telegraph_PlagueWaveRing");
            waveObj.transform.SetParent(transform, false);
            waveObj.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            plagueWaveRingLine = waveObj.AddComponent<LineRenderer>();
            plagueWaveRingLine.useWorldSpace = false;
            plagueWaveRingLine.loop = true;
            plagueWaveRingLine.positionCount = 48;
            plagueWaveRingLine.startWidth = 0.16f;
            plagueWaveRingLine.endWidth = 0.16f;
            plagueWaveRingLine.material = lineMat;
            plagueWaveRingLine.startColor = plagueWaveColor;
            plagueWaveRingLine.endColor = plagueWaveColor;
            plagueWaveRingLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            plagueWaveRingLine.receiveShadows = false;
            plagueWaveRingLine.enabled = false;

            for (int i = 0; i < 48; i++)
            {
                float a = i * (360f / 48) * Mathf.Deg2Rad;
                plagueWaveRingLine.SetPosition(i, new Vector3(Mathf.Sin(a) * plagueWaveRadius, 0f, Mathf.Cos(a) * plagueWaveRadius));
            }
        }

        private void Update()
        {
            if (eh != null && !eh.IsAlive) return;

            FindTargetPlayer();
            if (playerTr == null) return;

            // Update boss health bar in real-time
            if (UIManager.Instance != null && eh != null)
            {
                UIManager.Instance.UpdateBossBar(eh.CurrentHealth, eh.MaxHealth);
            }

            if (isExecutingAction) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTr.position);

            // Turn smoothly toward player
            Vector3 dirToPlayer = (playerTr.position - transform.position).normalized;
            dirToPlayer.y = 0;
            if (dirToPlayer.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 3.5f);
            }

            // Attack Decision Logic
            // 1. Root Burst if in linear range (5.0m to 9.0m)
            if (Time.time >= lastRootBurstTime + rootBurstCooldown && distToPlayer >= 4.0f && distToPlayer <= rootBurstRange + 1.0f)
            {
                StartCoroutine(ExecuteRootBurst());
                return;
            }

            // 2. Miasma Hazard Pool
            if (Time.time >= lastMiasmaTime + miasmaCooldown)
            {
                StartCoroutine(ExecuteMiasmaPool());
                return;
            }

            // Normal Locomotion (Prowling swamp walk)
            if (cc != null && cc.enabled)
            {
                float moveSpeed = 2.6f;
                Vector3 moveDelta = dirToPlayer * (moveSpeed * Time.deltaTime);
                moveDelta.y = -9.8f * Time.deltaTime; // Gravity
                cc.Move(moveDelta);
            }
        }

        // ==================== ATTACK 1: ROOT BURST ====================

        public IEnumerator ExecuteRootBurst()
        {
            isExecutingAction = true;
            lastRootBurstTime = Time.time;

            // Lock face toward player
            if (playerTr != null)
            {
                Vector3 lookDir = (playerTr.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // 0.8s Telegraph Warning
            if (rootBurstLine != null) rootBurstLine.enabled = true;
            float elapsed = 0f;
            while (elapsed < rootBurstTelegraphDuration)
            {
                elapsed += Time.deltaTime;
                float flash = Mathf.PingPong(elapsed * 10f, 1f);
                if (rootBurstLine != null)
                {
                    Color c = Color.Lerp(toxicTelegraphColor, Color.white, flash * 0.7f);
                    rootBurstLine.startColor = c;
                    rootBurstLine.endColor = c;
                }
                yield return null;
            }

            if (rootBurstLine != null) rootBurstLine.enabled = false;

            // Deal Root Damage in straight corridor
            if (playerTr != null && playerHp != null && playerHp.IsAlive)
            {
                Vector3 toPlayer = playerTr.position - transform.position;
                toPlayer.y = 0;
                float forwardProj = Vector3.Dot(toPlayer, transform.forward);
                float rightProj = Mathf.Abs(Vector3.Dot(toPlayer, transform.right));

                if (forwardProj >= 0.5f && forwardProj <= rootBurstRange && rightProj <= rootBurstWidth * 0.75f)
                {
                    playerHp.TakeDamage(new DamageData(rootBurstDamage, null, transform.forward * 4.0f, 0.25f, transform.position));
                }
            }

            AudioManager.Instance?.PlaySound(SoundType.PlayerHit, 1.2f, 0.1f);
            TopDownCameraFollow.Instance?.TriggerShake(0.35f, 0.40f);

            yield return new WaitForSeconds(0.45f);
            isExecutingAction = false;
        }

        // ==================== ATTACK 2: MIASMA HAZARD POOL ====================

        public IEnumerator ExecuteMiasmaPool()
        {
            isExecutingAction = true;
            lastMiasmaTime = Time.time;

            Vector3 targetPos = playerTr != null ? playerTr.position : transform.position + transform.forward * 4f;
            targetPos.y = 0.04f;

            if (miasmaTargetMarker != null)
            {
                miasmaTargetMarker.position = targetPos;
                if (miasmaTargetLine != null) miasmaTargetLine.enabled = true;
            }

            float elapsed = 0f;
            while (elapsed < miasmaTelegraphDuration)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.PingPong(elapsed * 8f, 1f);
                if (miasmaTargetLine != null)
                {
                    Color c = Color.Lerp(toxicTelegraphColor, Color.white, pulse * 0.7f);
                    miasmaTargetLine.startColor = c;
                    miasmaTargetLine.endColor = c;
                }
                yield return null;
            }

            if (miasmaTargetLine != null) miasmaTargetLine.enabled = false;

            // Spawn Miasma Pool at target location
            SpawnMiasmaHazard(targetPos);

            yield return new WaitForSeconds(0.35f);
            isExecutingAction = false;
        }

        private void SpawnMiasmaHazard(Vector3 position)
        {
            GameObject poolObj = null;
            if (miasmaPoolPrefab != null)
            {
                poolObj = Instantiate(miasmaPoolPrefab, position, Quaternion.identity);
            }
#if UNITY_EDITOR
            else
            {
                GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/MiasmaHazardPool.prefab");
                if (loaded != null) poolObj = Instantiate(loaded, position, Quaternion.identity);
            }
#endif
            if (poolObj == null)
            {
                poolObj = new GameObject("MiasmaHazardPool");
                poolObj.transform.position = position;
                MiasmaHazardPool hazard = poolObj.AddComponent<MiasmaHazardPool>();
                hazard.Initialize(6.0f, 1.75f, 6.0f);
            }

            AudioManager.Instance?.PlaySound(SoundType.PlayerHit, 0.8f, 0.1f);
        }

        // ==================== ATTACK 3: PLAGUE WAVE SHOCKWAVE ====================

        private void CheckPhaseTransitions(DamageData damage)
        {
            if (eh == null || eh.MaxHealth <= 0f) return;
            float hpPct = eh.CurrentHealth / eh.MaxHealth;

            // Phase 1 Wave at <= 70% HP
            if (!triggeredPhase1Wave && hpPct <= 0.70f)
            {
                triggeredPhase1Wave = true;
                StartCoroutine(ExecutePlagueWave(1));
            }
            // Phase 2 Wave at <= 35% HP
            else if (!triggeredPhase2Wave && hpPct <= 0.35f)
            {
                triggeredPhase2Wave = true;
                StartCoroutine(ExecutePlagueWave(2));
            }
        }

        public IEnumerator ExecutePlagueWave(int phase)
        {
            isExecutingAction = true;

            if (UIManager.Instance != null)
            {
                string msg = phase == 1 ? "THE MIRE AWAKENS!" : "PLAGUE STORM!";
                UIManager.Instance.ShowToast("THE MIRE MOTHER", msg, 2.8f, plagueWaveColor);
            }

            // Expanding ring telegraph (0 to 8m over 1.0s)
            if (plagueWaveRingLine != null) plagueWaveRingLine.enabled = true;
            float elapsed = 0f;
            while (elapsed < plagueWaveTelegraphDuration)
            {
                elapsed += Time.deltaTime;
                float currentRadius = Mathf.Lerp(1.0f, plagueWaveRadius, elapsed / plagueWaveTelegraphDuration);
                for (int i = 0; i < 48; i++)
                {
                    float a = i * (360f / 48) * Mathf.Deg2Rad;
                    plagueWaveRingLine.SetPosition(i, new Vector3(Mathf.Sin(a) * currentRadius, 0.05f, Mathf.Cos(a) * currentRadius));
                }
                yield return null;
            }

            if (plagueWaveRingLine != null) plagueWaveRingLine.enabled = false;

            // Shockwave detonation
            if (playerTr != null && playerHp != null && playerHp.IsAlive)
            {
                float dist = Vector3.Distance(transform.position, playerTr.position);
                if (dist <= plagueWaveRadius)
                {
                    Vector3 pushDir = (playerTr.position - transform.position).normalized;
                    pushDir.y = 0;
                    playerHp.TakeDamage(new DamageData(plagueWaveDamage, null, pushDir * 6.0f, 0.35f, transform.position));
                }
            }

            // Camera shake & sound
            TopDownCameraFollow.Instance?.TriggerShake(0.5f, 0.6f);
            AudioManager.Instance?.PlaySound(SoundType.BossSlam, 1.3f, 0.05f);

            yield return new WaitForSeconds(0.4f);
            isExecutingAction = false;
        }

        public void SetMiasmaPoolPrefab(GameObject prefab)
        {
            miasmaPoolPrefab = prefab;
        }
    }
}
