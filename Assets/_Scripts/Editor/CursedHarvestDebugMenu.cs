using UnityEditor;
using UnityEngine;

namespace CursedHarvest.Editor
{
    public static class CursedHarvestDebugMenu
    {
        [MenuItem("Cursed Harvest/Debug/Unlock All 5 Worlds")]
        public static void UnlockAllWorlds()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.UnlockAllWorlds();
                Debug.Log("[CursedHarvestDebugMenu] All 5 Worlds Unlocked via Editor Menu!");
            }
            else
            {
                PlayerPrefs.SetInt("CH_HighestUnlockedWorld", 5);
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] PlayerPrefs CH_HighestUnlockedWorld set to 5.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.SelectWorld(1);
            }
        }

        [MenuItem("Cursed Harvest/Debug/Reset World Progress to W1")]
        public static void ResetWorldProgress()
        {
            if (GameSession.Instance != null)
            {
                GameSession.Instance.ResetProgress();
                Debug.Log("[CursedHarvestDebugMenu] World Progress reset to World 1 via Editor Menu!");
            }
            else
            {
                PlayerPrefs.SetInt("CH_HighestUnlockedWorld", 1);
                for (int i = 1; i <= 5; i++)
                {
                    PlayerPrefs.SetInt($"CH_WorldCompleted_{i}", 0);
                }
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] PlayerPrefs World Progress reset to World 1.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.SelectWorld(1);
            }
        }

        // ==================== META PROGRESSION DEBUG TOOLS ====================

        [MenuItem("Cursed Harvest/Debug/Meta Progression/Add 1000 Essence")]
        public static void Add1000Essence()
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.AddEssence(1000, false);
                Debug.Log($"[CursedHarvestDebugMenu] Added 1,000 Harvest Essence. Total: {MetaProgressionManager.Instance.HarvestEssence}");
            }
            else
            {
                int cur = PlayerPrefs.GetInt("CH_HarvestEssence", 0);
                PlayerPrefs.SetInt("CH_HarvestEssence", cur + 1000);
                PlayerPrefs.Save();
                Debug.Log($"[CursedHarvestDebugMenu] Added 1,000 Harvest Essence to PlayerPrefs. Total: {cur + 1000}");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.RefreshUpgradesUI();
            }
        }

        [MenuItem("Cursed Harvest/Debug/Meta Progression/Reset Meta Upgrades")]
        public static void ResetMetaUpgrades()
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.ResetAllProgression();
                Debug.Log("[CursedHarvestDebugMenu] All Meta Upgrades and Essence reset!");
            }
            else
            {
                PlayerPrefs.SetInt("CH_HarvestEssence", 0);
                foreach (MetaUpgradeBranch branch in System.Enum.GetValues(typeof(MetaUpgradeBranch)))
                {
                    PlayerPrefs.SetInt($"CH_Meta_{branch}", 0);
                    PlayerPrefs.SetInt($"CH_MetaUpgrade_{branch}", 0);
                }
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] Meta Upgrades & Essence reset in PlayerPrefs.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.RefreshUpgradesUI();
            }
        }

        [MenuItem("Cursed Harvest/Debug/Meta Progression/Set All Meta Lv5")]
        public static void SetAllMetaLv5()
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.SetAllMaxRank();
                Debug.Log("[CursedHarvestDebugMenu] All 8 Meta Upgrades set to Level 5 (MAX)!");
            }
            else
            {
                foreach (MetaUpgradeBranch branch in System.Enum.GetValues(typeof(MetaUpgradeBranch)))
                {
                    PlayerPrefs.SetInt($"CH_Meta_{branch}", 5);
                    PlayerPrefs.SetInt($"CH_MetaUpgrade_{branch}", 5);
                }
                PlayerPrefs.Save();
                Debug.Log("[CursedHarvestDebugMenu] Set All Meta Upgrades to Level 5 in PlayerPrefs.");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.RefreshUpgradesUI();
            }
        }

        [MenuItem("Cursed Harvest/Debug/Meta Progression/Print Effective Player Stats")]
        public static void PrintEffectivePlayerStats()
        {
            int worldId = GameSession.Instance != null ? GameSession.Instance.SelectedWorldId : 1;
            WorldConfig wc = WorldConfig.GetConfig(worldId);
            string worldName = wc != null ? wc.displayName : $"World {worldId}";

            GameObject player = GameObject.FindWithTag("Player");
            PlayerHealth ph = player != null ? player.GetComponent<PlayerHealth>() : null;
            PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;
            WeaponManager wm = player != null ? player.GetComponent<WeaponManager>() : null;
            LuckyLastSystem lls = player != null ? player.GetComponent<LuckyLastSystem>() : null;
            HarvestPlot hp = Object.FindFirstObjectByType<HarvestPlot>();

            float baseHp = ph != null ? ph.BaseMaxHealth : 100f;
            float metaHp = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetVitalityBonus() : 0f;
            float finalMaxHp = ph != null ? ph.MaxHealth : (baseHp + metaHp);

            float baseDmg = 1.0f;
            float metaPower = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetPowerMultiplier() : 1f;
            float finalDmg = wm != null ? wm.GlobalDamageMultiplier : metaPower;

            float speed = pc != null ? pc.MoveSpeed : 6.8f * (MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetAgilityMultiplier() : 1f);
            float haste = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetHasteMultiplier() : 1f;
            float luckyLastDur = lls != null ? lls.TotalDuration : 12f + (MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetLuckyLastDurationBonus() : 0f);
            float luckyLastRecoveryHP = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetLuckyLastRecoveryHPBonus() : 0f;
            float harvestTime = hp != null ? hp.EffectiveGrowDuration : 3.5f * (MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetHarvestChannelMultiplier() : 1f);
            float harvestSpeedMult = MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetHarvestSpeedMultiplier() : 1f;

            Debug.Log($"==================== EFFECTIVE PLAYER STATS ====================\n" +
                      $"World: {worldId} - {worldName}\n" +
                      $"Base HP: {baseHp:F1} | Meta HP Bonus: +{metaHp:F1} | Final Max HP: {finalMaxHp:F1}\n" +
                      $"Base Damage Mult: {baseDmg:F2}x | Meta Power Mult: {metaPower:F2}x | Final Damage Mult: {finalDmg:F2}x\n" +
                      $"Movement Speed: {speed:F2} m/s (Agility Mult: {(MetaProgressionManager.Instance != null ? MetaProgressionManager.Instance.GetAgilityMultiplier() : 1f):F2}x)\n" +
                      $"Haste Attack Speed Mult: {haste:F2}x (Cooldown Factor: {(1f/haste):F2}x)\n" +
                      $"Lucky Last Duration: {luckyLastDur:F1}s | Extra Recovery HP: +{luckyLastRecoveryHP:F0} HP\n" +
                      $"Harvest Channel Duration: {harvestTime:F2}s ({harvestSpeedMult:F2}x speed)\n" +
                      $"===============================================================");
        }

        [MenuItem("Cursed Harvest/Debug/Complete Current Wave")]
        public static void CompleteCurrentWave()
        {
            SpawnDirector sd = Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null)
            {
                sd.ForceClearCurrentWave();
                Debug.Log($"[CursedHarvestDebugMenu] Force cleared Wave {sd.CurrentWave}!");
            }
            else
            {
                Debug.LogWarning("[CursedHarvestDebugMenu] No active SpawnDirector found in scene.");
            }
        }

        [MenuItem("Cursed Harvest/Debug/Spawn The Mire Mother")]
        public static void SpawnMireMother()
        {
            SpawnDirector sd = Object.FindFirstObjectByType<SpawnDirector>();
            if (sd != null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheMireMother.prefab");
                if (prefab != null)
                {
                    Transform player = GameObject.FindWithTag("Player")?.transform;
                    Vector3 pos = player != null ? player.position + Vector3.forward * 14f : new Vector3(0, 0.5f, 14f);
                    GameObject boss = Object.Instantiate(prefab, pos, Quaternion.identity);
                    BossController bc = boss.GetComponent<BossController>();
                    if (bc != null) bc.Initialize();
                    Debug.Log("[CursedHarvestDebugMenu] Spawned The Mire Mother!");
                }
                else
                {
                    Debug.LogWarning("[CursedHarvestDebugMenu] Boss_TheMireMother.prefab not found at Assets/_Prefabs/Boss_TheMireMother.prefab");
                }
            }
            else
            {
                Debug.LogWarning("[CursedHarvestDebugMenu] No active SpawnDirector found in scene.");
            }
        }
    }
}
