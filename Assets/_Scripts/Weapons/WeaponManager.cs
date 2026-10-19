using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Run Multipliers")]
        [SerializeField] private float runDamageMultiplier = 1f;
        [SerializeField] private float runCooldownMultiplier = 1f;
        [SerializeField] private float temporaryDamageBonus = 0f;
        [SerializeField] private float temporaryCooldownMultiplier = 1f;
        [SerializeField] private float globalRangeMultiplier = 1f;
        [SerializeField] private float globalProjectileSpeedMultiplier = 1f;
        [SerializeField] private int globalBonusProjectiles = 0;
        [SerializeField] private int globalBonusPierce = 0;

        private readonly List<BaseWeapon> activeWeapons = new List<BaseWeapon>();

        public float GlobalDamageMultiplier
        {
            get
            {
                float metaPower = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetPowerMultiplier() : 1f;
                return (metaPower * runDamageMultiplier) + temporaryDamageBonus;
            }
        }

        public float GlobalCooldownMultiplier
        {
            get
            {
                float metaHaste = (MetaProgressionManager.Instance != null) ? MetaProgressionManager.Instance.GetHasteMultiplier() : 1f;
                return Mathf.Max(0.05f, (runCooldownMultiplier * temporaryCooldownMultiplier) / metaHaste);
            }
        }

        public float RunDamageMultiplier => runDamageMultiplier;
        public float RunCooldownMultiplier => runCooldownMultiplier;
        public float GlobalRangeMultiplier => globalRangeMultiplier;
        public float GlobalProjectileSpeedMultiplier => globalProjectileSpeedMultiplier;
        public int GlobalBonusProjectiles => globalBonusProjectiles;
        public int GlobalBonusPierce => globalBonusPierce;

        private void Awake()
        {
            EnsureWeaponsInitialized();
        }

        public void EnsureWeaponsInitialized()
        {
            if (activeWeapons.Count == 0)
            {
                BaseWeapon[] weapons = GetComponents<BaseWeapon>();
                for (int i = 0; i < weapons.Length; i++)
                {
                    AddWeapon(weapons[i]);
                }
            }
        }

        public IReadOnlyList<BaseWeapon> ActiveWeapons
        {
            get
            {
                EnsureWeaponsInitialized();
                return activeWeapons;
            }
        }

        public void SetDamageMultiplier(float mult) => runDamageMultiplier = Mathf.Max(0.1f, mult);
        public void SetTemporaryDamageBonus(float bonus) => temporaryDamageBonus = bonus;
        public void SetCooldownMultiplier(float mult) => runCooldownMultiplier = Mathf.Max(0.1f, mult);
        public void SetTemporaryCooldownMultiplier(float mult) => temporaryCooldownMultiplier = Mathf.Max(0.1f, mult);
        public void SetRangeMultiplier(float mult) => globalRangeMultiplier = Mathf.Max(0.2f, mult);
        public void SetProjectileSpeedMultiplier(float mult) => globalProjectileSpeedMultiplier = Mathf.Max(0.2f, mult);
        public void AddBonusProjectiles(int count) => globalBonusProjectiles += count;
        public void AddBonusPierce(int count) => globalBonusPierce += count;

        public void ResetStats()
        {
            runDamageMultiplier = 1f;
            runCooldownMultiplier = 1f;
            temporaryDamageBonus = 0f;
            temporaryCooldownMultiplier = 1f;
            globalRangeMultiplier = 1f;
            globalProjectileSpeedMultiplier = 1f;
            globalBonusProjectiles = 0;
            globalBonusPierce = 0;
        }

        public void AddWeapon(BaseWeapon weapon)
        {
            if (weapon != null && !activeWeapons.Contains(weapon))
            {
                weapon.Initialize(this);
                activeWeapons.Add(weapon);
            }
        }

        public BaseWeapon GetWeapon(string weaponId)
        {
            EnsureWeaponsInitialized();
            for (int i = 0; i < activeWeapons.Count; i++)
            {
                if (activeWeapons[i].WeaponId == weaponId)
                {
                    return activeWeapons[i];
                }
            }
            return null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Transform pTransform = transform;

            for (int i = 0; i < activeWeapons.Count; i++)
            {
                if (activeWeapons[i] != null && activeWeapons[i].enabled)
                {
                    activeWeapons[i].UpdateWeapon(dt, pTransform);
                }
            }
        }
    }
}
