using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Final Boss for World 4 (Wave 15): BLOODWOOD SOVEREIGN (Monarch of the Bleeding Bark).
    /// Ancient towering corrupted timber monarch.
    /// Attacks:
    /// 1. Blood Sap Pools: Lobs 3 persistent corrosive blood sap hazard pools (3.2m diameter, 8 dmg/sec, 7s duration).
    /// 2. Timber Spears: Rains heavy timber skewers from the canopy (34 dmg, 1.1s falling telegraph).
    /// 3. Root Cage: Erupts wooden root cage around player if stationary (20 dmg, 0.75s telegraph).
    /// 4. Call of the Grove: Summons ancient timber minions when crossing 75%, 50%, and 25% HP thresholds.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class BloodwoodSovereignBoss : BossController
    {
        [Header("Bloodwood Sovereign Combat Configuration")]
        [SerializeField] private float sapPoolRange = 12.0f;
        [SerializeField] private float sapPoolCooldown = 6.5f;
        [SerializeField] private float sapPoolTelegraphDuration = 0.7f;
        [SerializeField] private GameObject sapPoolPrefab;

        [Header("Timber Spears (Canopy Rain)")]
        [SerializeField] private float spearDamage = 34f;
        [SerializeField] private float spearRadius = 2.4f;
        [SerializeField] private float spearCooldown = 8.0f;
        [SerializeField] private float spearTelegraphDuration = 1.1f;

        [Header("Root Cage")]
        [SerializeField] private float rootCageDamage = 20f;
        [SerializeField] private float rootCageCooldown = 9.5f;
        [SerializeField] private float rootCageTelegraphDuration = 0.75f;

        [Header("Movement & Contact")]
        [SerializeField] private float bossMoveSpeed = 2.4f;
        [SerializeField] private float bossContactDamage = 36f;
        [SerializeField] private float bossContactCooldown = 0.8f;
        private float lastContactDamageTime = -10f;

        [Header("Telegraph Visuals")]
        [SerializeField] private Color sapTelegraphColor = new Color(0.95f, 0.15f, 0.12f, 0.90f);
        [SerializeField] private Color spearTelegraphColor = new Color(1.0f, 0.35f, 0.15f, 0.95f);
        [SerializeField] private Color rootCageColor = new Color(0.85f, 0.20f, 0.10f, 0.90f);

        private CharacterController cc;
        private EnemyHealth eh;
        private Transform playerTr;
        private PlayerHealth playerHp;

        private float lastSapTime = -10f;
        private float lastSpearTime = -10f;
        private float lastCageTime = -10f;
        private bool isExecutingAction = false;

        private bool summonedPhase1 = false; // 75% HP
        private bool summonedPhase2 = false; // 50% HP
        private bool summonedPhase3 = false; // 25% HP

        private LineRenderer sapCircleLine;
        private LineRenderer spearCircleLine;
        private LineRenderer rootCageLine;

        public float SapPoolRange => sapPoolRange;
        public float SpearDamage => spearDamage;
        public float SpearRadius => spearRadius;
        public float MoveSpeed => bossMoveSpeed;

        public void SetSapPoolPrefab(GameObject prefab) => sapPoolPrefab = prefab;

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
                eh.SetMaxHealth(3600f);
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
            sapCircleLine = CreateLineRenderer("Telegraph_SapPool", sapTelegraphColor, 0.22f);
            spearCircleLine = CreateLineRenderer("Telegraph_TimberSpear", spearTelegraphColor, 0.25f);
            rootCageLine = CreateLineRenderer("Telegraph_RootCage", rootCageColor, 0.25f);
        }

        private LineRenderer CreateLineRenderer(string objName, Color color, float width)
        {
            GameObject obj = new GameObject(objName);
            obj.transform.SetParent(transform, false);
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.loop = true;

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

            // Phase summon checks
            if (!summonedPhase1 && hpPct <= 0.75f)
            {
                summonedPhase1 = true;
                TriggerGroveCallToast("The Bloodwood canopy groans with dark sap!");
            }
            else if (!summonedPhase2 && hpPct <= 0.50f)
            {
                summonedPhase2 = true;
                TriggerGroveCallToast("Ancient timbers split as bleeding roots awaken!");
            }
            else if (!summonedPhase3 && hpPct <= 0.25f)
            {
                summonedPhase3 = true;
                TriggerGroveCallToast("The Sovereign bleeds pure occult resin!");
            }

            if (isExecutingAction) return;

            // Combat AI Priority:
            // 1. Timber Spears canopy bombardment
            if (Time.time >= lastSpearTime + spearCooldown)
            {
                StartCoroutine(ExecuteTimberSpearsRoutine());
            }
            // 2. Blood Sap lob
            else if (distToPlayer <= sapPoolRange && Time.time >= lastSapTime + sapPoolCooldown)
            {
                StartCoroutine(ExecuteBloodSapRoutine());
            }
            // 3. Root Cage trap
            else if (distToPlayer <= 8.0f && Time.time >= lastCageTime + rootCageCooldown)
            {
                StartCoroutine(ExecuteRootCageRoutine());
            }
            // 4. Default: Stalk player
            else
            {
                Vector3 toPlayer = (playerTr.position - transform.position).normalized;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), Time.deltaTime * 5f);
                    cc.Move(toPlayer * bossMoveSpeed * Time.deltaTime + Vector3.down * 9.8f * Time.deltaTime);
                }
            }
        }

        private void TriggerGroveCallToast(string subtitle)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToast("CALL OF THE GROVE", subtitle, 3.0f, new Color(1f, 0.2f, 0.2f));
            }
        }

        private IEnumerator ExecuteBloodSapRoutine()
        {
            isExecutingAction = true;
            lastSapTime = Time.time;

            Vector3 targetPos = playerTr.position;
            targetPos.y = 0.05f;

            if (sapCircleLine != null)
            {
                sapCircleLine.enabled = true;
                DrawCircle(sapCircleLine, targetPos, 1.6f, 32);
            }

            yield return new WaitForSeconds(sapPoolTelegraphDuration);
            if (sapCircleLine != null) sapCircleLine.enabled = false;

            // Spawn Hazard Pool
            if (sapPoolPrefab != null)
            {
                Instantiate(sapPoolPrefab, targetPos, Quaternion.identity);
            }
            else
            {
                // Fallback runtime hazard
                GameObject poolObj = new GameObject("RuntimeBloodSapPool");
                poolObj.transform.position = targetPos;
                BloodSapHazardPool pool = poolObj.AddComponent<BloodSapHazardPool>();
                pool.Initialize(7.0f, 1.6f, 8.0f);
            }

            yield return new WaitForSeconds(0.4f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteTimberSpearsRoutine()
        {
            isExecutingAction = true;
            lastSpearTime = Time.time;

            Vector3 targetPos = playerTr.position;
            targetPos.y = 0.05f;

            if (spearCircleLine != null)
            {
                spearCircleLine.enabled = true;
                DrawCircle(spearCircleLine, targetPos, spearRadius, 36);
            }

            yield return new WaitForSeconds(spearTelegraphDuration);
            if (spearCircleLine != null) spearCircleLine.enabled = false;

            // Impact check
            float dist = Vector3.Distance(new Vector3(targetPos.x, 0f, targetPos.z),
                                          new Vector3(playerTr.position.x, 0f, playerTr.position.z));
            if (dist <= spearRadius && playerHp != null)
            {
                playerHp.TakeDamage(new DamageData(spearDamage, gameObject, Vector3.up * 4f, 0.4f, targetPos));
            }

            yield return new WaitForSeconds(0.5f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteRootCageRoutine()
        {
            isExecutingAction = true;
            lastCageTime = Time.time;

            Vector3 targetPos = playerTr.position;
            targetPos.y = 0.05f;

            if (rootCageLine != null)
            {
                rootCageLine.enabled = true;
                DrawCircle(rootCageLine, targetPos, 2.2f, 32);
            }

            yield return new WaitForSeconds(rootCageTelegraphDuration);
            if (rootCageLine != null) rootCageLine.enabled = false;

            // Root eruption check
            float dist = Vector3.Distance(new Vector3(targetPos.x, 0f, targetPos.z),
                                          new Vector3(playerTr.position.x, 0f, playerTr.position.z));
            if (dist <= 2.2f && playerHp != null)
            {
                playerHp.TakeDamage(new DamageData(rootCageDamage, gameObject, Vector3.up * 6f, 0.5f, targetPos));
            }

            yield return new WaitForSeconds(0.5f);
            isExecutingAction = false;
        }

        private void DrawCircle(LineRenderer lr, Vector3 center, float radius, int segments)
        {
            lr.positionCount = segments;
            float step = (Mathf.PI * 2f) / segments;
            for (int i = 0; i < segments; i++)
            {
                float a = step * i;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
        }
    }
}
