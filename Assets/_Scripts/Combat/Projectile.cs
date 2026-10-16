using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 24f;
        [SerializeField] private float maxLifetime = 3f;
        [SerializeField] private float radius = 0.25f;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private GameObject impactVfxPrefab;
        [SerializeField] private TrailRenderer trail;

        private float damage = 25f;
        private float knockback = 4f;
        private int pierceCount = 1;
        private GameObject sourceObject;

        private float spawnTime;
        private int hitsRemaining;
        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        private PooledObjectTracker poolTracker;

        public void SetHitMask(LayerMask mask)
        {
            hitMask = mask;
        }

        public void SetImpactVfx(GameObject vfx)
        {
            impactVfxPrefab = vfx;
        }

        public void Initialize(float damageAmount, float projectileSpeed, float knockbackForce, int pierce, GameObject source)
        {
            damage = damageAmount;
            speed = projectileSpeed;
            knockback = knockbackForce;
            pierceCount = Mathf.Max(1, pierce);
            sourceObject = source;
            hitsRemaining = pierceCount;
        }

        private void Awake()
        {
            poolTracker = GetComponent<PooledObjectTracker>();
            if (trail == null)
            {
                trail = GetComponentInChildren<TrailRenderer>();
            }
        }

        public void OnSpawnFromPool()
        {
            spawnTime = Time.time;
            hitsRemaining = pierceCount;
            damagedTargets.Clear();
            if (trail != null)
            {
                trail.Clear();
            }
        }

        public void OnReturnToPool()
        {
            damagedTargets.Clear();
            if (trail != null)
            {
                trail.Clear();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Vector3 startPos = transform.position;
            Vector3 movement = transform.forward * speed * dt;
            Vector3 targetPos = startPos + movement;

            // SphereCast along movement trajectory to guarantee collision at high speeds
            RaycastHit hit;
            if (Physics.SphereCast(startPos, radius, transform.forward, out hit, movement.magnitude, hitMask, QueryTriggerInteraction.Collide))
            {
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive && !damagedTargets.Contains(damageable))
                {
                    damagedTargets.Add(damageable);
                    DamageData dData = new DamageData(
                        damage,
                        sourceObject,
                        transform.forward,
                        knockback,
                        hit.point
                    );
                    damageable.TakeDamage(dData);
                    TriggerHitEffects(hit.point, hit.normal);

                    hitsRemaining--;
                    if (hitsRemaining <= 0)
                    {
                        Despawn();
                        return;
                    }
                }
            }

            transform.position = targetPos;

            if (Time.time >= spawnTime + maxLifetime)
            {
                Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive && !damagedTargets.Contains(damageable))
            {
                damagedTargets.Add(damageable);
                DamageData dData = new DamageData(
                    damage,
                    sourceObject,
                    transform.forward,
                    knockback,
                    transform.position
                );
                damageable.TakeDamage(dData);
                TriggerHitEffects(transform.position, -transform.forward);

                hitsRemaining--;
                if (hitsRemaining <= 0)
                {
                    Despawn();
                }
            }
        }

        private void TriggerHitEffects(Vector3 hitPos, Vector3 hitNormal)
        {
            if (impactVfxPrefab != null)
            {
                Quaternion rot = hitNormal.sqrMagnitude > 0.01f ? Quaternion.LookRotation(hitNormal, Vector3.up) : Quaternion.identity;
                GameObject vfx = Instantiate(impactVfxPrefab, hitPos, rot);
                vfx.transform.localScale = Vector3.one * 0.45f;
                Destroy(vfx, 0.3f);
            }
            AudioManager.Instance?.PlaySound(SoundType.EnemyHit, 0.75f, 0.08f);
        }

        private void Despawn()
        {
            if (poolTracker != null)
            {
                poolTracker.ReturnToPool();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
