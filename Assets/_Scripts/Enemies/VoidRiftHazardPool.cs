using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Area-denial void rift hazard pool created by the Eclipse Herald and Primeval Horror in World 5.
    /// Swirling abyssal pool (3.5m diameter, 7.5s duration, 12 dmg/sec, slows player by 30%).
    /// </summary>
    public class VoidRiftHazardPool : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private float duration = 7.5f;
        [SerializeField] private float damageRadius = 1.75f;
        [SerializeField] private float damagePerSecond = 12.0f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float slowFactor = 0.70f;

        [Header("Visuals")]
        [SerializeField] private Color voidColor = new Color(0.65f, 0.20f, 0.95f, 0.90f);
        [SerializeField] private Color darkObsidianColor = new Color(0.15f, 0.05f, 0.25f, 0.95f);

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

        public void Initialize(float poolDuration = 7.5f, float radius = 1.75f, float dps = 12.0f)
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
            // Boundary Ring
            GameObject ringObj = new GameObject("VoidRiftRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            ringRenderer = ringObj.AddComponent<LineRenderer>();
            ringRenderer.useWorldSpace = false;
            ringRenderer.loop = true;
            ringRenderer.startWidth = 0.20f;
            ringRenderer.endWidth = 0.20f;

            int segments = 40;
            ringRenderer.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                ringRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * damageRadius, 0f, Mathf.Sin(angle) * damageRadius));
            }

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = voidColor;
            ringRenderer.material = mat;
            ringRenderer.startColor = voidColor;
            ringRenderer.endColor = voidColor;

            // Ground glow light
            GameObject lightObj = new GameObject("VoidRiftLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.40f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.70f, 0.25f, 1.0f);
            l.intensity = 2.2f;
            l.range = damageRadius * 2.4f;
        }

        private void Update()
        {
            float elapsed = Time.time - spawnTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            FindPlayer();
            if (playerTransform == null) return;

            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (dist <= damageRadius)
            {
                if (!isPlayerInside)
                {
                    isPlayerInside = true;
                    if (playerController != null)
                    {
                        playerController.SetSpeedMultiplier(slowFactor);
                    }
                }

                if (Time.time >= nextDamageTime)
                {
                    nextDamageTime = Time.time + tickInterval;
                    float tickDmg = damagePerSecond * tickInterval;
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(new DamageData(tickDmg, gameObject, Vector3.zero, 0f, transform.position));
                    }
                }
            }
            else
            {
                if (isPlayerInside)
                {
                    isPlayerInside = false;
                    if (playerController != null)
                    {
                        playerController.SetSpeedMultiplier(1.0f);
                    }
                }
            }

            // Pulse ring and light
            float pulse = 0.85f + Mathf.PingPong(Time.time * 2.5f, 0.3f);
            if (ringRenderer != null)
            {
                Color c = voidColor;
                c.a = (1f - (elapsed / duration)) * 0.95f;
                ringRenderer.startColor = c;
                ringRenderer.endColor = c;
            }
        }

        private void OnDestroy()
        {
            if (isPlayerInside && playerController != null)
            {
                playerController.SetSpeedMultiplier(1.0f);
            }
        }
    }
}
