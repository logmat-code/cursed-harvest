using UnityEngine;

namespace CursedHarvest
{
    public class PlaguePuddle : MonoBehaviour
    {
        [SerializeField] private float duration = 5f;
        [SerializeField] private float damageRadius = 2.2f;
        [SerializeField] private float damagePerTick = 6f;
        [SerializeField] private float tickInterval = 0.5f;

        private float spawnTime;
        private float nextDamageTime;
        private Transform playerTransform;
        private PlayerHealth playerHealth;

        public void Initialize(float puddleDuration, float radius, float tickDamage)
        {
            duration = puddleDuration;
            damageRadius = radius;
            damagePerTick = tickDamage;
            spawnTime = Time.time;
            nextDamageTime = Time.time + tickInterval;

            FindPlayer();
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
                }
            }
        }

        private void Update()
        {
            if (Time.time >= spawnTime + duration)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.time >= nextDamageTime)
            {
                nextDamageTime = Time.time + tickInterval;
                FindPlayer();

                if (playerTransform != null && playerHealth != null && playerHealth.IsAlive)
                {
                    float dist = Vector3.Distance(transform.position, playerTransform.position);
                    if (dist <= damageRadius)
                    {
                        float effectiveDmg = damagePerTick * WorldDifficultyConfig.Current.enemyDamageMultiplier;
                        DamageData dmg = new DamageData(effectiveDmg, gameObject, Vector3.up, 0f, transform.position);
                        playerHealth.TakeDamage(dmg);
                    }
                }
            }
        }
    }
}
