using UnityEngine;

namespace CursedHarvest
{
    public abstract class BaseWeapon : MonoBehaviour
    {
        [Header("Weapon Info")]
        [SerializeField] protected string weaponId = "weapon";
        [SerializeField] protected string weaponName = "Weapon";
        [SerializeField] protected int currentLevel = 1;

        [Header("Base Stats")]
        [SerializeField] protected float baseDamage = 25f;
        [SerializeField] protected float baseCooldown = 1.0f;
        [SerializeField] protected float baseRange = 14f;
        [SerializeField] protected float baseProjectileSpeed = 24f;
        [SerializeField] protected int baseMultishot = 1;
        [SerializeField] protected int basePierce = 1;

        protected float lastFireTime = -10f;
        protected WeaponManager weaponManager;

        public string WeaponId => weaponId;
        public string WeaponName => weaponName;
        public int Level => currentLevel;

        public virtual float Damage => baseDamage * (weaponManager != null ? weaponManager.GlobalDamageMultiplier : 1f);
        public virtual float Cooldown => Mathf.Max(0.1f, baseCooldown * (weaponManager != null ? weaponManager.GlobalCooldownMultiplier : 1f));
        public virtual float Range => baseRange * (weaponManager != null ? weaponManager.GlobalRangeMultiplier : 1f);
        public virtual float ProjectileSpeed => baseProjectileSpeed * (weaponManager != null ? weaponManager.GlobalProjectileSpeedMultiplier : 1f);
        public virtual int Multishot => baseMultishot + (weaponManager != null ? weaponManager.GlobalBonusProjectiles : 0);
        public virtual int Pierce => basePierce + (weaponManager != null ? weaponManager.GlobalBonusPierce : 0);

        public virtual void Initialize(WeaponManager manager)
        {
            weaponManager = manager;
            lastFireTime = -Cooldown; // Ready immediately
        }

        public virtual void Upgrade()
        {
            currentLevel++;
            ApplyLevelStats();
        }

        protected virtual void ApplyLevelStats()
        {
            // Subclasses can implement specific scaling per level
        }

        public virtual void UpdateWeapon(float deltaTime, Transform playerTransform)
        {
            if (Time.time >= lastFireTime + Cooldown)
            {
                if (TryFire(playerTransform))
                {
                    lastFireTime = Time.time;
                }
            }
        }

        protected abstract bool TryFire(Transform playerTransform);
    }
}
