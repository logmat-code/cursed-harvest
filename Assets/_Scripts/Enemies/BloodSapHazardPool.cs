using UnityEngine;

namespace CursedHarvest
{
    /// <summary>
    /// Area-denial blood sap hazard pool created by the Bloodwood Sovereign in World 4.
    /// Persistent viscous corrosive pool (3.2m diameter, 7s duration, 8 dmg/sec, slows player by 25%).
    /// </summary>
    public class BloodSapHazardPool : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private float duration = 7.0f;
        [SerializeField] private float damageRadius = 1.6f;
        [SerializeField] private float damagePerSecond = 8.0f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float slowFactor = 0.75f;

        [Header("Visuals")]
        [SerializeField] private Color sapColor = new Color(0.85f, 0.12f, 0.10f, 0.90f);
        [SerializeField] private Color darkAmberColor = new Color(0.40f, 0.08f, 0.05f, 0.95f);

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

        public void Initialize(float poolDuration = 7.0f, float radius = 1.6f, float dps = 8.0f)
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
            GameObject ringObj = new GameObject("BloodSapRing");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            ringRenderer = ringObj.AddComponent<LineRenderer>();
            ringRenderer.useWorldSpace = false;
            ringRenderer.loop = true;
            ringRenderer.startWidth = 0.18f;
            ringRenderer.endWidth = 0.18f;

            int segments = 36;
            ringRenderer.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                ringRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * damageRadius, 0f, Mathf.Sin(angle) * damageRadius));
            }

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = sapColor;
            ringRenderer.material = mat;
            ringRenderer.startColor = sapColor;
            ringRenderer.endColor = sapColor;

            // Ground glow light
            GameObject lightObj = new GameObject("BloodSapLight");
            lightObj.transform.SetParent(transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.95f, 0.15f, 0.08f);
            l.intensity = 1.8f;
            l.range = damageRadius * 2.2f;
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

            float dist = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                                          new Vector3(playerTransform.position.x, 0f, playerTransform.position.z));

            if (dist <= damageRadius)
            {
                isPlayerInside = true;
                if (Time.time >= nextDamageTime)
                {
                    nextDamageTime = Time.time + tickInterval;
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(new DamageData(damagePerSecond * tickInterval, gameObject, Vector3.zero, 0f, transform.position));
                    }
                }
            }
            else
            {
                isPlayerInside = false;
            }
        }

        private void OnDestroy()
        {
            // Clean up player slow if applicable
        }
    }
}
