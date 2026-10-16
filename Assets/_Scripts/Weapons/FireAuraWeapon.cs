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

        private GameObject auraVfxObj;
        private ParticleSystem auraParticles;
        private Light auraLight;
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

            // BUG FIX: Do NOT create or activate visual effects if this weapon is currently disabled / locked!
            if (Application.isPlaying && enabled)
            {
                SetupAuraVfx();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SetupAuraVfx();
                if (auraVfxObj != null) auraVfxObj.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (auraVfxObj != null)
            {
                auraVfxObj.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (auraVfxObj != null)
            {
                Destroy(auraVfxObj);
                auraVfxObj = null;
            }
        }

        public override void UpdateWeapon(float deltaTime, Transform playerTransform)
        {
            // Level Up / Modal Isolation: Hide aura VFX during any active UI modal so it does not distract
            if (UIManager.Instance != null && UIManager.Instance.IsModalOpen)
            {
                if (auraVfxObj != null && auraVfxObj.activeSelf)
                {
                    auraVfxObj.SetActive(false);
                }
                return;
            }
            else
            {
                if (auraVfxObj != null && !auraVfxObj.activeSelf && enabled)
                {
                    auraVfxObj.SetActive(true);
                }
            }

            base.UpdateWeapon(deltaTime, playerTransform);
        }

        private void SetupAuraVfx()
        {
            if (auraVfxObj != null) return;

            auraVfxObj = new GameObject("FireAura_SubtleVfx");
            auraVfxObj.transform.SetParent(transform, false);
            auraVfxObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            // Subtle organic ember particle system around the perimeter
            auraParticles = auraVfxObj.AddComponent<ParticleSystem>();
            var main = auraParticles.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
            main.maxParticles = 35;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Emission: gentle rate
            var emission = auraParticles.emission;
            emission.rateOverTime = 16f;

            // Shape: Circle with radius = Range and thin donut band so embers emerge at the boundary
            var shape = auraParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Range;
            shape.radiusThickness = 0.25f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            // Color Over Lifetime: warm gold to deep red-amber to transparent
            var colorLife = auraParticles.colorOverLifetime;
            colorLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.70f, 0.20f), 0f),
                    new GradientColorKey(new Color(0.90f, 0.35f, 0.08f), 0.6f),
                    new GradientColorKey(new Color(0.50f, 0.10f, 0.05f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.48f, 0.2f),
                    new GradientAlphaKey(0.25f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorLife.color = grad;

            // Size Over Lifetime: shrink slightly as embers ascend
            var sizeLife = auraParticles.sizeOverLifetime;
            sizeLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.8f);
            sizeCurve.AddKey(0.3f, 1.0f);
            sizeCurve.AddKey(1f, 0.2f);
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Particle renderer material
            var pRenderer = auraVfxObj.GetComponent<ParticleSystemRenderer>();
            Material emberMat = null;
#if UNITY_EDITOR
            emberMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Synty/PolygonGeneric/Materials/FX/Fire/Gen_Ember_01.mat");
            if (emberMat == null)
            {
                emberMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SimpleFX/Materials/FX_Glow.mat");
            }
#endif
            if (emberMat != null)
            {
                pRenderer.material = emberMat;
            }

            // Warm ambient ground light
            GameObject lightChild = new GameObject("AuraWarmLight");
            lightChild.transform.SetParent(auraVfxObj.transform, false);
            lightChild.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            auraLight = lightChild.AddComponent<Light>();
            auraLight.type = LightType.Point;
            auraLight.color = new Color(1f, 0.52f, 0.16f);
            auraLight.range = Range * 1.15f;
            auraLight.intensity = 0.80f;

            UpdateAuraVfxParameters();
        }

        private void UpdateAuraVfxParameters()
        {
            if (auraParticles != null)
            {
                var shape = auraParticles.shape;
                shape.radius = Range;
            }
            if (auraLight != null)
            {
                auraLight.range = Range * 1.15f;
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

            UpdateAuraVfxParameters();
        }
    }
}
