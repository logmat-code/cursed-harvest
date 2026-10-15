using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public class WeaponManager : MonoBehaviour
    {
        [Header("Global Multipliers")]
        [SerializeField] private float globalDamageMultiplier = 1f;
        [SerializeField] private float globalCooldownMultiplier = 1f;
        [SerializeField] private float globalRangeMultiplier = 1f;
        [SerializeField] private float globalProjectileSpeedMultiplier = 1f;
        [SerializeField] private int globalBonusProjectiles = 0;
        [SerializeField] private int globalBonusPierce = 0;

        private readonly List<BaseWeapon> activeWeapons = new List<BaseWeapon>();

        public float GlobalDamageMultiplier => globalDamageMultiplier;
        public float GlobalCooldownMultiplier => globalCooldownMultiplier;
        public float GlobalRangeMultiplier => globalRangeMultiplier;
        public float GlobalProjectileSpeedMultiplier => globalProjectileSpeedMultiplier;
        public int GlobalBonusProjectiles => globalBonusProjectiles;
        public int GlobalBonusPierce => globalBonusPierce;

        public IReadOnlyList<BaseWeapon> ActiveWeapons => activeWeapons;

        public void SetDamageMultiplier(float mult) => globalDamageMultiplier = Mathf.Max(0.1f, mult);
        public void SetCooldownMultiplier(float mult) => globalCooldownMultiplier = Mathf.Max(0.2f, mult);
        public void SetRangeMultiplier(float mult) => globalRangeMultiplier = Mathf.Max(0.2f, mult);
        public void SetProjectileSpeedMultiplier(float mult) => globalProjectileSpeedMultiplier = Mathf.Max(0.2f, mult);
        public void AddBonusProjectiles(int count) => globalBonusProjectiles += count;
        public void AddBonusPierce(int count) => globalBonusPierce += count;

        public void ResetStats()
        {
            globalDamageMultiplier = 1f;
            globalCooldownMultiplier = 1f;
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
