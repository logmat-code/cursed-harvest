using UnityEngine;

namespace CursedHarvest
{
    public class CrossbowWeapon : BaseWeapon
    {
        [Header("Crossbow Specifics")]
        [SerializeField] private ObjectPool boltPool;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform visualBowModel;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private float spreadAngle = 12f;

        private Vector3 bowOriginalLocalPos;
        private EnemyController currentTarget;
        private GameObject targetReticle;
        private LineRenderer reticleLine;
        private PlayerController playerController;
        private PlayerCombatPose playerCombatPose;

        private void Awake()
        {
            InitializeStats();

            if (visualBowModel != null)
            {
                bowOriginalLocalPos = visualBowModel.localPosition;
            }

            playerController = GetComponentInParent<PlayerController>();
            playerCombatPose = GetComponentInParent<PlayerCombatPose>();
            CreateTargetReticle();
        }

        private void InitializeStats()
        {
            weaponId = "crossbow";
            weaponName = "Crossbow";
            baseDamage = 38f;          // Punchy single shot: kills standard grunt cleanly
            baseCooldown = 1.15f;      // Readable, clear firing cadence
            baseRange = 14f;
            baseProjectileSpeed = 26f;
            baseMultishot = 1;
            basePierce = 1;
        }

        public override void Initialize(WeaponManager manager)
        {
            InitializeStats();
            base.Initialize(manager);
            if (playerController == null)
            {
                playerController = GetComponentInParent<PlayerController>();
            }
            if (playerCombatPose == null)
            {
                playerCombatPose = GetComponentInParent<PlayerCombatPose>();
            }
        }

        public void SetBoltPool(ObjectPool pool)
        {
            boltPool = pool;
        }

        public void SetFirePoint(Transform point)
        {
            firePoint = point;
        }

        public void SetVisualBowModel(Transform model)
        {
            visualBowModel = model;
            if (visualBowModel != null)
            {
                bowOriginalLocalPos = visualBowModel.localPosition;
            }
        }

        public void SetMuzzleFlashPrefab(GameObject prefab)
        {
            muzzleFlashPrefab = prefab;
        }

        private void CreateTargetReticle()
        {
            if (targetReticle != null) return;

            targetReticle = new GameObject("TargetReticle_Crossbow");
            targetReticle.hideFlags = HideFlags.DontSave;

            reticleLine = targetReticle.AddComponent<LineRenderer>();
            reticleLine.useWorldSpace = false;
            reticleLine.loop = true;
            reticleLine.positionCount = 4;
            reticleLine.startWidth = 0.05f;
            reticleLine.endWidth = 0.05f;

            // Diamond shape centered at origin
            float r = 0.35f;
            reticleLine.SetPositions(new Vector3[]
            {
                new Vector3(0f, r, 0f),
                new Vector3(r, 0f, 0f),
                new Vector3(0f, -r, 0f),
                new Vector3(-r, 0f, 0f)
            });

            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                reticleLine.material = new Material(defaultShader);
            }
            Color reticleColor = new Color(1f, 0.45f, 0.15f, 0.85f); // Subtle amber/orange indicator
            reticleLine.startColor = reticleColor;
            reticleLine.endColor = reticleColor;

            targetReticle.SetActive(false);
        }

        private void Update()
        {
            UpdateTargetTracking();
        }

        private void UpdateTargetTracking()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1f;
            currentTarget = EnemyRegistry.GetClosestEnemy(origin, Range);

            if (currentTarget != null && currentTarget.IsAlive)
            {
                // Smoothly orient player towards current target
                Vector3 toTarget = (currentTarget.transform.position - transform.position);
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.01f && playerController != null)
                {
                    playerController.FaceDirection(toTarget.normalized);
                }

                // Position reticle above enemy head
                if (targetReticle != null)
                {
                    if (!targetReticle.activeSelf) targetReticle.SetActive(true);
                    targetReticle.transform.position = currentTarget.transform.position + Vector3.up * 2.0f;
                    
                    // Gentle pulse and billboard rotation towards camera
                    float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.12f;
                    targetReticle.transform.localScale = Vector3.one * pulse;

                    if (Camera.main != null)
                    {
                        targetReticle.transform.rotation = Camera.main.transform.rotation;
                    }
                }
            }
            else
            {
                if (targetReticle != null && targetReticle.activeSelf)
                {
                    targetReticle.SetActive(false);
                }
            }
        }

        protected override bool TryFire(Transform playerTransform)
        {
            Vector3 origin = firePoint != null ? firePoint.position : playerTransform.position + Vector3.up * 1f;

            if (currentTarget == null || !currentTarget.IsAlive)
            {
                currentTarget = EnemyRegistry.GetClosestEnemy(origin, Range);
            }

            if (currentTarget == null || !currentTarget.IsAlive)
            {
                return false;
            }

            Vector3 targetPos = currentTarget.transform.position + Vector3.up * 0.8f;
            Vector3 fireDir = (targetPos - origin).normalized;
            fireDir.y = 0f; // Keep trajectory cleanly along horizontal game plane
            if (fireDir.sqrMagnitude < 0.001f) fireDir = playerTransform.forward;
            fireDir.Normalize();

            // Face player towards target
            if (playerController != null)
            {
                playerController.FaceDirection(fireDir);
            }

            // Muzzle flash burst at firePoint
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, Quaternion.LookRotation(fireDir, Vector3.up));
                flash.transform.localScale = Vector3.one * 0.65f;
                Destroy(flash, 0.12f);
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
                        proj.Initialize(Damage, ProjectileSpeed, 4.5f, Pierce, playerTransform.gameObject);
                    }
                }
            }

            AudioManager.Instance?.PlaySound(SoundType.CrossbowShoot, 0.85f, 0.06f);

            // Visual recoil kickback
            if (playerCombatPose != null)
            {
                playerCombatPose.PlayRecoil(0.015f);
            }
            else if (visualBowModel != null)
            {
                visualBowModel.localPosition = bowOriginalLocalPos - Vector3.forward * 0.08f;
            }

            return true;
        }

        private void LateUpdate()
        {
            if (playerCombatPose == null && visualBowModel != null)
            {
                visualBowModel.localPosition = Vector3.Lerp(visualBowModel.localPosition, bowOriginalLocalPos, Time.deltaTime * 14f);
            }
        }

        private void OnDisable()
        {
            if (targetReticle != null)
            {
                targetReticle.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (targetReticle != null)
            {
                Destroy(targetReticle);
                targetReticle = null;
            }
        }
    }
}
