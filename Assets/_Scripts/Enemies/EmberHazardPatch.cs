using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Area-denial hazard created by The Ashen Reaper in World 2.
    /// Lobs a charred ember chunk that leaves a 3.0m diameter burning ground patch.
    /// Deals 8 damage per second for 5.0 seconds.
    /// </summary>
    public class EmberHazardPatch : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private float duration = 5.0f;
        [SerializeField] private float damageRadius = 1.5f; // 3.0m diameter
        [SerializeField] private float damagePerSecond = 8.0f;
        [SerializeField] private float tickInterval = 0.5f;

        [Header("Visuals")]
        [SerializeField] private Color emberColor = new Color(1.0f, 0.35f, 0.08f, 0.85f);
        [SerializeField] private Light emberLight;
        [SerializeField] private ParticleSystem emberParticles;

        private float spawnTime;
        private float nextDamageTime;
        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private LineRenderer ringRenderer;

        public float Duration => duration;
        public float DamageRadius => damageRadius;
        public float DamagePerSecond => damagePerSecond;

        public void Initialize(float patchDuration = 5.0f, float radius = 1.5f, float dps = 8.0f)
        {
            duration = patchDuration;
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
                }
            }
        }

        private void CreateVisuals()
        {
            // 1. Burning boundary ring & ground scorch mark
            GameObject ringObj = new GameObject("EmberRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            ringRenderer = ringObj.AddComponent<LineRenderer>();
            ringRenderer.useWorldSpace = false;
            ringRenderer.loop = true;
            ringRenderer.positionCount = 32;
            ringRenderer.startWidth = 0.09f;
            ringRenderer.endWidth = 0.09f;
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;

            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            ringRenderer.material = lineMat;
            ringRenderer.startColor = emberColor;
            ringRenderer.endColor = emberColor;

            for (int i = 0; i < 32; i++)
            {
                float angle = i * (360f / 32) * Mathf.Deg2Rad;
                ringRenderer.SetPosition(i, new Vector3(Mathf.Sin(angle) * damageRadius, 0f, Mathf.Cos(angle) * damageRadius));
            }

            // 2. Soft glowing point light
            if (emberLight == null)
            {
                GameObject lightObj = new GameObject("EmberPointLight");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                emberLight = lightObj.AddComponent<Light>();
                emberLight.type = LightType.Point;
                emberLight.color = new Color(1.0f, 0.40f, 0.10f);
                emberLight.range = damageRadius * 2.2f;
                emberLight.intensity = 1.4f;
                emberLight.shadows = LightShadows.None;
            }

            // 3. Ground quad for charred ember glow
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "EmberGroundDecal";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * (damageRadius * 2.0f);

            Collider qc = quad.GetComponent<Collider>();
            if (qc != null) Destroy(qc);

            MeshRenderer mr = quad.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material decalMat = new Material(Shader.Find("Sprites/Default"));
                decalMat.color = new Color(0.20f, 0.06f, 0.02f, 0.70f); // Dark scorched ground with faint orange tint
                mr.material = decalMat;
            }
        }

        private void Update()
        {
            float elapsed = Time.time - spawnTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            // Subtle pulsing ember glow
            if (ringRenderer != null)
            {
                float pulse = 0.75f + Mathf.PingPong(Time.time * 4f, 0.35f);
                Color c = emberColor * pulse;
                ringRenderer.startColor = c;
                ringRenderer.endColor = c;
            }

            if (emberLight != null)
            {
                emberLight.intensity = 1.1f + Mathf.PingPong(Time.time * 5f, 0.5f);
            }

            // Area denial damage tick
            if (Time.time >= nextDamageTime)
            {
                nextDamageTime = Time.time + tickInterval;
                FindPlayer();

                if (playerTransform != null && playerHealth != null && playerHealth.IsAlive)
                {
                    Vector3 flatPlayer = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
                    Vector3 flatPatch = new Vector3(transform.position.x, 0f, transform.position.z);
                    float dist = Vector3.Distance(flatPlayer, flatPatch);

                    if (dist <= damageRadius)
                    {
                        float tickDmg = (damagePerSecond * tickInterval) * WorldDifficultyConfig.Current.bossDamageMultiplier;
                        DamageData dmg = new DamageData(tickDmg, gameObject, Vector3.up, 0f, transform.position);
                        playerHealth.TakeDamage(dmg);
                    }
                }
            }
        }
    }
}
