using UnityEngine;

namespace CursedHarvest
{
    public class PumpkinBombProjectile : MonoBehaviour, IPoolable
    {
        [Header("Explosion Settings")]
        [SerializeField] private float blastRadius = 3.5f;
        [SerializeField] private float arcHeight = 2.5f;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private GameObject explosionVfxPrefab;

        private Vector3 startPos;
        private Vector3 targetPos;
        private float travelDuration;
        private float elapsedTime;
        private float damageAmount;
        private float knockbackForce;
        private bool isFlying;
        private PooledObjectTracker poolTracker;

        public void Initialize(Vector3 from, Vector3 to, float speed, float damage, float knockback, LayerMask mask, GameObject vfx)
        {
            startPos = from;
            targetPos = to;
            damageAmount = damage;
            knockbackForce = knockback;
            hitMask = mask;
            if (vfx != null) explosionVfxPrefab = vfx;

            float distance = Vector3.Distance(from, to);
            travelDuration = Mathf.Max(0.2f, distance / Mathf.Max(5f, speed));
            elapsedTime = 0f;
            isFlying = true;
            transform.position = from;
        }

        private void Awake()
        {
            poolTracker = GetComponent<PooledObjectTracker>();
        }

        public void OnSpawnFromPool()
        {
            elapsedTime = 0f;
            isFlying = false;
        }

        public void OnReturnToPool()
        {
            isFlying = false;
        }

        private void Update()
        {
            if (!isFlying) return;

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / travelDuration);

            // Parabolic arc interpolation
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
            currentPos.y += heightOffset;
            transform.position = currentPos;

            // Tumble rotation
            transform.Rotate(Vector3.right * (360f * Time.deltaTime) + Vector3.up * (180f * Time.deltaTime));

            if (t >= 1f)
            {
                Explode();
            }
        }

        public void Explode()
        {
            if (!isFlying && elapsedTime == 0f) return;
            isFlying = false;

            // Visual explosion
            if (explosionVfxPrefab != null)
            {
                Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
            }

            AudioManager.Instance?.PlaySound(SoundType.BombExplode, 1.0f, 0.05f);
            TopDownCameraFollow.Instance?.TriggerShake(0.25f, 0.35f);

            // AoE damage check
            Collider[] colliders = Physics.OverlapSphere(transform.position, blastRadius, hitMask);
            for (int i = 0; i < colliders.Length; i++)
            {
                IDamageable damageable = colliders[i].GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive)
                {
                    Vector3 blastDir = (colliders[i].transform.position - transform.position).normalized;
                    blastDir.y = 0.1f;
                    if (blastDir.sqrMagnitude < 0.001f) blastDir = Vector3.forward;

                    DamageData data = new DamageData(damageAmount, gameObject, blastDir, knockbackForce);
                    damageable.TakeDamage(data);
                }
            }

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
