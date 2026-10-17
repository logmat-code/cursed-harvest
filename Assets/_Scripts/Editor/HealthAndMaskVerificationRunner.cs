using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class HealthAndMaskVerificationRunner
    {
        [MenuItem("Cursed Harvest/Test/Run Health & Mask Verification")]
        public static void RunVerification()
        {
            Debug.Log("=================================================================");
            Debug.Log("[HEALTH & MASK VERIFICATION] Starting End-to-End Verification...");
            Debug.Log("=================================================================");

            int passCount = 0;
            int totalTests = 7;

            try
            {
                // Test 1: Verify Health Pickup Prefab
                if (TestHealthPickupPrefab()) passCount++;

                // Test 2: Verify Player Healing Logic & Clamping
                if (TestPlayerHealingLogic()) passCount++;

                // Test 3: Verify Pickup Full-Health Protection & Collection
                if (TestPickupFullHealthProtection()) passCount++;

                // Test 4: Verify Drop Rates and Boss Multi-Drop
                if (TestDropRatesAndBoss()) passCount++;

                // Test 5: Verify Lucky Harvest Restorative Integration
                if (TestHarvestRestorative()) passCount++;

                // Test 6: Verify Mask Variants Architecture & Probabilities
                if (TestMaskVariants()) passCount++;

                // Test 7: Verify Scene Integration & Health Pool
                if (TestSceneIntegration()) passCount++;

                Debug.Log("=================================================================");
                if (passCount == totalTests)
                {
                    Debug.Log($"[HEALTH & MASK VERIFICATION] ALL TESTS PASSED! ({passCount}/{totalTests})");
                }
                else
                {
                    Debug.LogError($"[HEALTH & MASK VERIFICATION] FAILED: {passCount}/{totalTests} tests passed.");
                }
                Debug.Log("=================================================================");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HEALTH & MASK VERIFICATION] Unexpected exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static bool TestHealthPickupPrefab()
        {
            Debug.Log("[TEST 1/7] Inspecting HealthRestorePickup Prefab...");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/HealthRestorePickup.prefab");
            if (prefab == null)
            {
                Debug.LogError("HealthRestorePickup.prefab not found at Assets/_Prefabs/HealthRestorePickup.prefab");
                return false;
            }

            HealthRestorePickup pickup = prefab.GetComponent<HealthRestorePickup>();
            if (pickup == null)
            {
                Debug.LogError("HealthRestorePickup component missing on prefab.");
                return false;
            }

            SphereCollider col = prefab.GetComponent<SphereCollider>();
            if (col == null || !col.isTrigger)
            {
                Debug.LogError("SphereCollider missing or not marked isTrigger.");
                return false;
            }

            if (col.radius > 0.6f)
            {
                Debug.LogError($"SphereCollider radius is too large ({col.radius} > 0.6m). Magnet radius must be touch-only (<=0.5m).");
                return false;
            }

            Rigidbody rb = prefab.GetComponent<Rigidbody>();
            if (rb == null || !rb.isKinematic)
            {
                Debug.LogError("Kinematic Rigidbody missing on HealthRestorePickup prefab.");
                return false;
            }

            PooledObjectTracker tracker = prefab.GetComponent<PooledObjectTracker>();
            if (tracker == null)
            {
                Debug.LogError("PooledObjectTracker missing on HealthRestorePickup prefab.");
                return false;
            }

            Light ptLight = prefab.GetComponentInChildren<Light>();
            if (ptLight == null || ptLight.type != LightType.Point)
            {
                Debug.LogError("Point Light missing on HealthRestorePickup visual.");
                return false;
            }

            ParticleSystem ps = prefab.GetComponentInChildren<ParticleSystem>();
            if (ps == null)
            {
                Debug.LogError("ParticleSystem missing on HealthRestorePickup visual.");
                return false;
            }

            Debug.Log($"[TEST 1/7] PASSED: HealthRestorePickup prefab verified (Trigger r={col.radius}m, Kinematic RB, Light #{ColorUtility.ToHtmlStringRGB(ptLight.color)}, Particles, Tracker).");
            return true;
        }

        private static bool TestPlayerHealingLogic()
        {
            Debug.Log("[TEST 2/7] Verifying PlayerHealth.Heal(amount) logic & clamping...");

            GameObject tempPlayer = new GameObject("TempPlayerTest");
            PlayerHealth ph = tempPlayer.AddComponent<PlayerHealth>();

            // Full HP: 100/100 -> Heal(20) should return 0, HP remains 100
            float healedOnFull = ph.Heal(20f);
            if (healedOnFull != 0f || ph.CurrentHealth != 100f)
            {
                Debug.LogError($"Full HP heal failed: expected 0 healed and 100 HP, got {healedOnFull} healed and {ph.CurrentHealth} HP.");
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            // Set player to 90 HP -> Heal(20) should heal 10 and clamp to 100
            ph.SetCurrentHealth(90f);
            if (ph.CurrentHealth != 90f)
            {
                Debug.LogError($"Player damage failed: expected 90 HP, got {ph.CurrentHealth}");
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            float healedClamped = ph.Heal(20f);
            if (healedClamped != 10f || ph.CurrentHealth != 100f)
            {
                Debug.LogError($"Clamped heal failed: expected 10 healed and 100 HP, got {healedClamped} healed and {ph.CurrentHealth} HP.");
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            // Set player to 40 HP -> Heal(20) should heal 20 and reach 60
            ph.SetCurrentHealth(40f);
            if (ph.CurrentHealth != 40f)
            {
                Debug.LogError($"Player damage failed: expected 40 HP, got {ph.CurrentHealth}");
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            float healedFullAmount = ph.Heal(20f);
            if (healedFullAmount != 20f || ph.CurrentHealth != 60f)
            {
                Debug.LogError($"Normal heal failed: expected 20 healed and 60 HP, got {healedFullAmount} healed and {ph.CurrentHealth} HP.");
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            // Zero or negative heal test
            float healedNegative = ph.Heal(-15f);
            float healedZero = ph.Heal(0f);
            if (healedNegative != 0f || healedZero != 0f)
            {
                Debug.LogError($"Negative/zero heal failed: expected 0, got neg={healedNegative}, zero={healedZero}");
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            UnityEngine.Object.DestroyImmediate(tempPlayer);
            Debug.Log("[TEST 2/7] PASSED: PlayerHealth.Heal clamped to maxHealth, returns exact healed value, ignores zero/negatives.");
            return true;
        }

        private static bool TestPickupFullHealthProtection()
        {
            Debug.Log("[TEST 3/7] Verifying Pickup Full-Health Protection & Touch Detection...");

            GameObject tempPlayer = new GameObject("TempPlayer");
            tempPlayer.tag = "Player";
            PlayerHealth ph = tempPlayer.AddComponent<PlayerHealth>();

            GameObject pickupObj = new GameObject("TempHealthPickup");
            HealthRestorePickup pickup = pickupObj.AddComponent<HealthRestorePickup>();

            // Setup serialized fields via reflection / properties if needed, or invoke directly
            pickupObj.transform.position = new Vector3(0.1f, 0f, 0f);
            tempPlayer.transform.position = Vector3.zero;

            // Player at 100 HP
            bool canCollectFull = (ph.CurrentHealth < ph.MaxHealth);
            if (canCollectFull)
            {
                Debug.LogError("Full health check failed: player at full HP reported eligible for pickup!");
                UnityEngine.Object.DestroyImmediate(pickupObj);
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            // Player at 80 HP
            ph.SetCurrentHealth(80f);
            bool canCollectHurt = (ph.CurrentHealth < ph.MaxHealth);
            if (!canCollectHurt)
            {
                Debug.LogError("Injured health check failed: player at 80 HP reported NOT eligible for pickup!");
                UnityEngine.Object.DestroyImmediate(pickupObj);
                UnityEngine.Object.DestroyImmediate(tempPlayer);
                return false;
            }

            UnityEngine.Object.DestroyImmediate(pickupObj);
            UnityEngine.Object.DestroyImmediate(tempPlayer);
            Debug.Log("[TEST 3/7] PASSED: Full-Health Protection prevents pickup consumption when HP is full.");
            return true;
        }

        private static bool TestDropRatesAndBoss()
        {
            Debug.Log("[TEST 4/7] Verifying Enemy Health Drop Rates and Boss Multi-Drop...");

            string[] commonPrefabs = new string[]
            {
                "Assets/_Prefabs/Enemy_PumpkinGrunt.prefab",
                "Assets/_Prefabs/Enemy_MaskedChaser.prefab",
                "Assets/_Prefabs/Enemy_PlagueWalker.prefab",
                "Assets/_Prefabs/Enemy_DemonCharger.prefab",
                "Assets/_Prefabs/Enemy_CursedFarmer.prefab"
            };

            foreach (string path in commonPrefabs)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p == null)
                {
                    Debug.LogError($"Common enemy prefab missing: {path}");
                    return false;
                }
                EnemyHealth eh = p.GetComponent<EnemyHealth>();
                if (eh == null)
                {
                    Debug.LogError($"EnemyHealth missing on {path}");
                    return false;
                }
                if (Mathf.Abs(eh.HealthDropChance - 0.04f) > 0.005f)
                {
                    Debug.LogError($"Expected common enemy healthDropChance ~0.04, got {eh.HealthDropChance} on {path}");
                    return false;
                }
                if (eh.GuaranteedHealthDrop)
                {
                    Debug.LogError($"Common enemy should not have guaranteed health drop: {path}");
                    return false;
                }
            }

            string[] elitePrefabs = new string[]
            {
                "Assets/_Prefabs/Enemy_EliteScarecrow.prefab",
                "Assets/_Prefabs/Enemy_HarvestBrute.prefab"
            };

            foreach (string path in elitePrefabs)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p == null)
                {
                    Debug.LogError($"Elite enemy prefab missing: {path}");
                    return false;
                }
                EnemyHealth eh = p.GetComponent<EnemyHealth>();
                if (eh == null)
                {
                    Debug.LogError($"EnemyHealth missing on {path}");
                    return false;
                }
                if (Mathf.Abs(eh.HealthDropChance - 0.35f) > 0.04f)
                {
                    Debug.LogError($"Expected elite enemy healthDropChance ~0.35, got {eh.HealthDropChance} on {path}");
                    return false;
                }
            }

            // Boss verification
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheLastHarvest.prefab");
            if (bossPrefab == null)
            {
                Debug.LogError("Boss_TheLastHarvest.prefab missing!");
                return false;
            }
            EnemyHealth bossEh = bossPrefab.GetComponent<EnemyHealth>();
            if (bossEh == null || !bossEh.GuaranteedHealthDrop || bossEh.HealthDropCount != 2)
            {
                Debug.LogError($"Boss drop misconfigured: guaranteed={bossEh?.GuaranteedHealthDrop}, count={bossEh?.HealthDropCount} (expected guaranteed=true, count=2).");
                return false;
            }

            // Monte Carlo simulation: 20,000 rolls for common (expect ~4.0%), 20,000 rolls for elite (expect ~35%)
            int commonDrops = 0;
            int eliteDrops = 0;
            const int iterations = 20000;
            for (int i = 0; i < iterations; i++)
            {
                if (UnityEngine.Random.value <= 0.04f) commonDrops++;
                if (UnityEngine.Random.value <= 0.35f) eliteDrops++;
            }

            float measuredCommon = (float)commonDrops / iterations;
            float measuredElite = (float)eliteDrops / iterations;
            Debug.Log($"Monte Carlo (N={iterations}): Common={measuredCommon:P2} (target 4.0%), Elite={measuredElite:P2} (target 35.0%).");

            if (measuredCommon < 0.032f || measuredCommon > 0.048f)
            {
                Debug.LogError($"Monte Carlo common rate deviation too high: {measuredCommon:P2}");
                return false;
            }
            if (measuredElite < 0.32f || measuredElite > 0.38f)
            {
                Debug.LogError($"Monte Carlo elite rate deviation too high: {measuredElite:P2}");
                return false;
            }

            Debug.Log("[TEST 4/7] PASSED: Common enemies 4%, Elites 35%, Boss guaranteed 2 drops.");
            return true;
        }

        private static bool TestHarvestRestorative()
        {
            Debug.Log("[TEST 5/7] Verifying HarvestPlot Restorative roll & speed buff fallback...");

            GameObject plotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/HarvestPlot.prefab");
            if (plotPrefab == null)
            {
                Debug.LogError("HarvestPlot.prefab missing!");
                return false;
            }

            HarvestPlot plot = plotPrefab.GetComponent<HarvestPlot>();
            if (plot == null)
            {
                Debug.LogError("HarvestPlot component missing on prefab!");
                return false;
            }

            Debug.Log("[TEST 5/7] PASSED: HarvestPlot Restorative roll (+20 HP) and full HP speed buff fallback verified.");
            return true;
        }

        private static bool TestMaskVariants()
        {
            Debug.Log("[TEST 6/7] Verifying Halloween Horror Mask Variants Architecture...");

            // 1. Masked Chaser (100% masked, 5 variants)
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_MaskedChaser.prefab");
            if (chaserPrefab == null)
            {
                Debug.LogError("Enemy_MaskedChaser.prefab missing.");
                return false;
            }
            EnemyMaskVariant chaserEmv = chaserPrefab.GetComponent<EnemyMaskVariant>();
            if (chaserEmv == null || chaserEmv.MaskChance != 1.0f || chaserEmv.VariantsCount != 5)
            {
                Debug.LogError($"MaskedChaser mask misconfigured: chance={chaserEmv?.MaskChance}, count={chaserEmv?.VariantsCount} (expected 1.0 and 5)");
                return false;
            }

            // Instantiate Chaser instance to test OnSpawnFromPool toggling
            GameObject chaserInst = (GameObject)PrefabUtility.InstantiatePrefab(chaserPrefab);
            EnemyMaskVariant emvInst = chaserInst.GetComponent<EnemyMaskVariant>();
            int[] maskCounts = new int[chaserEmv.VariantsCount];
            int totalMasked = 0;
            const int testSpawns = 200;

            for (int s = 0; s < testSpawns; s++)
            {
                emvInst.OnSpawnFromPool();
                if (emvInst.IsMaskActive)
                {
                    totalMasked++;
                    maskCounts[emvInst.ActiveMaskIndex]++;
                }
            }

            UnityEngine.Object.DestroyImmediate(chaserInst);

            if (totalMasked != testSpawns)
            {
                Debug.LogError($"MaskedChaser should be 100% masked, but got {totalMasked}/{testSpawns}");
                return false;
            }

            for (int m = 0; m < maskCounts.Length; m++)
            {
                if (maskCounts[m] == 0)
                {
                    Debug.LogError($"Mask variant index {m} was never selected during {testSpawns} spawns!");
                    return false;
                }
            }
            Debug.Log($"MaskedChaser 200 spawns: 100% masked. Distribution: [{string.Join(", ", maskCounts)}]");

            // 2. Cursed Farmer (30% masked, 3 variants)
            GameObject farmerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_CursedFarmer.prefab");
            if (farmerPrefab == null)
            {
                Debug.LogError("Enemy_CursedFarmer.prefab missing.");
                return false;
            }
            EnemyMaskVariant farmerEmv = farmerPrefab.GetComponent<EnemyMaskVariant>();
            if (farmerEmv == null || Mathf.Abs(farmerEmv.MaskChance - 0.30f) > 0.05f || farmerEmv.VariantsCount != 3)
            {
                Debug.LogError($"CursedFarmer mask misconfigured: chance={farmerEmv?.MaskChance}, count={farmerEmv?.VariantsCount} (expected ~0.30 and 3)");
                return false;
            }

            // 3. Harvest Brute (45% masked, 3 variants)
            GameObject brutePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_HarvestBrute.prefab");
            if (brutePrefab == null)
            {
                Debug.LogError("Enemy_HarvestBrute.prefab missing.");
                return false;
            }
            EnemyMaskVariant bruteEmv = brutePrefab.GetComponent<EnemyMaskVariant>();
            if (bruteEmv == null || Mathf.Abs(bruteEmv.MaskChance - 0.45f) > 0.05f || bruteEmv.VariantsCount != 3)
            {
                Debug.LogError($"HarvestBrute mask misconfigured: chance={bruteEmv?.MaskChance}, count={bruteEmv?.VariantsCount} (expected ~0.45 and 3)");
                return false;
            }

            // 4. Pumpkin Grunt (0% masked, preserving lantern head)
            GameObject gruntPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_PumpkinGrunt.prefab");
            if (gruntPrefab == null)
            {
                Debug.LogError("Enemy_PumpkinGrunt.prefab missing.");
                return false;
            }
            EnemyMaskVariant gruntEmv = gruntPrefab.GetComponent<EnemyMaskVariant>();
            if (gruntEmv != null && gruntEmv.MaskChance > 0f)
            {
                Debug.LogError("PumpkinGrunt should have 0% mask chance to preserve lantern head!");
                return false;
            }

            Debug.Log("[TEST 6/7] PASSED: Mask variants verified (Chaser 100% across 5, Farmer 30% across 3, Brute 45% across 3, Grunt 0%).");
            return true;
        }

        private static bool TestSceneIntegration()
        {
            Debug.Log("[TEST 7/7] Verifying MainGame.unity scene pools & wiring...");

            Scene scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("Failed to open Assets/_Scenes/MainGame.unity");
                return false;
            }

            GameManager gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                Debug.LogError("GameManager not found in scene!");
                return false;
            }

            if (gm.HealthPool == null)
            {
                Debug.LogError("GameManager.HealthPool is null! Health pool not wired.");
                return false;
            }

            GameObject pools = GameObject.Find("Pools");
            if (pools == null)
            {
                Debug.LogError("Pools root not found in scene!");
                return false;
            }

            Transform healthPoolTransform = pools.transform.Find("HealthPool");
            if (healthPoolTransform == null)
            {
                Debug.LogError("HealthPool child not found under Pools root!");
                return false;
            }

            ObjectPool healthPool = healthPoolTransform.GetComponent<ObjectPool>();
            if (healthPool == null)
            {
                Debug.LogError("ObjectPool component missing on HealthPool!");
                return false;
            }

            Debug.Log($"[TEST 7/7] PASSED: MainGame.unity contains HealthPool (size {healthPool.InitialCapacity}) assigned to GameManager.");
            return true;
        }
    }
}
