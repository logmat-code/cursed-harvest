using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Mid-Boss for World 5 (Wave 10): ECLIPSE HERALD (Shadow of the Dying Sun).
    /// Elusive obsidian shadow harbinger.
    /// Attacks:
    /// 1. Void Teleport: Vanishes in an umbral burst and teleports into tactical flanking position (7m from player).
    /// 2. Eclipse Ray: Linear beam of concentrated darkness (10m length, 1.2m width, 30 dmg, 0.65s telegraph line).
    /// 3. Umbral Shockwave: 360° void rupture (7.5m radius, 24 dmg, 0.9s expanding ring) triggered at 50% HP and on timer.
    /// 4. Void Rift Pool: Summons a persistent area-denial hazard pool at player's location.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EclipseHeraldBoss : BossController
    {
        [Header("Eclipse Herald Combat Configuration")]
        [SerializeField] private float eclipseRayRange = 10.0f;
        [SerializeField] private float eclipseRayWidth = 1.2f;
        [SerializeField] private float eclipseRayDamage = 30.0f;
        [SerializeField] private float eclipseRayTelegraphDuration = 0.65f;
        [SerializeField] private float eclipseRayCooldown = 5.5f;

        [Header("Void Teleport")]
        [SerializeField] private float teleportCooldown = 8.5f;
        [SerializeField] private float teleportDistance = 7.0f;

        [Header("Umbral Shockwave")]
        [SerializeField] private float shockwaveRadius = 7.5f;
        [SerializeField] private float shockwaveDamage = 24.0f;
        [SerializeField] private float shockwaveTelegraphDuration = 0.9f;
        [SerializeField] private float shockwaveCooldown = 12.0f;

        [Header("Void Rift Spawn")]
        [SerializeField] private float voidRiftCooldown = 7.0f;
        [SerializeField] private GameObject voidRiftPrefab;

        [Header("Movement & Contact")]
        [SerializeField] private float bossMoveSpeed = 3.2f;
        [SerializeField] private float bossContactDamage = 32.0f;
        [SerializeField] private float bossContactCooldown = 0.8f;
        private float lastContactDamageTime = -10f;

        [Header("Telegraph Colors")]
        [SerializeField] private Color rayColor = new Color(0.75f, 0.25f, 1.0f, 0.95f);
        [SerializeField] private Color shockwaveColor = new Color(0.60f, 0.15f, 0.90f, 0.90f);
        [SerializeField] private Color teleportColor = new Color(0.85f, 0.40f, 1.0f, 0.90f);

        private CharacterController cc;
        private EnemyHealth eh;
        private Transform playerTr;
        private PlayerHealth playerHp;

        private float lastRayTime = -10f;
        private float lastTeleportTime = -10f;
        private float lastShockwaveTime = -10f;
        private float lastVoidRiftTime = -10f;
        private bool isExecutingAction = false;
        private bool triggeredHalfHpShockwave = false;

        private LineRenderer rayTelegraphLine;
        private LineRenderer shockwaveRingLine;

        public float EclipseRayRange => eclipseRayRange;
        public float EclipseRayDamage => eclipseRayDamage;
        public float ShockwaveRadius => shockwaveRadius;
        public float MoveSpeed => bossMoveSpeed;

        public void SetVoidRiftPrefab(GameObject prefab) => voidRiftPrefab = prefab;

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
                eh.SetMaxHealth(3400f);
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
            // Eclipse Ray Line
            GameObject rayObj = new GameObject("Telegraph_EclipseRay");
            rayObj.transform.SetParent(transform, false);
            rayTelegraphLine = rayObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(rayTelegraphLine, rayColor, eclipseRayWidth);
            rayTelegraphLine.positionCount = 2;
            rayTelegraphLine.enabled = false;

            // Shockwave Ring
            GameObject shockObj = new GameObject("Telegraph_Shockwave");
            shockObj.transform.SetParent(transform, false);
            shockwaveRingLine = shockObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(shockwaveRingLine, shockwaveColor, 0.22f);
            int segs = 36;
            shockwaveRingLine.positionCount = segs;
            shockwaveRingLine.loop = true;
            shockwaveRingLine.enabled = false;
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

            // 50% HP threshold milestone shockwave
            float hpPct = eh != null && eh.MaxHealth > 0 ? eh.CurrentHealth / eh.MaxHealth : 1f;
            if (!triggeredHalfHpShockwave && hpPct <= 0.50f)
            {
                triggeredHalfHpShockwave = true;
                StartCoroutine(ExecuteShockwaveRoutine());
                return;
            }

            // Contact damage
            if (dist <= 1.8f && Time.time >= lastContactDamageTime + bossContactCooldown)
            {
                lastContactDamageTime = Time.time;
                if (playerHp != null)
                {
                    playerHp.TakeDamage(new DamageData(bossContactDamage, gameObject, Vector3.zero, 0f, transform.position));
                }
            }

            // AI Decision Priority
            if (Time.time >= lastTeleportTime + teleportCooldown && dist > 11f)
            {
                StartCoroutine(ExecuteTeleportRoutine());
            }
            else if (Time.time >= lastShockwaveTime + shockwaveCooldown && dist <= shockwaveRadius)
            {
                StartCoroutine(ExecuteShockwaveRoutine());
            }
            else if (Time.time >= lastRayTime + eclipseRayCooldown && dist <= eclipseRayRange)
            {
                StartCoroutine(ExecuteEclipseRayRoutine());
            }
            else if (Time.time >= lastVoidRiftTime + voidRiftCooldown && voidRiftPrefab != null)
            {
                StartCoroutine(ExecuteVoidRiftRoutine());
            }
            else
            {
                // Standard Chase
                if (dist > 1.4f && cc != null && cc.enabled)
                {
                    Vector3 moveDir = toPlayer.normalized;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 6f);
                    cc.Move(moveDir * (bossMoveSpeed * Time.deltaTime) + Vector3.down * (9.81f * Time.deltaTime));
                }
            }
        }

        private IEnumerator ExecuteEclipseRayRoutine()
        {
            isExecutingAction = true;
            lastRayTime = Time.time;

            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 aimDir = (playerTr.position - transform.position);
            aimDir.y = 0f;
            aimDir.Normalize();

            if (aimDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(aimDir);
            }

            Vector3 targetEnd = origin + aimDir * eclipseRayRange;

            if (rayTelegraphLine != null)
            {
                rayTelegraphLine.enabled = true;
                rayTelegraphLine.SetPosition(0, origin);
                rayTelegraphLine.SetPosition(1, targetEnd);
            }

            float timer = 0f;
            while (timer < eclipseRayTelegraphDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (rayTelegraphLine != null) rayTelegraphLine.enabled = false;

            // Deal beam damage along the line
            Ray ray = new Ray(origin, aimDir);
            RaycastHit[] hits = Physics.SphereCastAll(ray, eclipseRayWidth * 0.5f, eclipseRayRange);
            foreach (RaycastHit h in hits)
            {
                if (h.collider.CompareTag("Player") && playerHp != null)
                {
                    playerHp.TakeDamage(new DamageData(eclipseRayDamage, gameObject, Vector3.zero, 0f, h.point));
                    break;
                }
            }

            yield return new WaitForSeconds(0.35f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteTeleportRoutine()
        {
            isExecutingAction = true;
            lastTeleportTime = Time.time;

            // Disappear/fade
            yield return new WaitForSeconds(0.2f);

            // Calculate teleport point 7m from player
            Vector2 randCircle = UnityEngine.Random.insideUnitCircle.normalized * teleportDistance;
            Vector3 newPos = playerTr.position + new Vector3(randCircle.x, 0f, randCircle.y);
            newPos.x = Mathf.Clamp(newPos.x, -28f, 28f);
            newPos.z = Mathf.Clamp(newPos.z, -28f, 28f);
            newPos.y = 0.5f;

            if (cc != null) cc.enabled = false;
            transform.position = newPos;
            Vector3 lookAt = (playerTr.position - newPos);
            lookAt.y = 0f;
            if (lookAt.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(lookAt);
            if (cc != null) cc.enabled = true;

            yield return new WaitForSeconds(0.25f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteShockwaveRoutine()
        {
            isExecutingAction = true;
            lastShockwaveTime = Time.time;

            if (shockwaveRingLine != null)
            {
                shockwaveRingLine.enabled = true;
                int segs = 36;
                for (int i = 0; i < segs; i++)
                {
                    float angle = i * Mathf.PI * 2f / segs;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * shockwaveRadius, 0.08f, Mathf.Sin(angle) * shockwaveRadius);
                    shockwaveRingLine.SetPosition(i, transform.position + offset);
                }
            }

            float timer = 0f;
            while (timer < shockwaveTelegraphDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (shockwaveRingLine != null) shockwaveRingLine.enabled = false;

            // Damage
            Vector3 toPlayer = playerTr.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude <= shockwaveRadius && playerHp != null)
            {
                playerHp.TakeDamage(new DamageData(shockwaveDamage, gameObject, Vector3.zero, 0f, transform.position));
            }

            yield return new WaitForSeconds(0.3f);
            isExecutingAction = false;
        }

        private IEnumerator ExecuteVoidRiftRoutine()
        {
            isExecutingAction = true;
            lastVoidRiftTime = Time.time;

            Vector3 spawnTarget = playerTr.position;
            spawnTarget.y = 0.05f;

            yield return new WaitForSeconds(0.4f);

            if (voidRiftPrefab != null)
            {
                Instantiate(voidRiftPrefab, spawnTarget, Quaternion.identity);
            }

            yield return new WaitForSeconds(0.2f);
            isExecutingAction = false;
        }
    }
}
