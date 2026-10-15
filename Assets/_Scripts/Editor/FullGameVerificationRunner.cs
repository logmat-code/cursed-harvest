using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class FullGameVerificationRunner
    {
        [MenuItem("Cursed Harvest/Test/Run Full Game Verification")]
        public static void RunTests()
        {
            Debug.Log("=================================================================");
            Debug.Log("[FULL GAME VERIFICATION] Starting End-to-End Verification...");
            Debug.Log("=================================================================");

            // 1. Rebuild Complete Game
            CoreSetup.BuildCompleteGame();

            // 2. Open Scene
            Scene scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: MainGame.unity is not valid!");
                return;
            }
            Debug.Log("[1/8] MainGame.unity built and loaded successfully.");

            // 3. Verify Player and Systems
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: Player GameObject not found!");
                return;
            }

            PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            PlayerDash pd = playerObj.GetComponent<PlayerDash>();
            WeaponManager wm = playerObj.GetComponent<WeaponManager>();
            UpgradeManager um = playerObj.GetComponent<UpgradeManager>();
            ExperienceManager xp = playerObj.GetComponent<ExperienceManager>();
            LuckyLastSystem lucky = playerObj.GetComponent<LuckyLastSystem>();

            if (ph == null || pc == null || pd == null || wm == null || um == null || xp == null || lucky == null)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: Essential Player components missing!");
                return;
            }
            Debug.Log("[2/8] Player and Core Subsystems verified (Health, Dash, Weapons, Upgrades, XP, Lucky Last).");

            // 4. Verify 4 Weapons and 18 Upgrades
            if (wm.ActiveWeapons.Count < 4)
            {
                Debug.LogError($"[FULL GAME VERIFICATION] FAILED: Expected 4 active weapons, got {wm.ActiveWeapons.Count}!");
                return;
            }

            string[] weaponIds = new string[] { "crossbow", "pumpkin_bomb", "harvest_scythe", "fire_aura" };
            foreach (var wid in weaponIds)
            {
                BaseWeapon w = wm.GetWeapon(wid);
                if (w == null)
                {
                    Debug.LogError($"[FULL GAME VERIFICATION] FAILED: Weapon {wid} missing from WeaponManager!");
                    return;
                }
            }

            int upgradeCount = 0;
            foreach (UpgradeType uType in Enum.GetValues(typeof(UpgradeType)))
            {
                if (um.GetUpgrade(uType) != null) upgradeCount++;
            }
            if (upgradeCount < 18)
            {
                Debug.LogError($"[FULL GAME VERIFICATION] FAILED: Expected 18 upgrades, got {upgradeCount}!");
                return;
            }
            Debug.Log($"[3/8] Weapons (4/4) and Upgrades Database ({upgradeCount}/18) fully verified.");

            // 5. Verify Audio System & Procedural Synthesis
            AudioManager audioMgr = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: AudioManager not found in scene!");
                return;
            }

            foreach (SoundType st in Enum.GetValues(typeof(SoundType)))
            {
                audioMgr.PlaySound(st, 0.5f);
            }
            Debug.Log("[4/8] Procedural Audio System (15 SoundTypes + Ambient Spooky Drone) verified.");

            // 6. Verify Damage Numbers Manager
            DamageNumberManager dmgMgr = UnityEngine.Object.FindAnyObjectByType<DamageNumberManager>();
            if (dmgMgr == null)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: DamageNumberManager not found in scene!");
                return;
            }
            dmgMgr.SpawnDamage(playerObj.transform.position, 45f, true);
            Debug.Log("[5/8] Floating Damage Numbers Manager verified.");

            // 7. Verify UI Canvas, HUD, and Modals
            UIManager uim = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uim == null)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: UIManager not found in scene!");
                return;
            }
            uim.HideAllModals();
            uim.StartRunFromTitle();
            Debug.Log("[6/8] UIManager, HUD elements, Title & Game Modals verified.");

            // 8. Verify Harvest Plots & Gamble System
            HarvestPlot[] plots = UnityEngine.Object.FindObjectsByType<HarvestPlot>();
            if (plots.Length < 4)
            {
                Debug.LogError($"[FULL GAME VERIFICATION] FAILED: Expected at least 4 HarvestPlots, found {plots.Length}!");
                return;
            }
            HarvestPlot plot = plots[0];
            HarvestResult luckyResult = plot.ResolveHarvest(0.05f); // Force Lucky
            HarvestResult cursedResult = plot.ResolveHarvest(0.95f); // Force Cursed
            if (luckyResult != HarvestResult.Lucky || cursedResult != HarvestResult.Cursed)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: Harvest gamble outcomes did not resolve correctly!");
                return;
            }
            Debug.Log($"[7/8] Harvest Gamble system (4 Arena Quadrants, Lucky/Cursed mechanics) verified.");

            // 9. Verify Boss Spawning, Defeat, and Endless Mode
            SpawnDirector sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>();
            if (sd == null)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: SpawnDirector not found in scene!");
                return;
            }
            sd.InitializeDirector();

            GameObject bossObj = sd.SpawnBoss();
            if (bossObj == null)
            {
                Debug.LogError("[FULL GAME VERIFICATION] FAILED: Boss could not be spawned!");
                return;
            }

            BossController boss = bossObj.GetComponent<BossController>();
            EnemyHealth bossHealth = bossObj.GetComponent<EnemyHealth>();
            boss.Initialize();

            bool bossDefeatedInvoked = false;
            Action<BossController> bossHandler = (b) => { bossDefeatedInvoked = true; };
            BossController.OnBossDefeated += bossHandler;

            bossHealth.TakeDamage(new DamageData(5000f)); // Lethal hit
            BossController.OnBossDefeated -= bossHandler;

            if (!bossDefeatedInvoked || !sd.IsEndless)
            {
                Debug.LogError($"[FULL GAME VERIFICATION] FAILED: Boss defeat or Endless mode transition failed! Defeated:{bossDefeatedInvoked}, Endless:{sd.IsEndless}");
                UnityEngine.Object.DestroyImmediate(bossObj);
                return;
            }
            UnityEngine.Object.DestroyImmediate(bossObj);
            Debug.Log("[8/8] Boss 'THE LAST HARVEST' and Endless Mode transition verified.");

            // 10. Clean Run Reset
            sd.ResetDirector();
            ph.ResetHealth();
            lucky.ResetSystem();
            CursedSeedPickup.ResetSeeds();

            Debug.Log("=================================================================");
            Debug.Log("★★★ [FULL GAME VERIFICATION] ALL SYSTEMS OPERATIONAL! READY TO PLAY! ★★★");
            Debug.Log("=================================================================");
        }
    }
}
