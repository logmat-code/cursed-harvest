using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class Phase2TestRunner
    {
        [MenuItem("Cursed Harvest/Test/Run Phase 2 Tests")]
        public static void RunTests()
        {
            Debug.Log("==========================================");
            Debug.Log("[Phase 2 Test] Starting automated verification...");
            Debug.Log("==========================================");

            // 1. Build Phase 2 Core
            CoreSetup.BuildPhase2();

            // 2. Open Scene
            Scene scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Phase 2 Test] FAILED: MainGame.unity is not valid!");
                return;
            }
            Debug.Log("[Phase 2 Test] Scene loaded successfully.");

            // 3. Verify Player Components
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[Phase 2 Test] FAILED: Player not found!");
                return;
            }

            ExperienceManager xp = playerObj.GetComponent<ExperienceManager>();
            UpgradeManager um = playerObj.GetComponent<UpgradeManager>();
            LuckyLastSystem lucky = playerObj.GetComponent<LuckyLastSystem>();
            WeaponManager wm = playerObj.GetComponent<WeaponManager>();
            PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
            PlayerController pc = playerObj.GetComponent<PlayerController>();

            if (xp == null || um == null || lucky == null || wm == null || ph == null || pc == null)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Missing player component! XP:{xp!=null}, UM:{um!=null}, Lucky:{lucky!=null}");
                return;
            }
            Debug.Log("[Phase 2 Test] Player progression components verified: ExperienceManager, UpgradeManager, LuckyLastSystem.");

            // 4. Verify XP Pool and Enemy XP Drop on Death
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null || gm.XpPool == null || gm.EnemyPool == null)
            {
                Debug.LogError("[Phase 2 Test] FAILED: GameManager or XpPool missing!");
                return;
            }

            // Spawn enemy and kill it
            GameObject enemyObj = gm.EnemyPool.Get(new Vector3(4f, 0.5f, 4f), Quaternion.identity);
            EnemyHealth eh = enemyObj.GetComponent<EnemyHealth>();
            eh.TakeDamage(new DamageData(100f, playerObj)); // Lethal hit

            if (gm.XpPool.ActiveCount == 0)
            {
                Debug.LogError("[Phase 2 Test] FAILED: Enemy death did not spawn an XP pickup from XpPool!");
                return;
            }
            Debug.Log($"[Phase 2 Test] Enemy death dropped XP pickup! Active pickups: {gm.XpPool.ActiveCount}");

            // 5. Test Experience Progression and Level Up
            int startLevel = xp.CurrentLevel;
            float reqXP = xp.RequiredXP;
            bool levelUpFired = false;
            xp.OnLevelUp += (lvl) => { levelUpFired = true; };

            xp.AddXP(reqXP + 1f);

            if (!levelUpFired || xp.CurrentLevel <= startLevel)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Level Up did not fire! Level:{xp.CurrentLevel}, Fired:{levelUpFired}");
                return;
            }
            Debug.Log($"[Phase 2 Test] Level Up verified! Level {startLevel} -> Level {xp.CurrentLevel}. Next Required XP: {xp.RequiredXP}");

            // 6. Test Upgrade Choices and Stacking
            List<UpgradeData> choices = um.GetRandomUpgradeChoices(3);
            if (choices.Count != 3)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Expected 3 upgrade choices, got {choices.Count}!");
                return;
            }
            Debug.Log($"[Phase 2 Test] Upgrade choices generated: [{choices[0].title}], [{choices[1].title}], [{choices[2].title}]");

            // Apply Sharp Bolts (+25% Damage)
            float baseDmg = wm.GlobalDamageMultiplier;
            UpgradeData sharpBolts = um.GetUpgrade(UpgradeType.SharpBolts);
            um.ApplyUpgrade(sharpBolts);
            if (wm.GlobalDamageMultiplier <= baseDmg)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Sharp Bolts did not increase GlobalDamageMultiplier! Was:{baseDmg}, Now:{wm.GlobalDamageMultiplier}");
                return;
            }
            Debug.Log($"[Phase 2 Test] Sharp Bolts applied! GlobalDamageMultiplier: {baseDmg} -> {wm.GlobalDamageMultiplier}");

            // Apply Double Shot (+1 Multishot)
            int baseProj = wm.GlobalBonusProjectiles;
            UpgradeData doubleShot = um.GetUpgrade(UpgradeType.DoubleShot);
            um.ApplyUpgrade(doubleShot);
            if (wm.GlobalBonusProjectiles <= baseProj)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Double Shot did not increase BonusProjectiles! Was:{baseProj}, Now:{wm.GlobalBonusProjectiles}");
                return;
            }
            Debug.Log($"[Phase 2 Test] Double Shot applied! BonusProjectiles: {baseProj} -> {wm.GlobalBonusProjectiles}");

            // Apply Tough Skin (+25 Max HP)
            float baseMaxHp = ph.MaxHealth;
            UpgradeData toughSkin = um.GetUpgrade(UpgradeType.ToughSkin);
            um.ApplyUpgrade(toughSkin);
            if (ph.MaxHealth <= baseMaxHp)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Tough Skin did not increase MaxHealth! Was:{baseMaxHp}, Now:{ph.MaxHealth}");
                return;
            }
            Debug.Log($"[Phase 2 Test] Tough Skin applied! MaxHealth: {baseMaxHp} -> {ph.MaxHealth}");

            // Apply Fleet Footed (+15% speed)
            float baseSpeed = pc.MoveSpeed;
            UpgradeData fleet = um.GetUpgrade(UpgradeType.FleetFooted);
            um.ApplyUpgrade(fleet);
            if (pc.MoveSpeed <= baseSpeed)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Fleet Footed did not increase MoveSpeed! Was:{baseSpeed}, Now:{pc.MoveSpeed}");
                return;
            }
            Debug.Log($"[Phase 2 Test] Fleet Footed applied! MoveSpeed: {baseSpeed} -> {pc.MoveSpeed}");

            // Test Reroll
            int startRerolls = um.RerollCharges;
            bool rerollSuccess = um.TryReroll();
            if (!rerollSuccess || um.RerollCharges != startRerolls - 1)
            {
                Debug.LogError($"[Phase 2 Test] FAILED: Reroll did not decrement charges properly! Charges:{um.RerollCharges}");
                return;
            }
            Debug.Log($"[Phase 2 Test] Reroll mechanic verified! Charges remaining: {um.RerollCharges}");

            // 7. Verify SpawnDirector
            SpawnDirector sd = Object.FindFirstObjectByType<SpawnDirector>();
            if (sd == null)
            {
                Debug.LogError("[Phase 2 Test] FAILED: SpawnDirector not found!");
                return;
            }
            Debug.Log($"[Phase 2 Test] SpawnDirector verified! RunTime:{sd.RunTime:F1}s, Endless:{sd.IsEndless}");

            // 8. Verify UIManager
            UIManager uim = Object.FindFirstObjectByType<UIManager>();
            if (uim == null)
            {
                Debug.LogError("[Phase 2 Test] FAILED: UIManager not found!");
                return;
            }
            Debug.Log("[Phase 2 Test] UIManager verified!");

            // Clean up scene pools
            gm.EnemyPool.ReturnAllActive();
            gm.XpPool.ReturnAllActive();
            gm.BoltPool.ReturnAllActive();

            Debug.Log("==========================================");
            Debug.Log("[Phase 2 Test] ALL PHASE 2 SURVIVOR LOOP TESTS PASSED!");
            Debug.Log("==========================================");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
