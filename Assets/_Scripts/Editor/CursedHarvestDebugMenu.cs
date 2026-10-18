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

        [MenuItem("Cursed Harvest/Debug/Add 1,000 Harvest Essence")]
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

        [MenuItem("Cursed Harvest/Debug/Add 10,000 Harvest Essence")]
        public static void Add10000Essence()
        {
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.AddEssence(10000, false);
                Debug.Log($"[CursedHarvestDebugMenu] Added 10,000 Harvest Essence. Total: {MetaProgressionManager.Instance.HarvestEssence}");
            }
            else
            {
                int cur = PlayerPrefs.GetInt("CH_HarvestEssence", 0);
                PlayerPrefs.SetInt("CH_HarvestEssence", cur + 10000);
                PlayerPrefs.Save();
                Debug.Log($"[CursedHarvestDebugMenu] Added 10,000 Harvest Essence to PlayerPrefs. Total: {cur + 10000}");
            }

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.RefreshUpgradesUI();
            }
        }

        [MenuItem("Cursed Harvest/Debug/Reset All Meta Upgrades and Essence")]
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
