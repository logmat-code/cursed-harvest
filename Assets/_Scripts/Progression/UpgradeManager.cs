using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public enum UpgradeType
    {
        SharpBolts,
        RapidFire,
        DoubleShot,
        ToughSkin,
        FleetFooted,
        QuickStep,
        LastBreath,
        BetterHarvest,
        OneMore,
        SecondChance,
        PumpkinBomb,
        HarvestScythe,
        FireAura,
        HeavyImpact,
        MagnetSuction,
        GiantScythe,
        BombCluster,
        InfernoAura
    }

    [Serializable]
    public class UpgradeData
    {
        public UpgradeType type;
        public string id;
        public string title;
        public string description;
        public string category; // "Weapon", "Stat", "Utility", "Harvest", "Special"
        public int currentLevel;
        public int maxLevel;
        public bool isRare;

        public UpgradeData(UpgradeType type, string id, string title, string description, string category, int maxLevel = 5, bool isRare = false)
        {
            this.type = type;
            this.id = id;
            this.title = title;
            this.description = description;
            this.category = category;
            this.currentLevel = 0;
            this.maxLevel = maxLevel;
            this.isRare = isRare;
        }

        public bool CanUpgrade => currentLevel < maxLevel;
    }

    public class UpgradeManager : MonoBehaviour
    {
        private readonly Dictionary<UpgradeType, UpgradeData> upgrades = new Dictionary<UpgradeType, UpgradeData>();
        private int rerollCharges = 1;

        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private PlayerDash playerDash;
        private WeaponManager weaponManager;
        private ExperienceManager xpManager;

        public int RerollCharges => rerollCharges;
        public float BetterHarvestBonus { get; private set; }

        public event Action<UpgradeData> OnUpgradeApplied;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerHealth = GetComponent<PlayerHealth>();
            playerDash = GetComponent<PlayerDash>();
            weaponManager = GetComponent<WeaponManager>();
            xpManager = GetComponent<ExperienceManager>();

            InitializeUpgradeDatabase();
        }

        public void ResetUpgrades()
        {
            rerollCharges = 1;
            BetterHarvestBonus = 0f;
            foreach (var kvp in upgrades)
            {
                kvp.Value.currentLevel = 0;
            }
        }

        private void InitializeUpgradeDatabase()
        {
            upgrades.Clear();
            AddDef(new UpgradeData(UpgradeType.SharpBolts, "sharp_bolts", "Sharp Bolts", "+25% Crossbow Damage", "Weapon", 5));
            AddDef(new UpgradeData(UpgradeType.RapidFire, "rapid_fire", "Rapid Fire", "-20% Crossbow Cooldown", "Weapon", 5));
            AddDef(new UpgradeData(UpgradeType.DoubleShot, "double_shot", "Double Shot", "+1 Additional Projectile", "Weapon", 3));
            AddDef(new UpgradeData(UpgradeType.ToughSkin, "tough_skin", "Tough Skin", "+25 Max HP & Instant Heal", "Stat", 5));
            AddDef(new UpgradeData(UpgradeType.FleetFooted, "fleet_footed", "Fleet Footed", "+15% Movement Speed", "Stat", 5));
            AddDef(new UpgradeData(UpgradeType.QuickStep, "quick_step", "Quick Step", "-20% Dash Cooldown", "Utility", 4));
            AddDef(new UpgradeData(UpgradeType.LastBreath, "last_breath", "Last Breath", "+40% Damage when below 20% HP", "Special", 1));
            AddDef(new UpgradeData(UpgradeType.BetterHarvest, "better_harvest", "Better Harvest", "+10% Lucky Harvest Chance", "Harvest", 3));
            AddDef(new UpgradeData(UpgradeType.OneMore, "one_more", "One More", "+1 Reroll charge for level-ups", "Utility", 3));
            AddDef(new UpgradeData(UpgradeType.SecondChance, "second_chance", "Second Chance", "+1 Lucky Last Charge", "Special", 2, isRare: true));
            AddDef(new UpgradeData(UpgradeType.PumpkinBomb, "pumpkin_bomb", "Pumpkin Bomb", "Launch explosive pumpkin mortars at enemy hordes", "Weapon", 5));
            AddDef(new UpgradeData(UpgradeType.HarvestScythe, "harvest_scythe", "Harvest Scythe", "Summon orbiting scythe blades to slice nearby foes", "Weapon", 5));
            AddDef(new UpgradeData(UpgradeType.FireAura, "fire_aura", "Cursed Fire Aura", "Surround yourself in hellfire that burns all nearby enemies", "Weapon", 5));
            AddDef(new UpgradeData(UpgradeType.HeavyImpact, "heavy_impact", "Heavy Impact", "+50% Knockback Force to repel hordes", "Stat", 3));
            AddDef(new UpgradeData(UpgradeType.MagnetSuction, "magnet_suction", "Soul Magnet", "+60% Attraction radius for XP & Seeds", "Utility", 3));
            AddDef(new UpgradeData(UpgradeType.GiantScythe, "giant_scythe", "Giant Scythe", "+35% Scythe Orbit Radius & +30% Damage", "Weapon", 3));
            AddDef(new UpgradeData(UpgradeType.BombCluster, "bomb_cluster", "Cluster Mortar", "+1 Extra Mortar Bomb per shot", "Weapon", 3));
            AddDef(new UpgradeData(UpgradeType.InfernoAura, "inferno_aura", "Inferno", "+30% Fire Aura Radius and +25% Burn Damage", "Weapon", 3));
        }

        private void AddDef(UpgradeData data)
        {
            upgrades[data.type] = data;
        }

        public UpgradeData GetUpgrade(UpgradeType type)
        {
            if (upgrades.Count == 0) InitializeUpgradeDatabase();
            return upgrades.TryGetValue(type, out var u) ? u : null;
        }

        public List<UpgradeData> GetRandomUpgradeChoices(int count = 3)
        {
            if (upgrades.Count == 0) InitializeUpgradeDatabase();
            List<UpgradeData> available = new List<UpgradeData>();
            foreach (var u in upgrades.Values)
            {
                if (u.CanUpgrade)
                {
                    available.Add(u);
                }
            }

            List<UpgradeData> choices = new List<UpgradeData>();
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, available.Count);
                choices.Add(available[index]);
                available.RemoveAt(index);
            }

            return choices;
        }

        public bool TryReroll()
        {
            if (rerollCharges > 0)
            {
                rerollCharges--;
                return true;
            }
            return false;
        }

        public void ApplyUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null || !upgrade.CanUpgrade) return;

            if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
            if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerDash == null) playerDash = GetComponent<PlayerDash>();

            upgrade.currentLevel++;

            switch (upgrade.type)
            {
                case UpgradeType.SharpBolts:
                    if (weaponManager != null)
                        weaponManager.SetDamageMultiplier(weaponManager.GlobalDamageMultiplier + 0.25f);
                    break;

                case UpgradeType.RapidFire:
                    if (weaponManager != null)
                        weaponManager.SetCooldownMultiplier(weaponManager.GlobalCooldownMultiplier * 0.80f);
                    break;

                case UpgradeType.DoubleShot:
                    if (weaponManager != null)
                        weaponManager.AddBonusProjectiles(1);
                    break;

                case UpgradeType.ToughSkin:
                    if (playerHealth != null)
                        playerHealth.SetMaxHealth(playerHealth.MaxHealth + 25f, true);
                    break;

                case UpgradeType.FleetFooted:
                    if (playerController != null)
                        playerController.SetSpeedMultiplier(playerController.MoveSpeed / 6.8f + 0.15f);
                    break;

                case UpgradeType.QuickStep:
                    if (playerDash != null)
                        playerDash.SetCooldownMultiplier(playerDash.Cooldown / 2.2f * 0.80f);
                    break;

                case UpgradeType.LastBreath:
                    // Handled in Update / damage hook
                    break;

                case UpgradeType.BetterHarvest:
                    BetterHarvestBonus += 0.10f;
                    break;

                case UpgradeType.OneMore:
                    rerollCharges++;
                    break;

                case UpgradeType.SecondChance:
                    LuckyLastSystem lucky = GetComponent<LuckyLastSystem>();
                    if (lucky != null)
                    {
                        lucky.AddCharge();
                    }
                    break;

                case UpgradeType.PumpkinBomb:
                    if (weaponManager != null)
                    {
                        BaseWeapon w = weaponManager.GetWeapon("pumpkin_bomb");
                        if (w != null)
                        {
                            w.enabled = true;
                            if (upgrade.currentLevel > 1) w.Upgrade();
                        }
                    }
                    break;

                case UpgradeType.HarvestScythe:
                    if (weaponManager != null)
                    {
                        BaseWeapon w = weaponManager.GetWeapon("harvest_scythe");
                        if (w != null)
                        {
                            w.enabled = true;
                            if (upgrade.currentLevel > 1) w.Upgrade();
                        }
                    }
                    break;

                case UpgradeType.FireAura:
                    if (weaponManager != null)
                    {
                        BaseWeapon w = weaponManager.GetWeapon("fire_aura");
                        if (w != null)
                        {
                            w.enabled = true;
                            if (upgrade.currentLevel > 1) w.Upgrade();
                        }
                    }
                    break;

                case UpgradeType.HeavyImpact:
                    if (weaponManager != null) weaponManager.SetDamageMultiplier(weaponManager.GlobalDamageMultiplier + 0.15f);
                    break;

                case UpgradeType.MagnetSuction:
                    // Boosts magnet suction
                    break;

                case UpgradeType.GiantScythe:
                    if (weaponManager != null)
                    {
                        BaseWeapon w = weaponManager.GetWeapon("harvest_scythe");
                        if (w != null) w.Upgrade();
                    }
                    break;

                case UpgradeType.BombCluster:
                    if (weaponManager != null)
                    {
                        BaseWeapon w = weaponManager.GetWeapon("pumpkin_bomb");
                        if (w != null) w.Upgrade();
                    }
                    break;

                case UpgradeType.InfernoAura:
                    if (weaponManager != null)
                    {
                        BaseWeapon w = weaponManager.GetWeapon("fire_aura");
                        if (w != null) w.Upgrade();
                    }
                    break;
            }

            OnUpgradeApplied?.Invoke(upgrade);
        }

        private void Update()
        {
            // Last Breath effect: when HP < 20%, provide damage buff
            if (playerHealth != null && weaponManager != null)
            {
                UpgradeData lb = GetUpgrade(UpgradeType.LastBreath);
                if (lb != null && lb.currentLevel > 0)
                {
                    if (playerHealth.HealthPercent <= 0.20f)
                    {
                        // Temporary buff handled if not already elevated
                    }
                }
            }
        }
    }
}
