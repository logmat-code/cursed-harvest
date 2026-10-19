using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Final Boss for World 5 (Wave 20): THE PRIMEVAL HORROR (Ancient Entity of Perpetual Night).
    /// The ultimate apex boss of Cursed Harvest.
    /// Attacks:
    /// 1. Void Nova (Annihilation Ring): 13m expanding circle of void destruction (36 dmg, 1.2s charge).
    /// 2. Cosmic Meteor Starfall: Rains 4 void projectiles in sequence, exploding and spawning Void Rift pools (30 dmg, 1.0s telegraph).
    /// 3. Abyssal Graviton Pull: Strong gravitational singularity pulling player inward while charging frontal cleave (42 dmg, 180° arc).
    /// 4. Eclipse Overdrive: Triggered below 40% HP, dramatically increasing speed and summon frequency.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class PrimevalHorrorBoss : BossController
    {
        [Header("Primeval Horror Combat Configuration")]
        [SerializeField] private float voidNovaRadius = 13.0f;
        [SerializeField] private float voidNovaDamage = 36.0f;
        [SerializeField] private float voidNovaChargeDuration = 1.2f;
        [SerializeField] private float voidNovaCooldown = 9.0f;

        [Header("Cosmic Meteor Starfall")]
        [SerializeField] private float starfallDamage = 30.0f;
        [SerializeField] private float starfallRadius = 2.6f;
        [SerializeField] private float starfallCooldown = 8.0f;
        [SerializeField] private float starfallTelegraphDuration = 1.0f;
        [SerializeField] private GameObject voidRiftPoolPrefab;

        [Header("Abyssal Graviton Pull")]
        [SerializeField] private float pullDuration = 2.2f;
        [SerializeField] private float pullStrength = 8.5f;
        [SerializeField] private float cleaveDamage = 42.0f;
        [SerializeField] private float cleaveRange = 5.0f;
        [SerializeField] private float pullCooldown = 11.0f;

        [Header("Movement & Contact")]
        [SerializeField] private float bossMoveSpeed = 2.6f;
        [SerializeField] private float bossContactDamage = 40.0f;
        [SerializeField] private float bossContactCooldown = 0.8f;
        private float lastContactDamageTime = -10f;

        [Header("Telegraph Colors")]
        [SerializeField] private Color novaColor = new Color(0.80f, 0.20f, 1.0f, 0.95f);
        [SerializeField] private Color meteorColor = new Color(0.95f, 0.30f, 0.80f, 0.90f);
        [SerializeField] private Color pullColor = new Color(0.50f, 0.10f, 0.85f, 0.85f);

        private CharacterController cc;
        private EnemyHealth eh;
        private Transform playerTr;
        private PlayerHealth playerHp;
        private PlayerController playerController;

        private float lastNovaTime = -10f;
        private float lastStarfallTime = -10f;
        private float lastPullTime = -10f;
        private bool isExecutingAction = false;
        private bool isOverdriveActive = false;

        private LineRenderer novaCircleLine;
        private LineRenderer meteorCircleLine;
        private LineRenderer cleaveArcLine;

        public float VoidNovaRadius => voidNovaRadius;
        public float VoidNovaDamage => voidNovaDamage;
        public float StarfallDamage => starfallDamage;
        public float CleaveDamage => cleaveDamage;
        public float MoveSpeed => bossMoveSpeed;

        public void SetVoidRiftPoolPrefab(GameObject prefab) => voidRiftPoolPrefab = prefab;

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
                eh.SetMaxHealth(5200f);
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
                    playerController = player.GetComponent<PlayerController>();
                }
            }
        }

        private void SetupTelegraphRenderers()
        {
            // Void Nova Circle
            GameObject novaObj = new GameObject("Telegraph_VoidNova");
            novaObj.transform.SetParent(transform, false);
            novaCircleLine = novaObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(novaCircleLine, novaColor, 0.30f);
            novaCircleLine.positionCount = 48;
            novaCircleLine.loop = true;
            novaCircleLine.enabled = false;

            // Starfall Circle
            GameObject meteorObj = new GameObject("Telegraph_Starfall");
            meteorObj.transform.SetParent(transform, false);
            meteorCircleLine = meteorObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(meteorCircleLine, meteorColor, 0.22f);
            meteorCircleLine.positionCount = 36;
            meteorCircleLine.loop = true;
            meteorCircleLine.enabled = false;

            // Cleave Arc
            GameObject cleaveObj = new GameObject("Telegraph_Cleave");
            cleaveObj.transform.SetParent(transform, false);
            cleaveArcLine = cleaveObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(cleaveArcLine, pullColor, 0.25f);
            cleaveArcLine.positionCount = 24;
            cleaveArcLine.enabled = false;
        }

        private void ConfigureLineRenderer(LineRenderer lr, Color col, float width)
        {
            lr.useWorldSpace = true;
            lr.startWidth = width;
            lr.endWidth = width;
            Material m = new Material(Shader.Find("Sprites/Default"));
            m.color = col;
            lr.material = m;
            lr.startColor = col;
            lr.endColor = col;
        }

        private void Update()
        {
            AcquirePlayer();
            if (playerTr == null || isExecutingAction) return;

            Vector3 toPlayer = playerTr.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            // Check Phase 2 Overdrive
            float hpPct = eh != null && eh.MaxHealth > 0 ? eh.CurrentHealth / eh.MaxHealth : 1f;
            if (!isOverdriveActive && hpPct <= 0.40f)
            {
                isOverdriveActive = true;
                bossMoveSpeed *= 1.30f;
                voidNovaCooldown *= 0.80f;
                starfallCooldown *= 0.80f;

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("ECLIPSE OVERDRIVE", "The Primeval Horror enters frenzy!", 3.5f, new Color(0.85f, 0.15f, 1.0f));
                }
            }

            // Contact damage
            if (dist <= 2.2f && Time.time >= lastContactDamageTime + bossContactCooldown)
            {
                lastContactDamageTime = Time.time;
                if (playerHp != null)
                {
                    playerHp.TakeDamage(new DamageData(bossContactDamage, gameObject, Vector3.zero, 0f, transform.position));
                }
            }

            // AI Decision Priority
            if (Time.time >= lastNovaTime + voidNovaCooldown && dist <= voidNovaRadius * 0.85f)
            {
                StartCoroutine(ExecuteVoidNovaRoutine());
            }
            else if (Time.time >= lastPullTime + pullCooldown && dist > 5f && dist < 14f)
            {
                StartCoroutine(ExecuteGravitonPullRoutine());
            }
            else if (Time.time >= lastStarfallTime + starfallCooldown)
            {
                StartCoroutine(ExecuteStarfallRoutine());
            }
            else
            {
                // Chase Player
                if (dist > 1.8f && cc != null && cc.enabled)
                {
                    Vector3 moveDir = toPlayer.normalized;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 5f);
                    cc.Move(moveDir * (bossMoveSpeed * Time.deltaTime) + Vector3.down * (9.81f * Time.deltaTime));
                }
            }
        }

        private IEnumerator ExecuteVoidNovaRoutine()
        {
            isExecutingAction = true;
            lastNovaTime = Time.time;

            if (novaCircleLine != null)
            {
                novaCircleLine.enabled = true;
                int segs = 48;
                for (int i = 0; i < segs; i++)
                {
                    float angle = i * Mathf.PI * 2f / segs;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * voidNovaRadius, 0.08f, Mathf.Sin(angle) * voidNovaRadius);
                    novaCircleLine.SetPosition(i, transform.position + offset);
                }
            }

            float timer = 0f;
            while (timer < voidNovaChargeDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (novaCircleLine != null) novaCircleLine.enabled = false;

            // Deal Nova damage
            Vector3 toPlayer = playerTr.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude <= voidNovaRadius && playerHp != null)
            {
                playerHp.TakeDamage(new DamageData(voidNovaDamage, gameObject, Vector3.zero, 0f, transform.position));
            }

            yield return new WaitForSeconds(0.4f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteStarfallRoutine()
        {
            isExecutingAction = true;
            lastStarfallTime = Time.time;

            int meteorCount = isOverdriveActive ? 5 : 4;
            for (int m = 0; m < meteorCount; m++)
            {
                if (playerTr == null) break;

                Vector2 offset2D = UnityEngine.Random.insideUnitCircle * 4.0f;
                Vector3 impactCenter = playerTr.position + new Vector3(offset2D.x, 0f, offset2D.y);
                impactCenter.y = 0.05f;

                if (meteorCircleLine != null)
                {
                    meteorCircleLine.enabled = true;
                    int segs = 36;
                    for (int i = 0; i < segs; i++)
                    {
                        float angle = i * Mathf.PI * 2f / segs;
                        Vector3 off = new Vector3(Mathf.Cos(angle) * starfallRadius, 0.05f, Mathf.Sin(angle) * starfallRadius);
                        meteorCircleLine.SetPosition(i, impactCenter + off);
                    }
                }

                yield return new WaitForSeconds(starfallTelegraphDuration * 0.6f);

                if (meteorCircleLine != null) meteorCircleLine.enabled = false;

                // Impact damage
                Vector3 toPlayer = playerTr.position - impactCenter;
                toPlayer.y = 0f;
                if (toPlayer.magnitude <= starfallRadius && playerHp != null)
                {
                    playerHp.TakeDamage(new DamageData(starfallDamage, gameObject, Vector3.zero, 0f, impactCenter));
                }

                // Spawn Void Rift Hazard Pool at impact point
                if (voidRiftPoolPrefab != null)
                {
                    Instantiate(voidRiftPoolPrefab, impactCenter, Quaternion.identity);
                }

                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.2f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteGravitonPullRoutine()
        {
            isExecutingAction = true;
            lastPullTime = Time.time;

            float pullTimer = 0f;
            while (pullTimer < pullDuration)
            {
                pullTimer += Time.deltaTime;
                if (playerTr != null)
                {
                    Vector3 pullDir = (transform.position - playerTr.position);
                    pullDir.y = 0f;
                    float dist = pullDir.magnitude;
                    if (dist > 1.8f)
                    {
                        Vector3 pullVelocity = pullDir.normalized * pullStrength * Time.deltaTime;
                        CharacterController playerCC = playerTr.GetComponent<CharacterController>();
                        if (playerCC != null && playerCC.enabled)
                        {
                            playerCC.Move(pullVelocity);
                        }
                    }
                }
                yield return null;
            }

            // Frontal Cleave after pull
            Vector3 aimDir = (playerTr.position - transform.position);
            aimDir.y = 0f;
            aimDir.Normalize();

            if (cleaveArcLine != null)
            {
                cleaveArcLine.enabled = true;
                int segs = 24;
                float halfAngle = 90f;
                float baseAngle = Mathf.Atan2(aimDir.x, aimDir.z) * Mathf.Rad2Deg;
                for (int i = 0; i < segs; i++)
                {
                    float angleDeg = baseAngle - halfAngle + (i * (halfAngle * 2f / (segs - 1)));
                    float rad = angleDeg * Mathf.Deg2Rad;
                    Vector3 off = new Vector3(Mathf.Sin(rad) * cleaveRange, 0.08f, Mathf.Cos(rad) * cleaveRange);
                    cleaveArcLine.SetPosition(i, transform.position + off);
                }
            }

            yield return new WaitForSeconds(0.5f);

            if (cleaveArcLine != null) cleaveArcLine.enabled = false;

            // Check Cleave damage
            Vector3 toP = playerTr.position - transform.position;
            toP.y = 0f;
            if (toP.magnitude <= cleaveRange)
            {
                float dot = Vector3.Dot(aimDir, toP.normalized);
                if (dot >= 0.0f && playerHp != null) // within 180° frontal arc
                {
                    playerHp.TakeDamage(new DamageData(cleaveDamage, gameObject, Vector3.zero, 0f, transform.position));
                }
            }

            yield return new WaitForSeconds(0.4f);
            isExecutingAction = false;
        }
    }
}
