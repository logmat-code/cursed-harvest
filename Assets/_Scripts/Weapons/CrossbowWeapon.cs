using UnityEngine;

namespace CursedHarvest
{
    public class CrossbowWeapon : BaseWeapon
    {
        [Header("Crossbow Specifics")]
        [SerializeField] private ObjectPool boltPool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform visualBowModel;
        [SerializeField] private float spreadAngle = 12f;

        private Vector3 bowOriginalLocalPos;

        private void Awake()
        {
            InitializeStats();

            if (visualBowModel != null)
            {
                bowOriginalLocalPos = visualBowModel.localPosition;
            }
        }

        private void InitializeStats()
        {
            weaponId = "crossbow";
            weaponName = "Crossbow";
            baseDamage = 30f;
            baseCooldown = 0.9f;
            baseRange = 14f;
            baseProjectileSpeed = 26f;
            baseMultishot = 1;
            basePierce = 1;
        }

        public override void Initialize(WeaponManager manager)
        {
            InitializeStats();
            base.Initialize(manager);
        }

        public void SetBoltPool(ObjectPool pool)
        {
            boltPool = pool;
        }

        public void SetFirePoint(Transform point)
        {
            firePoint = point;
        }

        protected override bool TryFire(Transform playerTransform)
        {
            Vector3 origin = firePoint != null ? firePoint.position : playerTransform.position + Vector3.up * 1f;

            EnemyController target = EnemyRegistry.GetClosestEnemy(origin, Range);
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            Vector3 targetPos = target.transform.position + Vector3.up * 0.8f;
            Vector3 fireDir = (targetPos - origin).normalized;
            fireDir.y = 0f; // Keep shots horizontal along the playfield
            if (fireDir.sqrMagnitude < 0.001f) fireDir = playerTransform.forward;
            fireDir.Normalize();

            // Face player towards target
            PlayerController pc = playerTransform.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.FaceDirection(fireDir);
            }

            int count = Multishot;
            float totalSpread = (count - 1) * spreadAngle;
            float startAngle = -totalSpread * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float currentAngle = startAngle + i * spreadAngle;
                Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.up) * Quaternion.LookRotation(fireDir, Vector3.up);

                if (boltPool != null)
                {
                    GameObject boltObj = boltPool.Get(origin, rotation);
                    Projectile proj = boltObj.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(Damage, ProjectileSpeed, 4f, Pierce, playerTransform.gameObject);
                    }
                }
            }

            AudioManager.Instance?.PlaySound(SoundType.CrossbowShoot, 0.7f, 0.05f);

            // Quick recoil visual kick
            if (visualBowModel != null)
            {
                visualBowModel.localPosition = bowOriginalLocalPos - Vector3.forward * 0.15f;
            }

            return true;
        }

        private void LateUpdate()
        {
            if (visualBowModel != null)
            {
                visualBowModel.localPosition = Vector3.Lerp(visualBowModel.localPosition, bowOriginalLocalPos, Time.deltaTime * 12f);
            }
        }
    }
}
