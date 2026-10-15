using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public class HarvestScytheWeapon : BaseWeapon
    {
        [Header("Scythe Configuration")]
        [SerializeField] private GameObject bladeModelPrefab;
        [SerializeField] private float orbitRadius = 2.4f;
        [SerializeField] private float rotationSpeed = 240f; // degrees per sec
        [SerializeField] private float hitCooldownPerEnemy = 0.35f;
        [SerializeField] private float scytheKnockback = 6f;

        private float currentAngle;
        private readonly List<GameObject> activeBlades = new List<GameObject>();
        private readonly Dictionary<IDamageable, float> enemyHitTimes = new Dictionary<IDamageable, float>();
        private Transform bladesContainer;
        private LayerMask enemyMask;

        public void SetBladeModel(GameObject prefab) => bladeModelPrefab = prefab;

        private void Reset()
        {
            weaponId = "harvest_scythe";
            weaponName = "Harvest Scythe";
            baseDamage = 28f;
            baseCooldown = 0.1f;
            baseRange = 1f; // Used as orbit radius multiplier
            baseMultishot = 2; // 2 orbiting blades
        }

        public override void Initialize(WeaponManager manager)
        {
            weaponId = "harvest_scythe";
            weaponName = "Harvest Scythe";
            baseDamage = 28f;
            baseCooldown = 0.1f;
            baseMultishot = 2;
            base.Initialize(manager);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            enemyMask = enemyLayer >= 0 ? (1 << enemyLayer) : ~0;

            CreateBlades(transform);
        }

        private void OnEnable()
        {
            if (activeBlades.Count == 0 && weaponManager != null)
            {
                CreateBlades(transform);
            }
        }

        private void OnDisable()
        {
            DestroyBlades();
        }

        private void DestroyBlades()
        {
            if (bladesContainer != null)
            {
                Destroy(bladesContainer.gameObject);
                bladesContainer = null;
            }
            activeBlades.Clear();
        }

        private void CreateBlades(Transform parent)
        {
            DestroyBlades();

            GameObject containerObj = new GameObject("ScytheBladesContainer");
            containerObj.transform.SetParent(parent, false);
            containerObj.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            bladesContainer = containerObj.transform;

            int bladeCount = Mathf.Max(1, Multishot);
            float angleStep = 360f / bladeCount;

            for (int i = 0; i < bladeCount; i++)
            {
                GameObject blade = new GameObject($"Blade_{i}");
                blade.transform.SetParent(bladesContainer, false);

                if (bladeModelPrefab != null)
                {
                    GameObject model = Instantiate(bladeModelPrefab, blade.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    model.transform.localScale = Vector3.one * 0.9f;
                }
                else
                {
                    // Fallback visual
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(blade.transform, false);
                    cube.transform.localScale = new Vector3(0.15f, 0.8f, 0.3f);
                    Collider c = cube.GetComponent<Collider>();
                    if (c != null) Destroy(c);
                }

                activeBlades.Add(blade);
            }
        }

        public override void UpdateWeapon(float deltaTime, Transform playerTransform)
        {
            if (bladesContainer == null) CreateBlades(playerTransform);

            // Recreate blades if count changed (e.g. from upgrade)
            int targetCount = Mathf.Max(1, Multishot);
            if (activeBlades.Count != targetCount)
            {
                CreateBlades(playerTransform);
            }

            // Rotate blades
            currentAngle = (currentAngle + rotationSpeed * deltaTime) % 360f;
            float r = orbitRadius * (weaponManager != null ? weaponManager.GlobalRangeMultiplier : 1f);
            float angleStep = 360f / activeBlades.Count;

            for (int i = 0; i < activeBlades.Count; i++)
            {
                if (activeBlades[i] == null) continue;

                float angle = (currentAngle + i * angleStep) * Mathf.Deg2Rad;
                Vector3 localPos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                activeBlades[i].transform.localPosition = localPos;
                activeBlades[i].transform.localRotation = Quaternion.Euler(0f, -(currentAngle + i * angleStep) + 90f, 0f);

                // Blade hit detection via OverlapSphere
                CheckBladeHits(activeBlades[i].transform.position, playerTransform.position);
            }
        }

        private void CheckBladeHits(Vector3 bladePos, Vector3 centerPos)
        {
            Collider[] colliders = Physics.OverlapSphere(bladePos, 0.8f, enemyMask);
            float now = Time.time;

            for (int i = 0; i < colliders.Length; i++)
            {
                IDamageable damageable = colliders[i].GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    if (!enemyHitTimes.TryGetValue(damageable, out float lastHit) || now >= lastHit + hitCooldownPerEnemy)
                    {
                        enemyHitTimes[damageable] = now;
                        Vector3 pushDir = (colliders[i].transform.position - centerPos).normalized;
                        pushDir.y = 0.05f;

                        DamageData data = new DamageData(Damage, gameObject, pushDir, scytheKnockback);
                        damageable.TakeDamage(data);
                        AudioManager.Instance?.PlaySound(SoundType.ScytheHit, 0.75f, 0.1f);
                    }
                }
            }
        }

        protected override bool TryFire(Transform playerTransform)
        {
            // Continuous weapon, handled in UpdateWeapon
            return true;
        }

        protected override void ApplyLevelStats()
        {
            baseDamage += 8f;
            if (currentLevel % 2 == 0)
            {
                baseMultishot++;
                CreateBlades(transform);
            }
        }
    }
}
