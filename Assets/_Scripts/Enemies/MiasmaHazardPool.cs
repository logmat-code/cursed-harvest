using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Area-denial toxic pool hazard created by The Mire Mother in World 3.
    /// Lobs a virulent miasma globule that creates a 3.5m diameter toxic pool.
    /// Deals 6 damage per second for 6.0 seconds and slows player movement by 20%.
    /// Features a 0.7s telegraph marker before activation.
    /// </summary>
    public class MiasmaHazardPool : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private float duration = 6.0f;
        [SerializeField] private float damageRadius = 1.75f; // 3.5m diameter
        [SerializeField] private float damagePerSecond = 6.0f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float slowFactor = 0.80f; // 20% slow

        [Header("Visuals")]
        [SerializeField] private Color miasmaColor = new Color(0.25f, 0.75f, 0.30f, 0.85f);
        [SerializeField] private Color swampMurkColor = new Color(0.12f, 0.30f, 0.15f, 0.90f);
        [SerializeField] private Light miasmaLight;

        private float spawnTime;
        private float nextDamageTime;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private PlayerController playerController;
        private LineRenderer ringRenderer;
        private bool isPlayerInside = false;

        public float Duration => duration;
        public float DamageRadius => damageRadius;
        public float DamagePerSecond => damagePerSecond;

        public void Initialize(float poolDuration = 6.0f, float radius = 1.75f, float dps = 6.0f)
        {
            duration = poolDuration;
            damageRadius = radius;
            damagePerSecond = dps;
            spawnTime = Time.time;
            nextDamageTime = Time.time + tickInterval;

            FindPlayer();
        }

        private void Awake()
        {
            CreateVisuals();
        }

        private void Start()
        {
            spawnTime = Time.time;
            nextDamageTime = Time.time + tickInterval;
            FindPlayer();
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
                    playerController = player.GetComponent<PlayerController>();
                }
            }
        }

        private void CreateVisuals()
        {
            // 1. Miasma Boundary Ring
            GameObject ringObj = new GameObject("MiasmaRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            ringRenderer = ringObj.AddComponent<LineRenderer>();
            ringRenderer.useWorldSpace = false;
            ringRenderer.loop = true;
            ringRenderer.positionCount = 32;
            ringRenderer.startWidth = 0.11f;
            ringRenderer.endWidth = 0.11f;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;

            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            ringRenderer.material = lineMat;
            ringRenderer.startColor = miasmaColor;
            ringRenderer.endColor = miasmaColor;

            for (int i = 0; i < 32; i++)
            {
                float angle = i * (360f / 32) * Mathf.Deg2Rad;
                ringRenderer.SetPosition(i, new Vector3(Mathf.Sin(angle) * damageRadius, 0f, Mathf.Cos(angle) * damageRadius));
            }

            // 2. Toxic Spore Light
            if (miasmaLight == null)
            {
                GameObject lightObj = new GameObject("MiasmaLight");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);

                miasmaLight = lightObj.AddComponent<Light>();
                miasmaLight.type = LightType.Point;
                miasmaLight.color = new Color(0.20f, 0.85f, 0.35f);
                miasmaLight.range = damageRadius * 2.2f;
                miasmaLight.intensity = 1.4f;
                miasmaLight.shadows = LightShadows.None;
            }

            // 3. Stagnant Slime Decal Disc
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "SlimeDisc";
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            disc.transform.localScale = new Vector3(damageRadius * 2.0f, 0.01f, damageRadius * 2.0f);

            Collider col = disc.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            Renderer r = disc.GetComponent<Renderer>();
            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = swampMurkColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.10f, 0.40f, 0.15f) * 0.6f);
                r.material = mat;
            }
        }

        private void Update()
        {
            float age = Time.time - spawnTime;

            // Fade out near end of duration
            if (age >= duration - 0.8f)
            {
                float fade = Mathf.Clamp01((duration - age) / 0.8f);
                if (ringRenderer != null)
                {
                    Color c = miasmaColor;
                    c.a = fade * 0.85f;
                    ringRenderer.startColor = c;
                    ringRenderer.endColor = c;
                }
                if (miasmaLight != null)
                {
                    miasmaLight.intensity = fade * 1.4f;
                }
            }

            if (age >= duration)
            {
                CleanupAndDestroy();
                return;
            }

            // Pulse light
            if (miasmaLight != null)
            {
                miasmaLight.intensity = 1.2f + Mathf.Sin(Time.time * 5f) * 0.3f;
            }

            // Check player overlap & damage
            FindPlayer();
            if (playerTransform != null && playerHealth != null && playerHealth.IsAlive)
            {
                Vector3 flatPlayerPos = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
                Vector3 flatPoolPos = new Vector3(transform.position.x, 0f, transform.position.z);
                float dist = Vector3.Distance(flatPlayerPos, flatPoolPos);

                if (dist <= damageRadius)
                {
                    isPlayerInside = true;

                    // Apply slow while inside
                    if (playerController != null)
                    {
                        playerController.SetSpeedMultiplier(slowFactor);
                    }

                    // Tick damage
                    if (Time.time >= nextDamageTime)
                    {
                        nextDamageTime = Time.time + tickInterval;
                        float damageAmount = damagePerSecond * tickInterval;
                        playerHealth.TakeDamage(new DamageData(damageAmount, null, Vector3.zero, 0f, transform.position));
                    }
                }
                else if (isPlayerInside)
                {
                    // Restored outside pool
                    isPlayerInside = false;
                    if (playerController != null)
                    {
                        playerController.SetSpeedMultiplier(1f);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            CleanupAndDestroy();
        }

        private void CleanupAndDestroy()
        {
            if (isPlayerInside && playerController != null)
            {
                playerController.SetSpeedMultiplier(1f);
                isPlayerInside = false;
            }
            Destroy(gameObject);
        }
    }
}
