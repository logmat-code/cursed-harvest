using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class Phase5And6TestRunner
    {
        [MenuItem("Cursed Harvest/Test/Run Phase 5 and 6 Tests")]
        public static void RunTests()
        {
            Debug.Log("==========================================");
            Debug.Log("[Phase 5 & 6 Test] Starting Content, Boss & Run Structure verification...");
            Debug.Log("==========================================");

            // 1. Rebuild Complete Game
            CoreSetup.BuildCompleteGame();

            // 2. Open Scene
            Scene scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: MainGame.unity is not valid!");
                return;
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: Player not found!");
                return;
            }

            PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            WeaponManager wm = playerObj.GetComponent<WeaponManager>();
            UpgradeManager um = playerObj.GetComponent<UpgradeManager>();
            LuckyLastSystem lucky = playerObj.GetComponent<LuckyLastSystem>();

            if (ph == null || pc == null || wm == null || um == null || lucky == null)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: Required player components missing!");
                return;
            }

            // ==================== TEST 1: WEAPONS SYSTEM ====================
            Debug.Log("[Phase 5 & 6 Test] Testing Weapons System...");
            if (wm.ActiveWeapons.Count < 4)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: Expected at least 4 active weapons in WeaponManager, found {wm.ActiveWeapons.Count}!");
                return;
            }

            BaseWeapon crossbow = wm.GetWeapon("crossbow");
            BaseWeapon bomb = wm.GetWeapon("pumpkin_bomb");
            BaseWeapon scythe = wm.GetWeapon("harvest_scythe");
            BaseWeapon aura = wm.GetWeapon("fire_aura");

            if (crossbow == null || bomb == null || scythe == null || aura == null)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: One or more weapons missing from WeaponManager!");
                return;
            }

            // Enable and upgrade scythe
            scythe.enabled = true;
            scythe.Upgrade();
            if (scythe.Level < 2)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: Scythe level expected >= 2, got {scythe.Level}");
                return;
            }

            // Enable and upgrade fire aura
            aura.enabled = true;
            aura.Upgrade();
            if (aura.Level < 2)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: Fire aura level expected >= 2, got {aura.Level}");
                return;
            }
            Debug.Log("[Phase 5 & 6 Test] All 4 Weapons verified.");

            // ==================== TEST 2: 18 UPGRADES DATABASE ====================
            Debug.Log("[Phase 5 & 6 Test] Testing Upgrades Database (18 total)...");
            int upgradeCount = 0;
            foreach (UpgradeType uType in Enum.GetValues(typeof(UpgradeType)))
            {
                UpgradeData data = um.GetUpgrade(uType);
                if (data != null)
                {
                    upgradeCount++;
                }
                else
                {
                    Debug.LogError($"[Phase 5 & 6 Test] FAILED: UpgradeType {uType} not found in database!");
                    return;
                }
            }

            if (upgradeCount < 18)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: Expected 18 upgrades, found {upgradeCount}!");
                return;
            }
            Debug.Log($"[Phase 5 & 6 Test] Upgrades verified: {upgradeCount} unique configurable upgrades available.");

            // ==================== TEST 3: ENEMY VARIANTS & PREFABS ====================
            Debug.Log("[Phase 5 & 6 Test] Testing Enemy Variants...");
            string[] enemyPrefabNames = new string[]
            {
                "Enemy_PumpkinGrunt",
                "Enemy_MaskedChaser",
                "Enemy_PlagueWalker",
                "Enemy_DemonCharger",
                "Enemy_CursedFarmer",
                "Enemy_EliteScarecrow",
                "Enemy_HarvestBrute"
            };

            foreach (var eName in enemyPrefabNames)
            {
                GameObject ePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Prefabs/{eName}.prefab");
                if (ePrefab == null)
                {
                    Debug.LogError($"[Phase 5 & 6 Test] FAILED: Prefab {eName} does not exist in Assets/_Prefabs!");
                    return;
                }

                EnemyController ec = ePrefab.GetComponent<EnemyController>();
                EnemyHealth eh = ePrefab.GetComponent<EnemyHealth>();
                if (ec == null || eh == null)
                {
                    Debug.LogError($"[Phase 5 & 6 Test] FAILED: {eName} missing EnemyController or EnemyHealth!");
                    return;
                }
            }
            Debug.Log("[Phase 5 & 6 Test] All 7 Enemy/Elite variants verified.");

            // Test Charger ability
            GameObject chargerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_DemonCharger.prefab");
            EnemyController chargerEc = chargerPrefab.GetComponent<EnemyController>();
            SerializedObject soCharger = new SerializedObject(chargerEc);
            if (!soCharger.FindProperty("canCharge").boolValue)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: DemonCharger should have canCharge=true!");
                return;
            }

            // Test Plague hazard
            GameObject plaguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_PlagueWalker.prefab");
            EnemyHealth plagueEh = plaguePrefab.GetComponent<EnemyHealth>();
            SerializedObject soPlague = new SerializedObject(plagueEh);
            if (soPlague.FindProperty("onDeathHazardPrefab").objectReferenceValue == null)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: PlagueWalker should have onDeathHazardPrefab assigned!");
                return;
            }
            Debug.Log("[Phase 5 & 6 Test] Enemy abilities (Charge, Plague Puddle) verified.");

            // ==================== TEST 4: BOSS & ENDLESS MODE ====================
            Debug.Log("[Phase 5 & 6 Test] Testing Boss & Endless Mode...");
            SpawnDirector sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>();
            if (sd == null)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: SpawnDirector not found in scene!");
                return;
            }
            sd.InitializeDirector();

            GameObject bossObj = sd.SpawnBoss();
            if (bossObj == null)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: SpawnDirector.SpawnBoss returned null!");
                return;
            }

            BossController boss = bossObj.GetComponent<BossController>();
            EnemyHealth bossHealth = bossObj.GetComponent<EnemyHealth>();
            if (boss == null || bossHealth == null)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: Boss missing BossController or EnemyHealth!");
                UnityEngine.Object.DestroyImmediate(bossObj);
                return;
            }
            boss.Initialize();

            if (bossHealth.MaxHealth < 1500f)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: Boss MaxHealth expected >= 1500, got {bossHealth.MaxHealth}!");
                UnityEngine.Object.DestroyImmediate(bossObj);
                return;
            }

            // Test Boss Defeat and Endless Mode Transition
            bool bossDefeatedEventFired = false;
            Action<BossController> bossHandler = (b) => { bossDefeatedEventFired = true; };
            BossController.OnBossDefeated += bossHandler;

            bossHealth.TakeDamage(new DamageData(3000f)); // Lethal hit
            BossController.OnBossDefeated -= bossHandler;

            if (!bossDefeatedEventFired)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: BossController.OnBossDefeated was not invoked!");
                UnityEngine.Object.DestroyImmediate(bossObj);
                return;
            }

            if (!sd.IsEndless)
            {
                Debug.LogError("[Phase 5 & 6 Test] FAILED: SpawnDirector did not enter Endless mode upon boss defeat!");
                UnityEngine.Object.DestroyImmediate(bossObj);
                return;
            }
            Debug.Log("[Phase 5 & 6 Test] Boss & Endless Mode Transition verified.");
            UnityEngine.Object.DestroyImmediate(bossObj);

            // ==================== TEST 5: RUN RESET ====================
            Debug.Log("[Phase 5 & 6 Test] Testing Run Reset...");
            sd.ResetDirector();
            ph.ResetHealth();
            lucky.ResetSystem();
            CursedSeedPickup.ResetSeeds();

            if (sd.RunTime != 0f || sd.IsEndless)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: SpawnDirector not properly reset! Time={sd.RunTime}, Endless={sd.IsEndless}");
                return;
            }
            if (ph.CurrentHealth != ph.MaxHealth || !ph.IsAlive)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: PlayerHealth not properly reset! HP={ph.CurrentHealth}/{ph.MaxHealth}");
                return;
            }
            if (lucky.CurrentCharges != 1 || lucky.IsActive)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: LuckyLastSystem not properly reset! Charges={lucky.CurrentCharges}, Active={lucky.IsActive}");
                return;
            }
            if (CursedSeedPickup.PlayerSeedCount != 0)
            {
                Debug.LogError($"[Phase 5 & 6 Test] FAILED: Seed inventory not properly reset! Seeds={CursedSeedPickup.PlayerSeedCount}");
                return;
            }
            Debug.Log("[Phase 5 & 6 Test] Run Reset cleanly verified.");

            Debug.Log("==========================================");
            Debug.Log("[Phase 5 & 6 Test] ALL TESTS PASSED SUCCESSFULLY! ★★★");
            Debug.Log("==========================================");
        }
    }
}
