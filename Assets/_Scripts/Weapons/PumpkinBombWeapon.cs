using UnityEngine;

namespace CursedHarvest
{
    public class PumpkinBombWeapon : BaseWeapon
    {
        [Header("Pumpkin Bomb Specific")]
        [SerializeField] private ObjectPool bombPool;
        [SerializeField] private GameObject explosionVfxPrefab;
        [SerializeField] private LayerMask enemyLayerMask;
        [SerializeField] private float blastKnockback = 8f;

        public void SetBombPool(ObjectPool pool) => bombPool = pool;
        public void SetExplosionVfx(GameObject vfx) => explosionVfxPrefab = vfx;

        private void Reset()
        {
            weaponId = "pumpkin_bomb";
            weaponName = "Pumpkin Bomb";
            baseDamage = 45f;
            baseCooldown = 2.2f;
            baseRange = 15f;
            baseProjectileSpeed = 16f;
            baseMultishot = 1;
            basePierce = 1;
        }

        public override void Initialize(WeaponManager manager)
        {
            weaponId = "pumpkin_bomb";
            weaponName = "Pumpkin Bomb";
            baseDamage = 45f;
            baseCooldown = 2.2f;
            baseRange = 15f;
            baseProjectileSpeed = 16f;
            base.Initialize(manager);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            enemyLayerMask = enemyLayer >= 0 ? (1 << enemyLayer) : ~0;
        }

        protected override bool TryFire(Transform playerTransform)
        {
            if (bombPool == null) return false;

            EnemyController closestEnemy = EnemyRegistry.GetClosestEnemy(playerTransform.position, Range);
            if (closestEnemy == null) return false;

            Vector3 baseTarget = closestEnemy.transform.position;
            Vector3 origin = playerTransform.position + Vector3.up * 1.2f;

            int count = Multishot;
            for (int i = 0; i < count; i++)
            {
                Vector3 target = baseTarget;
                if (i > 0)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * 3f;
                    target += new Vector3(offset.x, 0f, offset.y);
                }

                GameObject bombObj = bombPool.Get(origin, Quaternion.identity);
                PumpkinBombProjectile proj = bombObj.GetComponent<PumpkinBombProjectile>();
                if (proj != null)
                {
                    proj.Initialize(origin, target, ProjectileSpeed, Damage, blastKnockback, enemyLayerMask, explosionVfxPrefab);
                }
            }

            return true;
        }

        protected override void ApplyLevelStats()
        {
            baseDamage += 12f;
            baseCooldown = Mathf.Max(1.0f, baseCooldown - 0.2f);
            if (currentLevel % 2 == 0)
            {
                baseMultishot++;
            }
        }
    }
}
