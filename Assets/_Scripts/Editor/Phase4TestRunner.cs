using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class Phase4TestRunner
    {
        [MenuItem("Cursed Harvest/Test/Run Phase 4 Tests")]
        public static void RunTests()
        {
            Debug.Log("==========================================");
            Debug.Log("[Phase 4 Test] Starting Cursed Harvest & Gamble verification...");
            Debug.Log("==========================================");

            // 1. Build Phase 4 Project
            CoreSetup.BuildPhase4();

            // 2. Open Scene
            Scene scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Phase 4 Test] FAILED: MainGame.unity is not valid!");
                return;
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[Phase 4 Test] FAILED: Player not found!");
                return;
            }

            PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            WeaponManager wm = playerObj.GetComponent<WeaponManager>();
            UpgradeManager um = playerObj.GetComponent<UpgradeManager>();
            LuckyLastSystem lucky = playerObj.GetComponent<LuckyLastSystem>();

            if (ph == null || pc == null || wm == null || um == null || lucky == null)
            {
                Debug.LogError("[Phase 4 Test] FAILED: Required player components missing!");
                return;
            }

            // Test 1: Seed Inventory & Consumption
            Debug.Log("[Phase 4 Test] Testing Seed Inventory...");
            CursedSeedPickup.ResetSeeds();
            if (CursedSeedPickup.PlayerSeedCount != 1)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Initial seed count should be 1, got {CursedSeedPickup.PlayerSeedCount}!");
                return;
            }

            CursedSeedPickup.AddSeeds(3);
            if (CursedSeedPickup.PlayerSeedCount != 4)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Seed count after adding 3 should be 4, got {CursedSeedPickup.PlayerSeedCount}!");
                return;
            }

            bool consumed = CursedSeedPickup.TryConsumeSeed();
            if (!consumed || CursedSeedPickup.PlayerSeedCount != 3)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: TryConsumeSeed failed or count incorrect: consumed={consumed}, count={CursedSeedPickup.PlayerSeedCount}!");
                return;
            }

            // Empty seeds
            CursedSeedPickup.TryConsumeSeed();
            CursedSeedPickup.TryConsumeSeed();
            CursedSeedPickup.TryConsumeSeed();
            if (CursedSeedPickup.PlayerSeedCount != 0 || CursedSeedPickup.TryConsumeSeed())
            {
                Debug.LogError("[Phase 4 Test] FAILED: TryConsumeSeed should return false when inventory is 0!");
                return;
            }
            Debug.Log("[Phase 4 Test] Seed Inventory logic verified.");

            // Test 2: Seed Pickup Collection
            Debug.Log("[Phase 4 Test] Testing Seed Pickup Collection...");
            GameObject seedObj = new GameObject("TestSeed");
            CursedSeedPickup seedPickup = seedObj.AddComponent<CursedSeedPickup>();
            int countBeforeCollect = CursedSeedPickup.PlayerSeedCount;
            seedPickup.Collect();
            if (CursedSeedPickup.PlayerSeedCount != countBeforeCollect + 1)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Seed count did not increment on Collect! Expected {countBeforeCollect + 1}, got {CursedSeedPickup.PlayerSeedCount}");
                UnityEngine.Object.DestroyImmediate(seedObj);
                return;
            }
            UnityEngine.Object.DestroyImmediate(seedObj);
            Debug.Log("[Phase 4 Test] Seed Collection verified.");

            // Test 3: Harvest Plots & Scene Layout
            Debug.Log("[Phase 4 Test] Testing Scene Harvest Plots & Seed Pool...");
            if (GameManager.Instance == null || GameManager.Instance.SeedPool == null)
            {
                Debug.LogError("[Phase 4 Test] FAILED: GameManager.Instance.SeedPool is null!");
                return;
            }

            HarvestPlot[] plots = UnityEngine.Object.FindObjectsByType<HarvestPlot>();
            if (plots == null || plots.Length < 4)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Expected at least 4 HarvestPlot instances in arena, found {(plots != null ? plots.Length : 0)}!");
                return;
            }
            Debug.Log($"[Phase 4 Test] Found {plots.Length} Harvest Plots in arena.");

            // Test 4: Plot Growth & Gamble Calculation
            HarvestPlot testPlot = plots[0];
            testPlot.Initialize(playerObj.transform);
            testPlot.ResetPlot();

            if (testPlot.State != PlotState.Ready)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Initial plot state should be Ready, got {testPlot.State}!");
                return;
            }

            // Normal HP base chance
            ph.ResetHealth();
            float baseChance = testPlot.CalculateLuckyChance();
            if (Mathf.Abs(baseChance - 0.70f) > 0.01f)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Base lucky chance should be ~0.70f, got {baseChance}!");
                return;
            }
            Debug.Log($"[Phase 4 Test] Base lucky chance: {baseChance:P0}");

            // Desperation luck bonus (HP < 25%)
            ph.TakeDamage(new DamageData(85f)); // 15 HP remaining = 15%
            float desperateChance = testPlot.CalculateLuckyChance();
            if (desperateChance <= baseChance)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Desperation bonus not applied when HP < 25%! Desperate chance: {desperateChance}, Base: {baseChance}");
                return;
            }
            Debug.Log($"[Phase 4 Test] Desperation lucky chance (HP 15%): {desperateChance:P0} (+{(desperateChance - baseChance):P0})");

            // Better Harvest Upgrade
            ph.ResetHealth();
            UpgradeData betterHarvest = um.GetUpgrade(UpgradeType.BetterHarvest);
            um.ApplyUpgrade(betterHarvest);
            float upgradedChance = testPlot.CalculateLuckyChance();
            if (upgradedChance <= baseChance)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Better Harvest upgrade did not boost lucky chance! Got {upgradedChance}, expected > {baseChance}");
                return;
            }
            Debug.Log($"[Phase 4 Test] Upgraded lucky chance: {upgradedChance:P0}");

            // Test 5: Gamble Resolution - Lucky
            Debug.Log("[Phase 4 Test] Testing Lucky Resolution...");
            testPlot.ResetPlot();
            testPlot.StartGrowing();
            if (testPlot.State != PlotState.Growing)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Plot state after StartGrowing should be Growing, got {testPlot.State}!");
                return;
            }

            bool luckyEventReceived = false;
            Action<HarvestResult> gambleHandler = (res) =>
            {
                if (res == HarvestResult.Lucky) luckyEventReceived = true;
            };
            HarvestPlot.OnHarvestGambleResolved += gambleHandler;

            HarvestResult luckyResult = testPlot.ResolveHarvest(0.05f); // Force roll well below lucky chance
            HarvestPlot.OnHarvestGambleResolved -= gambleHandler;

            if (luckyResult != HarvestResult.Lucky)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Forced roll 0.05 should yield Lucky result, got {luckyResult}!");
                return;
            }
            if (!luckyEventReceived)
            {
                Debug.LogError("[Phase 4 Test] FAILED: OnHarvestGambleResolved event was not received for Lucky result!");
                return;
            }
            if (testPlot.State != PlotState.Cooldown)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Plot state should be Cooldown after resolving, got {testPlot.State}!");
                return;
            }
            Debug.Log("[Phase 4 Test] Lucky Gamble Resolution verified.");

            // Test 6: Gamble Resolution - Cursed
            Debug.Log("[Phase 4 Test] Testing Cursed Resolution...");
            testPlot.ResetPlot();
            testPlot.StartGrowing();

            bool cursedEventReceived = false;
            Action<HarvestResult> cursedHandler = (res) =>
            {
                if (res == HarvestResult.Cursed) cursedEventReceived = true;
            };
            HarvestPlot.OnHarvestGambleResolved += cursedHandler;

            HarvestResult cursedResult = testPlot.ResolveHarvest(0.99f); // Force roll well above lucky chance
            HarvestPlot.OnHarvestGambleResolved -= cursedHandler;

            if (cursedResult != HarvestResult.Cursed)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Forced roll 0.99 should yield Cursed result, got {cursedResult}!");
                return;
            }
            if (!cursedEventReceived)
            {
                Debug.LogError("[Phase 4 Test] FAILED: OnHarvestGambleResolved event was not received for Cursed result!");
                return;
            }
            if (testPlot.State != PlotState.Cooldown)
            {
                Debug.LogError($"[Phase 4 Test] FAILED: Plot state should be Cooldown after resolving, got {testPlot.State}!");
                return;
            }
            Debug.Log("[Phase 4 Test] Cursed Gamble Resolution verified.");

            Debug.Log("==========================================");
            Debug.Log("[Phase 4 Test] ALL TESTS PASSED SUCCESSFULLY! ★★★");
            Debug.Log("==========================================");
        }
    }
}
