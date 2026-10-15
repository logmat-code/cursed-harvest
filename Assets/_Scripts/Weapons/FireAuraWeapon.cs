using UnityEngine;

namespace CursedHarvest
{
    public class FireAuraWeapon : BaseWeapon
    {
        [Header("Aura Configuration")]
        [SerializeField] private GameObject auraVfxPrefab;
        [SerializeField] private float auraRadius = 3.8f;
        [SerializeField] private float burnTickInterval = 0.45f;
        [SerializeField] private float auraKnockback = 1.5f;

        private GameObject activeVfxInstance;
        private LayerMask enemyMask;

        public void SetAuraVfx(GameObject prefab) => auraVfxPrefab = prefab;

        private void Reset()
        {
            weaponId = "fire_aura";
            weaponName = "Cursed Fire Aura";
            baseDamage = 16f;
            baseCooldown = 0.45f;
            baseRange = 3.8f;
            baseMultishot = 1;
            basePierce = 999;
        }

        public override void Initialize(WeaponManager manager)
        {
            weaponId = "fire_aura";
            weaponName = "Cursed Fire Aura";
            baseDamage = 16f;
            baseCooldown = burnTickInterval;
            baseRange = auraRadius;
            base.Initialize(manager);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            enemyMask = enemyLayer >= 0 ? (1 << enemyLayer) : ~0;

            SpawnAuraVfx();
        }

        private void OnEnable()
        {
            SpawnAuraVfx();
        }

        private void OnDisable()
        {
            if (activeVfxInstance != null)
            {
                Destroy(activeVfxInstance);
                activeVfxInstance = null;
            }
        }

        private void SpawnAuraVfx()
        {
            if (activeVfxInstance == null && auraVfxPrefab != null)
            {
                activeVfxInstance = Instantiate(auraVfxPrefab, transform);
                activeVfxInstance.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                activeVfxInstance.transform.localScale = Vector3.one * (Range / 2.5f);
            }
        }

        protected override bool TryFire(Transform playerTransform)
        {
            float currentRadius = Range;
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, currentRadius, enemyMask);

            if (hits.Length == 0) return false;

            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    Vector3 pushDir = (hits[i].transform.position - playerTransform.position).normalized;
                    pushDir.y = 0.05f;

                    DamageData data = new DamageData(Damage, gameObject, pushDir, auraKnockback);
                    damageable.TakeDamage(data);
                }
            }

            if (hits.Length > 0)
            {
                AudioManager.Instance?.PlaySound(SoundType.FireTick, 0.35f, 0.15f);
            }

            return true;
        }

        protected override void ApplyLevelStats()
        {
            baseDamage += 5f;
            baseRange += 0.4f;
            baseCooldown = Mathf.Max(0.2f, baseCooldown - 0.03f);

            if (activeVfxInstance != null)
            {
                activeVfxInstance.transform.localScale = Vector3.one * (Range / 2.5f);
            }
        }
    }
}
