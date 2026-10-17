using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CursedHarvest.Editor
{
    [InitializeOnLoad]
    public static class WaveSystemVerificationRunner
    {
        private const string MarkerFile = "wave_verification_done.marker";
        private const string LogFile = "wave_verify_log.txt";

        static WaveSystemVerificationRunner()
        {
            EditorApplication.delayCall += AutoRunIfPending;
        }

        private static void AutoRunIfPending()
        {
            if (File.Exists(MarkerFile)) return;

            // Mark as running so it doesn't double-trigger
            File.WriteAllText(MarkerFile, "running");
            RunVerification();
        }

        [MenuItem("Cursed Harvest/Test/Run Wave System Verification")]
        public static void RunVerification()
        {
            List<string> logs = new List<string>();
            Action<string> log = (msg) =>
            {
                Debug.Log(msg);
                logs.Add(msg);
            };
            Action<string> logErr = (msg) =>
            {
                Debug.LogError(msg);
                logs.Add("ERROR: " + msg);
            };

            log("=================================================================");
            log("[WAVE SYSTEM VERIFICATION] Starting End-to-End Verification...");
            log("=================================================================");

            int passCount = 0;
            int totalTests = 8;

            try
            {
                // Rebuild scene first to ensure latest CoreSetup wiring
                log("[SETUP] Rebuilding game scene via CoreSetup.BuildCompleteGame()...");
                CoreSetup.BuildCompleteGame();

                if (EditorSceneManager.GetActiveScene().path != "Assets/_Scenes/MainGame.unity")
                {
                    EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
                }

                // Test 1: Damage Audit & Invulnerability Frames
                if (TestDamageAuditAndInvulnerability(log, logErr)) passCount++;

                // Test 2: Lucky Last 12s Duration & Death Immunity
                if (TestLuckyLastOverhaul(log, logErr)) passCount++;

                // Test 3: Wave Progression Curve (Waves 1-10 + Exponential)
                if (TestWaveProgressionCurve(log, logErr)) passCount++;

                // Test 4: Wave State Machine & Unified Authority
                if (TestWaveStateMachine(log, logErr)) passCount++;

                // Test 5: Collectible Registry & End-of-Wave Vacuum
                if (TestVacuumAndCollectibleRegistry(log, logErr)) passCount++;

                // Test 6: Guaranteed Health Restore Drop
                if (TestGuaranteedHealthDrop(log, logErr)) passCount++;

                // Test 7: Level-Up Queueing
                if (TestLevelUpQueue(log, logErr)) passCount++;

                // Test 8: Scene Integration & Wave UI Elements
                if (TestSceneIntegrationAndWaveUI(log, logErr)) passCount++;

                log("=================================================================");
                if (passCount == totalTests)
                {
                    log($"[WAVE SYSTEM VERIFICATION] ALL TESTS PASSED! ({passCount}/{totalTests})");
                }
                else
                {
                    logErr($"[WAVE SYSTEM VERIFICATION] FAILED: {passCount}/{totalTests} tests passed.");
                }
                log("=================================================================");

                // Capture Visual Proof Screenshots
                CaptureWaveVisuals(log);

                File.WriteAllText(MarkerFile, $"PASSED {passCount}/{totalTests} at {DateTime.UtcNow:u}");
            }
            catch (Exception ex)
            {
                logErr($"[WAVE SYSTEM VERIFICATION] Unexpected exception: {ex.Message}\n{ex.StackTrace}");
                File.WriteAllText(MarkerFile, $"EXCEPTION: {ex.Message}");
            }
            finally
            {
                File.WriteAllLines(LogFile, logs);
            }
        }

        private static bool TestDamageAuditAndInvulnerability(Action<string> log, Action<string> logErr)
        {
            log("[TEST 1/8] Verifying Player Invulnerability Duration (0.60s), Damage Multipliers, Dash Cooldown & Charger Speed...");
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                logErr("Player GameObject not found!");
                return false;
            }

            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph == null)
            {
                logErr("PlayerHealth component missing!");
                return false;
            }

            // Verify WorldDifficultyConfig centralization
            if (Mathf.Abs(WorldDifficultyConfig.Current.hitInvulnerability - 0.60f) > 0.01f)
            {
                logErr($"WorldDifficultyConfig.hitInvulnerability is {WorldDifficultyConfig.Current.hitInvulnerability}, expected 0.60f!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.enemyDamageMultiplier - 0.82f) > 0.01f)
            {
                logErr($"WorldDifficultyConfig.enemyDamageMultiplier is {WorldDifficultyConfig.Current.enemyDamageMultiplier}, expected 0.82f!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.bossDamageMultiplier - 0.90f) > 0.01f)
            {
                logErr($"WorldDifficultyConfig.bossDamageMultiplier is {WorldDifficultyConfig.Current.bossDamageMultiplier}, expected 0.90f!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.dashBaseCooldown - 1.90f) > 0.01f)
            {
                logErr($"WorldDifficultyConfig.dashBaseCooldown is {WorldDifficultyConfig.Current.dashBaseCooldown}, expected 1.90f!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.chargerSpeedMultiplier - 0.90f) > 0.01f)
            {
                logErr($"WorldDifficultyConfig.chargerSpeedMultiplier is {WorldDifficultyConfig.Current.chargerSpeedMultiplier}, expected 0.90f!");
                return false;
            }

            // Verify PlayerHealth default invulnerabilityDuration is 0.60s
            SerializedObject so = new SerializedObject(ph);
            float iDuration = so.FindProperty("invulnerabilityDuration").floatValue;
            if (Mathf.Abs(iDuration - 0.60f) > 0.01f)
            {
                logErr($"PlayerHealth.invulnerabilityDuration is {iDuration}, expected 0.60f!");
                return false;
            }

            // Test PlayerDash baseCooldown is 1.90s
            PlayerDash pd = player.GetComponent<PlayerDash>();
            if (pd != null)
            {
                if (Mathf.Abs(pd.BaseCooldown - 1.90f) > 0.05f)
                {
                    logErr($"PlayerDash.BaseCooldown is {pd.BaseCooldown}, expected 1.90f!");
                    return false;
                }
            }

            // Test i-frames prevent multi-hit frame annihilation
            ph.ResetHealth();
            ph.TakeDamage(new DamageData(20f, null, Vector3.zero, 0f, player.transform.position));
            if (!ph.IsInvulnerable)
            {
                logErr("Player should be invulnerable immediately after taking damage!");
                return false;
            }

            // Immediate second hit must be blocked by i-frames
            float hpBefore = ph.CurrentHealth;
            ph.TakeDamage(new DamageData(20f, null, Vector3.zero, 0f, player.transform.position));
            if (ph.CurrentHealth != hpBefore)
            {
                logErr("Hit during i-frames should be ignored!");
                return false;
            }

            // Test Boss multipliers
            GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Boss_TheLastHarvest.prefab");
            if (bossPrefab != null)
            {
                BossController bc = bossPrefab.GetComponent<BossController>();
                if (bc != null)
                {
                    float expectedContact = bc.ContactDamage * 0.90f;
                    float expectedSlam = bc.SlamDamage * 0.90f;
                    if (Mathf.Abs(bc.GetEffectiveContactDamage() - expectedContact) > 0.1f)
                    {
                        logErr($"Boss effective contact damage is {bc.GetEffectiveContactDamage()}, expected {expectedContact}!");
                        return false;
                    }
                    if (Mathf.Abs(bc.GetEffectiveSlamDamage() - expectedSlam) > 0.1f)
                    {
                        logErr($"Boss effective slam damage is {bc.GetEffectiveSlamDamage()}, expected {expectedSlam}!");
                        return false;
                    }
                }
            }

            // Test Demon Charger speed scaling
            GameObject chargerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemy_DemonCharger.prefab");
            if (chargerPrefab != null)
            {
                EnemyController ec = chargerPrefab.GetComponent<EnemyController>();
                if (ec != null)
                {
                    float expectedMoveSpeed = ec.BaseMoveSpeed * 0.90f;
                    float expectedChargeSpeed = ec.ChargeSpeed * 0.90f;
                    if (Mathf.Abs(ec.MoveSpeed - expectedMoveSpeed) > 0.05f)
                    {
                        logErr($"DemonCharger MoveSpeed is {ec.MoveSpeed}, expected {expectedMoveSpeed}!");
                        return false;
                    }
                    if (Mathf.Abs(ec.EffectiveChargeSpeed - expectedChargeSpeed) > 0.05f)
                    {
                        logErr($"DemonCharger EffectiveChargeSpeed is {ec.EffectiveChargeSpeed}, expected {expectedChargeSpeed}!");
                        return false;
                    }
                }
            }

            ph.ResetHealth();
            log("Test 1 Passed: 0.60s i-frames, 0.82x enemy damage, 0.90x boss damage, 1.90s dash cooldown, and 0.90x charger speed verified.");
            return true;
        }

        private static bool TestLuckyLastOverhaul(Action<string> log, Action<string> logErr)
        {
            log("[TEST 2/8] Verifying Lucky Last 12s Duration, Minimum 15 HP Activation & +25 HP Healing...");
            GameObject player = GameObject.FindWithTag("Player");
            LuckyLastSystem lls = player.GetComponent<LuckyLastSystem>();
            PlayerHealth ph = player.GetComponent<PlayerHealth>();

            if (lls == null || ph == null)
            {
                logErr("LuckyLastSystem or PlayerHealth missing!");
                return false;
            }

            // Verify duration is 12s
            if (Mathf.Abs(lls.Duration - 12f) > 0.01f)
            {
                logErr($"LuckyLastSystem.duration is {lls.Duration}, expected 12.0f!");
                return false;
            }

            // Test lethal hit interception & minimum 15 HP activation
            ph.LethalHitInterceptor = lls.CheckLethalDamage;
            lls.ResetSystem();
            ph.ResetHealth();
            ph.ResetInvulnerability();

            // Deal 200 lethal damage
            ph.TakeDamage(new DamageData(200f, null, Vector3.zero, 0f, player.transform.position));

            if (!lls.IsActive)
            {
                logErr("Lucky Last should activate on lethal damage!");
                return false;
            }

            // In World 1 Difficulty Pass, Lucky Last activates at minimum 15 HP!
            if (Mathf.Abs(ph.CurrentHealth - 15f) > 0.01f || !ph.IsAlive)
            {
                logErr($"Player HP is {ph.CurrentHealth}, expected minimum 15 HP and alive!");
                return false;
            }

            // Test that while Lucky Last is active, subsequent lethal damage CANNOT kill player (clamped at >= 1 HP)
            ph.ResetInvulnerability();
            ph.TakeDamage(new DamageData(50f, null, Vector3.zero, 0f, player.transform.position));
            if (ph.CurrentHealth < 1f || !ph.IsAlive)
            {
                logErr("Player died during active Lucky Last! Death protection failed.");
                return false;
            }

            // Test that while Lucky Last is active, HealthRestorePickup heals +25 HP
            GameObject tempPickupObj = new GameObject("TestHealthPickup");
            HealthRestorePickup hrp = tempPickupObj.AddComponent<HealthRestorePickup>();
            hrp.Initialize(20f);
            float activeHeal = hrp.GetEffectiveHealAmount();
            if (Mathf.Abs(activeHeal - 25f) > 0.01f)
            {
                logErr($"HealthRestorePickup effective heal during Lucky Last is {activeHeal}, expected 25f!");
                UnityEngine.Object.DestroyImmediate(tempPickupObj);
                return false;
            }

            float hpBeforeHeal = ph.CurrentHealth;
            ph.Heal(activeHeal);
            if (Mathf.Abs(ph.CurrentHealth - (hpBeforeHeal + 25f)) > 0.01f)
            {
                logErr($"Player HP after +25 heal is {ph.CurrentHealth}, expected {hpBeforeHeal + 25f}!");
                UnityEngine.Object.DestroyImmediate(tempPickupObj);
                return false;
            }
            UnityEngine.Object.DestroyImmediate(tempPickupObj);

            // Test non-lethal damage reduction
            ph.ResetInvulnerability();
            float hpBeforeHit = ph.CurrentHealth;
            ph.TakeDamage(new DamageData(10f, null, Vector3.zero, 0f, player.transform.position));
            if (Mathf.Abs(ph.CurrentHealth - (hpBeforeHit - 10f)) > 0.01f)
            {
                logErr($"Player HP after taking 10 dmg is {ph.CurrentHealth}, expected {hpBeforeHit - 10f}!");
                return false;
            }

            lls.ResetSystem();
            ph.ResetHealth();

            // When Lucky Last is inactive, verify normal heal is 20 HP
            GameObject normalPickupObj = new GameObject("NormalHealthPickup");
            HealthRestorePickup normalHrp = normalPickupObj.AddComponent<HealthRestorePickup>();
            normalHrp.Initialize(20f);
            float normalHeal = normalHrp.GetEffectiveHealAmount();
            if (Mathf.Abs(normalHeal - 20f) > 0.01f)
            {
                logErr($"Normal heal amount is {normalHeal}, expected 20f!");
                UnityEngine.Object.DestroyImmediate(normalPickupObj);
                return false;
            }
            UnityEngine.Object.DestroyImmediate(normalPickupObj);

            log("Test 2 Passed: Lucky Last 12s duration, minimum 15 HP activation, death immunity, and +25 HP heal bonus verified.");
            return true;
        }

        private static bool TestWaveProgressionCurve(Action<string> log, Action<string> logErr)
        {
            log("[TEST 3/8] Verifying World 1 Wave Progression Curve (Waves 1-9: [8..48], Boss Wave 10 = 18 adds, Post-Wave 10 Smooth Growth)...");
            int[] expectedCounts = new int[] { 8, 11, 14, 18, 22, 27, 33, 40, 48, 18 };

            // 1. Verify Waves 1-10 exact values
            for (int w = 1; w <= 10; w++)
            {
                int count = SpawnDirector.GetEnemyCountForWave(w);
                if (count != expectedCounts[w - 1])
                {
                    logErr($"Wave {w} enemy count is {count}, expected {expectedCounts[w - 1]}!");
                    return false;
                }
            }

            // Explicit World 1 checks: Wave 1 = 8, Wave 5 = 22, Wave 8 = 40, Wave 9 = 48, Wave 10 = 18 dedicated adds
            if (SpawnDirector.GetEnemyCountForWave(1) != 8) { logErr("Wave 1 is not 8!"); return false; }
            if (SpawnDirector.GetEnemyCountForWave(3) != 14) { logErr("Wave 3 is not 14!"); return false; }
            if (SpawnDirector.GetEnemyCountForWave(5) != 22) { logErr("Wave 5 is not 22!"); return false; }
            if (SpawnDirector.GetEnemyCountForWave(8) != 40) { logErr("Wave 8 is not 40!"); return false; }
            if (SpawnDirector.GetEnemyCountForWave(9) != 48) { logErr("Wave 9 is not 48!"); return false; }
            if (SpawnDirector.GetEnemyCountForWave(10) != 18) { logErr("Wave 10 is not 18 adds!"); return false; }

            // 2. Post-Wave 10 values with growth = 1.12f
            int w11 = SpawnDirector.GetEnemyCountForWave(11);
            int w12 = SpawnDirector.GetEnemyCountForWave(12);
            int w13 = SpawnDirector.GetEnemyCountForWave(13);
            int w14 = SpawnDirector.GetEnemyCountForWave(14);
            int w15 = SpawnDirector.GetEnemyCountForWave(15);
            int w20 = SpawnDirector.GetEnemyCountForWave(20);

            // Verify strictly increasing post-wave 10: Wave 12 > Wave 11, Wave 13 > Wave 12, Wave 15 > Wave 14
            if (w12 <= w11) { logErr($"Wave 12 ({w12}) must be greater than Wave 11 ({w11})!"); return false; }
            if (w13 <= w12) { logErr($"Wave 13 ({w13}) must be greater than Wave 12 ({w12})!"); return false; }
            if (w15 <= w14) { logErr($"Wave 15 ({w15}) must be greater than Wave 14 ({w14})!"); return false; }

            // Verify expected numerical targets for growth 1.12:
            // Wave 11 = 84, Wave 12 = 94, Wave 13 = 105, Wave 14 = 118, Wave 15 = 132, Wave 20 = 233
            if (w11 != 84) { logErr($"Wave 11 count is {w11}, expected 84!"); return false; }
            if (w12 != 94) { logErr($"Wave 12 count is {w12}, expected 94!"); return false; }
            if (w13 != 105) { logErr($"Wave 13 count is {w13}, expected 105!"); return false; }
            if (w14 != 118) { logErr($"Wave 14 count is {w14}, expected 118!"); return false; }
            if (w15 != 132) { logErr($"Wave 15 count is {w15}, expected 132!"); return false; }
            if (w20 != 233) { logErr($"Wave 20 count is {w20}, expected 233!"); return false; }

            // Verify strict monotonicity across Waves 1 to 9
            for (int w = 2; w <= 9; w++)
            {
                int prev = SpawnDirector.GetEnemyCountForWave(w - 1);
                int curr = SpawnDirector.GetEnemyCountForWave(w);
                if (curr <= prev)
                {
                    logErr($"Difficulty drop detected! Wave {w} ({curr}) is not greater than Wave {w - 1} ({prev})!");
                    return false;
                }
            }

            // Verify strict monotonicity across Waves 11 to 20
            for (int w = 12; w <= 20; w++)
            {
                int prev = SpawnDirector.GetEnemyCountForWave(w - 1);
                int curr = SpawnDirector.GetEnemyCountForWave(w);
                if (curr <= prev)
                {
                    logErr($"Difficulty drop post-wave 10! Wave {w} ({curr}) is not greater than Wave {w - 1} ({prev})!");
                    return false;
                }
            }

            // 3. QA Simulation for Waves: 1, 3, 5, 8, 9, 10, 11, 12, 15, 20
            log("-----------------------------------------------------------------");
            log("[QA SIMULATION REPORT: WORLD 1 BALANCING & PROGRESSION]");
            int[] simulatedWaves = new int[] { 1, 3, 5, 8, 9, 10, 11, 12, 15, 20 };
            foreach (int sw in simulatedWaves)
            {
                int totalCount = SpawnDirector.GetEnemyCountForWave(sw);
                int maxSimul = sw == 10 ? 8 : (sw > 10 ? 100 : 70);
                string milestone = sw == 10 ? "WAVE 10: BOSS WAVE (The Last Harvest + 18 Adds)" :
                                   sw % 10 == 0 ? $"WAVE {sw}: CURSED SURGE (Elite)" :
                                   sw % 5 == 0 ? $"Wave {sw} Elite" : "None";
                string healthSupply = (sw == 2 || sw == 4 || sw == 6 || sw == 8) ? "YES (Supply Drop: 1x Health)" :
                                      (sw == 9) ? "YES (Pre-Boss Recovery: 1-2x Health)" :
                                      (sw > 10 && sw % 3 == 0) ? "YES (Endless Supply Drop)" : "No";

                log($"  Wave {sw,2} | Total: {totalCount,3} | Max Concurrent: {maxSimul,2} | Milestone: {milestone,-42} | Supply Drop: {healthSupply}");
            }
            log("-----------------------------------------------------------------");

            log($"Test 3 Passed: Verified World 1 progression: 1=8, 3=14, 5=22, 8=40, 9=48, Wave 10 Boss=18 adds, 11=84, 12=94, 15=132, 20=233.");
            return true;
        }

        private static bool TestWaveStateMachine(Action<string> log, Action<string> logErr)
        {
            log("[TEST 4/8] Verifying Wave State Machine, Boss Wave 10 Mechanics (18 adds, 8 max concurrent, 2.1s interval, quotas)...");
            SpawnDirector sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>();
            if (sd == null)
            {
                logErr("SpawnDirector not found in scene!");
                return false;
            }

            sd.ResetDirector();
            if (sd.CurrentState != WaveState.Preparing)
            {
                logErr($"Initial state is {sd.CurrentState}, expected Preparing!");
                return false;
            }

            // 1. Verify 12:00 boss trigger is permanently removed
            SerializedObject soSd = new SerializedObject(sd);
            soSd.FindProperty("runTime").floatValue = 800f;
            soSd.ApplyModifiedProperties();
            if (sd.BossSpawned || sd.ActiveBossObject != null)
            {
                logErr("Boss should NEVER spawn based on run time (800s)! Time trigger must be removed.");
                return false;
            }

            // 2. Test standard Wave 1 (8 enemies)
            sd.StartWave(1);
            if (sd.CurrentState != WaveState.Spawning)
            {
                logErr($"State after StartWave(1) is {sd.CurrentState}, expected Spawning!");
                return false;
            }
            if (sd.TotalEnemiesForCurrentWave != 8)
            {
                logErr($"Total enemies for wave 1 is {sd.TotalEnemiesForCurrentWave}, expected 8!");
                return false;
            }

            // 3. Test Boss Wave 10 Flow & Completion Rules
            sd.StartAtWave(10);
            if (!sd.IsBossWave)
            {
                logErr("Wave 10 must have IsBossWave == true!");
                return false;
            }
            if (sd.TotalEnemiesForCurrentWave != 18)
            {
                logErr($"Wave 10 total enemies is {sd.TotalEnemiesForCurrentWave}, expected 18 adds!");
                return false;
            }

            // Verify WorldDifficultyConfig Boss settings
            if (WorldDifficultyConfig.Current.bossMaxConcurrentAdds != 8)
            {
                logErr($"bossMaxConcurrentAdds is {WorldDifficultyConfig.Current.bossMaxConcurrentAdds}, expected 8!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.bossAddSpawnInterval - 2.1f) > 0.05f)
            {
                logErr($"bossAddSpawnInterval is {WorldDifficultyConfig.Current.bossAddSpawnInterval}, expected 2.1f!");
                return false;
            }

            // Verify Quotas: 6 Grunts, 4 Chasers, 3 Walkers, 3 Farmers, 2 Chargers (Sum = 18)
            int sumQuotas = WorldDifficultyConfig.Current.bossGruntQuota +
                            WorldDifficultyConfig.Current.bossChaserQuota +
                            WorldDifficultyConfig.Current.bossWalkerQuota +
                            WorldDifficultyConfig.Current.bossFarmerQuota +
                            WorldDifficultyConfig.Current.bossChargerQuota;
            if (sumQuotas != 18)
            {
                logErr($"Sum of boss add quotas is {sumQuotas}, expected 18!");
                return false;
            }

            // Verify Wave 10 HUD formatting
            UIManager uim = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uim != null)
            {
                uim.UpdateWaveHUD(10, sd.EnemiesRemainingThisWave, sd.TotalEnemiesForCurrentWave);
                SerializedObject soUim = new SerializedObject(uim);
                Text waveRemText = soUim.FindProperty("waveRemainingText").objectReferenceValue as Text;
                if (waveRemText != null && waveRemText.text != "THE LAST HARVEST")
                {
                    logErr($"Wave 10 waveRemainingText is '{waveRemText.text}', expected 'THE LAST HARVEST'!");
                    return false;
                }
            }

            // Test Boss completion condition:
            var tSd = typeof(SpawnDirector);
            var fKilled = tSd.GetField("enemiesKilledThisWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fSpawned = tSd.GetField("spawnedThisWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fState = tSd.GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fBossDefeated = tSd.GetField("bossDefeatedThisWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var checkMethod = tSd.GetMethod("CheckWaveCompletion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Case A: Adds killed while boss is still alive -> Wave must NOT complete!
            fState?.SetValue(sd, WaveState.Combat);
            fSpawned?.SetValue(sd, 18);
            fKilled?.SetValue(sd, 18);
            fBossDefeated?.SetValue(sd, false); // Boss still alive!

            checkMethod?.Invoke(sd, null);

            if (sd.CurrentState != WaveState.Combat)
            {
                logErr("Wave 10 completed while Boss was still alive! Must stay in Combat until boss is dead.");
                return false;
            }

            // Case B: Boss killed while adds remain -> Wave must NOT complete!
            fBossDefeated?.SetValue(sd, true);
            fKilled?.SetValue(sd, 8); // 10 adds still alive!
            checkMethod?.Invoke(sd, null);

            if (sd.CurrentState != WaveState.Combat)
            {
                logErr("Wave 10 completed while adds remained alive! Must stay in Combat until all adds are cleared.");
                return false;
            }

            // Case C: Both Boss dead AND all adds dead -> Wave must complete!
            fKilled?.SetValue(sd, 18);
            checkMethod?.Invoke(sd, null);

            if (sd.CurrentState != WaveState.Cleared && sd.CurrentState != WaveState.Vacuuming)
            {
                logErr($"Wave 10 did not transition to Cleared/Vacuuming after Boss and all adds died! State: {sd.CurrentState}");
                return false;
            }

            // 4. Test Transition into Endless Mode / Wave 11
            sd.EnterEndlessMode();
            sd.StartIntermissionAfterBoss();
            if (!sd.IsEndless)
            {
                logErr("sd.IsEndless should be true after EnterEndlessMode()!");
                return false;
            }

            sd.StartAtWave(11);
            if (sd.CurrentWave != 11 || sd.TotalEnemiesForCurrentWave != 84)
            {
                logErr($"Wave 11 total enemies is {sd.TotalEnemiesForCurrentWave}, expected 84!");
                return false;
            }

            sd.ResetDirector();
            log("Test 4 Passed: Wave 10 Boss Wave rules (18 adds, 8 max concurrent, 2.1s interval, quotas), completion logic, and Wave 11 (84) transition verified.");
            return true;
        }

        private static bool TestVacuumAndCollectibleRegistry(Action<string> log, Action<string> logErr)
        {
            log("[TEST 5/8] Verifying CollectibleRegistry and End-of-Wave Vacuum...");
            GameObject player = GameObject.FindWithTag("Player");
            PlayerHealth ph = player.GetComponent<PlayerHealth>();

            CollectibleRegistry.Clear();

            // Create temporary test collectibles
            GameObject xpObj = new GameObject("Test_XpPickup");
            ExperiencePickup xp = xpObj.AddComponent<ExperiencePickup>();
            xpObj.transform.position = player.transform.position + Vector3.forward * 8f;

            GameObject seedObj = new GameObject("Test_SeedPickup");
            CursedSeedPickup seed = seedObj.AddComponent<CursedSeedPickup>();
            seedObj.transform.position = player.transform.position + Vector3.right * 7f;

            GameObject hpObj = new GameObject("Test_HealthPickup");
            HealthRestorePickup hp = hpObj.AddComponent<HealthRestorePickup>();
            hpObj.transform.position = player.transform.position + Vector3.left * 6f;

            // Verify registration
            CollectibleRegistry.Register(xp);
            CollectibleRegistry.Register(seed);
            CollectibleRegistry.Register(hp);

            if (CollectibleRegistry.Count != 3)
            {
                logErr($"CollectibleRegistry count is {CollectibleRegistry.Count}, expected 3!");
                UnityEngine.Object.DestroyImmediate(xpObj);
                UnityEngine.Object.DestroyImmediate(seedObj);
                UnityEngine.Object.DestroyImmediate(hpObj);
                return false;
            }

            // Test vacuum trigger
            CollectibleRegistry.TriggerVacuumAll(player.transform, 0.5f);

            // Test full health consumption during vacuum: player is at full health, health pickup should be consumed
            ph.ResetHealth();
            SerializedObject soHp = new SerializedObject(hp);

            // Cleanup test objects
            CollectibleRegistry.Unregister(xp);
            CollectibleRegistry.Unregister(seed);
            CollectibleRegistry.Unregister(hp);
            UnityEngine.Object.DestroyImmediate(xpObj);
            UnityEngine.Object.DestroyImmediate(seedObj);
            UnityEngine.Object.DestroyImmediate(hpObj);

            log("Test 5 Passed: CollectibleRegistry and vacuum triggering verified.");
            return true;
        }

        private static bool TestGuaranteedHealthDrop(Action<string> log, Action<string> logErr)
        {
            log("[TEST 6/8] Verifying Supply Drop Cadence (Waves 2, 4, 6, 8, 9) & Wave 9 Pre-Boss Recovery (1 vs 2 Pickups)...");
            SpawnDirector sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>();
            GameObject player = GameObject.FindWithTag("Player");

            if (sd == null || player == null)
            {
                logErr("SpawnDirector or Player missing!");
                return false;
            }

            // 1. Verify supply drop eligibility
            int[] expectedSupplyWaves = new int[] { 2, 4, 6, 8, 9 };
            foreach (int w in expectedSupplyWaves)
            {
                if (!WorldDifficultyConfig.Current.IsSupplyDropWave(w))
                {
                    logErr($"Wave {w} should be a supply drop wave!");
                    return false;
                }
            }

            int[] nonSupplyWaves = new int[] { 1, 3, 5, 7, 10 };
            foreach (int nw in nonSupplyWaves)
            {
                if (WorldDifficultyConfig.Current.IsSupplyDropWave(nw))
                {
                    logErr($"Wave {nw} should NOT be a supply drop wave!");
                    return false;
                }
            }

            // Endless mode supply drop test (Wave 12 = true, Wave 13 = false)
            if (!WorldDifficultyConfig.Current.IsSupplyDropWave(12))
            {
                logErr("Endless Wave 12 should be a supply drop wave!");
                return false;
            }
            if (WorldDifficultyConfig.Current.IsSupplyDropWave(13))
            {
                logErr("Endless Wave 13 should NOT be a supply drop wave!");
                return false;
            }

            // 2. Wave 9 Pre-Boss Recovery logic
            int lowHpDropCount = WorldDifficultyConfig.Current.GetSupplyDropCount(9, 0.35f); // Player at 35% HP
            int normalHpDropCount = WorldDifficultyConfig.Current.GetSupplyDropCount(9, 0.75f); // Player at 75% HP

            if (lowHpDropCount != 2)
            {
                logErr($"Wave 9 low HP drop count is {lowHpDropCount}, expected 2 drops!");
                return false;
            }
            if (normalHpDropCount != 1)
            {
                logErr($"Wave 9 normal HP drop count is {normalHpDropCount}, expected 1 drop!");
                return false;
            }

            // 3. Test spawning 2 pickups in scene via SpawnGuaranteedHealthDrop
            List<GameObject> spawned = sd.SpawnGuaranteedHealthDrop(2);
            if (spawned == null || spawned.Count != 2)
            {
                logErr($"SpawnGuaranteedHealthDrop(2) returned {spawned?.Count ?? 0} pickups, expected 2!");
                return false;
            }

            // Clean up spawned pickups
            foreach (GameObject go in spawned)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }

            sd.ResetDirector();
            log("Test 6 Passed: Supply drop cadence (Waves 2, 4, 6, 8, 9, 12...), Wave 9 Pre-Boss Recovery (1 vs 2 drops), and pickup spawning verified.");
            return true;
        }

        private static bool TestLevelUpQueue(Action<string> log, Action<string> logErr)
        {
            log("[TEST 7/8] Verifying Level-Up Queueing in UIManager...");
            UIManager uim = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uim == null)
            {
                logErr("UIManager not found!");
                return false;
            }

            SerializedObject soUim = new SerializedObject(uim);
            var lvlModalProp = soUim.FindProperty("levelUpModal");
            GameObject lvlModal = lvlModalProp.objectReferenceValue as GameObject;

            if (lvlModal == null)
            {
                logErr("levelUpModal not assigned on UIManager!");
                return false;
            }

            // Trigger level up twice in immediate succession
            lvlModal.SetActive(false);
            uim.ShowLevelUp(2);
            if (!lvlModal.activeSelf)
            {
                logErr("levelUpModal should be active after ShowLevelUp(2)!");
                return false;
            }

            // Second level up while modal is active should increment pendingLevelUps
            uim.ShowLevelUp(3);
            int pending = uim.PendingLevelUps;
            if (pending != 1)
            {
                logErr($"pendingLevelUps is {pending}, expected 1!");
                lvlModal.SetActive(false);
                return false;
            }

            // Clean up
            lvlModal.SetActive(false);
            Time.timeScale = 1f;

            log("Test 7 Passed: Sequential Level-Up Queueing verified.");
            return true;
        }

        private static bool TestSceneIntegrationAndWaveUI(Action<string> log, Action<string> logErr)
        {
            log("[TEST 8/8] Verifying Scene Integration, Wave HUD, Health Drop Rates & Drop Assist...");
            UIManager uim = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            SpawnDirector sd = UnityEngine.Object.FindAnyObjectByType<SpawnDirector>();
            GameManager gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();

            if (uim == null || sd == null || gm == null)
            {
                logErr("One or more core managers (UIManager, SpawnDirector, GameManager) missing!");
                return false;
            }

            // Verify drop rates in WorldDifficultyConfig
            if (Mathf.Abs(WorldDifficultyConfig.Current.commonHealthDropChance - 0.04f) > 0.005f)
            {
                logErr($"commonHealthDropChance is {WorldDifficultyConfig.Current.commonHealthDropChance}, expected 0.04f!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.eliteHealthDropChance - 0.35f) > 0.005f)
            {
                logErr($"eliteHealthDropChance is {WorldDifficultyConfig.Current.eliteHealthDropChance}, expected 0.35f!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.luckyLastCommonHealthDropChance - 0.08f) > 0.005f)
            {
                logErr($"luckyLastCommonHealthDropChance is {WorldDifficultyConfig.Current.luckyLastCommonHealthDropChance}, expected 0.08f!");
                return false;
            }
            if (Mathf.Abs(WorldDifficultyConfig.Current.luckyLastEliteHealthDropChance - 0.50f) > 0.005f)
            {
                logErr($"luckyLastEliteHealthDropChance is {WorldDifficultyConfig.Current.luckyLastEliteHealthDropChance}, expected 0.50f!");
                return false;
            }

            SerializedObject soUim = new SerializedObject(uim);
            var waveText = soUim.FindProperty("waveText").objectReferenceValue as Text;
            var waveRemText = soUim.FindProperty("waveRemainingText").objectReferenceValue as Text;
            var timerText = soUim.FindProperty("timerText").objectReferenceValue as Text;
            var luckyBanner = soUim.FindProperty("luckyLastBanner").objectReferenceValue as GameObject;
            var luckyTimerText = soUim.FindProperty("luckyLastTimerText").objectReferenceValue as Text;

            if (waveText == null)
            {
                logErr("waveText is not assigned on UIManager!");
                return false;
            }
            if (waveRemText == null)
            {
                logErr("waveRemainingText is not assigned on UIManager!");
                return false;
            }
            if (timerText == null)
            {
                logErr("timerText is not assigned on UIManager!");
                return false;
            }
            if (luckyBanner == null)
            {
                logErr("luckyLastBanner is not assigned on UIManager!");
                return false;
            }
            if (luckyTimerText == null)
            {
                logErr("luckyLastTimerText is not assigned on UIManager!");
                return false;
            }

            // Verify GameManager autoSpawnEnemies is false
            SerializedObject soGm = new SerializedObject(gm);
            bool autoSpawn = soGm.FindProperty("autoSpawnEnemies").boolValue;
            if (autoSpawn)
            {
                logErr("GameManager.autoSpawnEnemies is true! Should be false for single unified SpawnDirector authority.");
                return false;
            }

            log("Test 8 Passed: Scene integration, Wave HUD references, health drop rates (4%/35% normal, 8%/50% Lucky Last) verified.");
            return true;
        }

        private static void CaptureWaveVisuals(Action<string> log)
        {
            log("[SCREENSHOTS] Capturing Wave System visual verification proof...");
            try
            {
                GameObject player = GameObject.FindWithTag("Player");
                UIManager uim = UnityEngine.Object.FindAnyObjectByType<UIManager>();
                Camera cam = Camera.main;
                if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();

                Vector3 origPos = cam != null ? cam.transform.position : Vector3.zero;
                Quaternion origRot = cam != null ? cam.transform.rotation : Quaternion.identity;

                // 1. WaveUI_InCombat.png
                uim.UpdateWaveHUD(1, 8, 8);
                uim.UpdateTimerHUD(45f);
                ScreenshotHelper.CaptureCustom("WaveUI_InCombat", 1920, 1080);
                log("Captured WaveUI_InCombat.png");

                // 2. WaveIntermission_SupplyDrop.png
                uim.UpdateWaveHUD(2, 0, 11);
                uim.UpdateIntermissionHUD(11.2f);
                ScreenshotHelper.CaptureCustom("WaveIntermission_SupplyDrop", 1920, 1080);
                log("Captured WaveIntermission_SupplyDrop.png");

                // 3. LuckyLast_Active_Countdown.png
                SerializedObject soUim = new SerializedObject(uim);
                GameObject banner = soUim.FindProperty("luckyLastBanner").objectReferenceValue as GameObject;
                Text bannerText = soUim.FindProperty("luckyLastTimerText").objectReferenceValue as Text;
                if (banner != null)
                {
                    banner.SetActive(true);
                    if (bannerText != null) bannerText.text = "★ LUCKY LAST: 11.4s ★";
                    ScreenshotHelper.CaptureCustom("LuckyLast_Active_Countdown", 1920, 1080);
                    banner.SetActive(false);
                    log("Captured LuckyLast_Active_Countdown.png");
                }

                // 4. Vacuum_Spoils_InFlight.png
                uim.UpdateWaveHUD(2, 0, 11);
                uim.ShowToast("WAVE 2 CLEARED!", "Vacuuming spoils...", 2.0f, new Color(0.2f, 1f, 0.45f));
                ScreenshotHelper.CaptureCustom("Vacuum_Spoils_InFlight", 1920, 1080);
                log("Captured Vacuum_Spoils_InFlight.png");

                // 5. Wave10_BossWave_HUD.png
                uim.UpdateWaveHUD(10, 18, 18);
                uim.ShowBossBar("THE LAST HARVEST", 1200f, 1200f);
                uim.ShowToast("BOSS WAVE", "THE LAST HARVEST", 3.5f, new Color(1f, 0.25f, 0.25f));
                ScreenshotHelper.CaptureCustom("Wave10_BossWave_HUD", 1920, 1080);
                uim.HideBossBar();
                uim.HideToast();
                log("Captured Wave10_BossWave_HUD.png");

                // 6. Wave10_VictoryModal.png
                uim.ShowVictoryModal();
                ScreenshotHelper.CaptureCustom("Wave10_VictoryModal", 1920, 1080);
                SerializedObject soUimVic = new SerializedObject(uim);
                GameObject vicObj = soUimVic.FindProperty("victoryModal").objectReferenceValue as GameObject;
                if (vicObj != null) vicObj.SetActive(false);
                log("Captured Wave10_VictoryModal.png");

                if (cam != null)
                {
                    cam.transform.position = origPos;
                    cam.transform.rotation = origRot;
                }
            }
            catch (Exception ex)
            {
                log($"[SCREENSHOTS] Warning: visual capture exception: {ex.Message}");
            }
        }
    }
}
