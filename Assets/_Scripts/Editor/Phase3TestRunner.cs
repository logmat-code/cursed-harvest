using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class Phase3TestRunner
    {
        [MenuItem("Cursed Harvest/Test/Run Phase 3 Tests")]
        public static void RunTests()
        {
            Debug.Log("==========================================");
            Debug.Log("[Phase 3 Test] Starting Lucky Last verification...");
            Debug.Log("==========================================");

            // 1. Build Phase 2 Core (with Lucky Last updates)
            CoreSetup.BuildPhase2();

            // 2. Open Scene
            Scene scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Phase 3 Test] FAILED: MainGame.unity is not valid!");
                return;
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[Phase 3 Test] FAILED: Player not found!");
                return;
            }

            PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
            PlayerController pc = playerObj.GetComponent<PlayerController>();
            WeaponManager wm = playerObj.GetComponent<WeaponManager>();
            LuckyLastSystem lucky = playerObj.GetComponent<LuckyLastSystem>();

            if (ph == null || pc == null || wm == null || lucky == null)
            {
                Debug.LogError("[Phase 3 Test] FAILED: Required player components missing!");
                return;
            }

            // Hook into player health interception
            ph.LethalHitInterceptor = lucky.CheckLethalDamage;

            // 3. Test Initial State
            lucky.ResetSystem();
            ph.ResetHealth();
            if (lucky.CurrentCharges != 1)
            {
                Debug.LogError($"[Phase 3 Test] FAILED: Initial charges expected 1, got {lucky.CurrentCharges}!");
                return;
            }
            if (lucky.IsActive)
            {
                Debug.LogError("[Phase 3 Test] FAILED: Lucky Last should not be active initially!");
                return;
            }
            Debug.Log($"[Phase 3 Test] Initial state verified. HP: {ph.CurrentHealth}/{ph.MaxHealth}, Charges: {lucky.CurrentCharges}");

            // 4. Test Non-Lethal Damage
            ph.TakeDamage(new DamageData(40f));
            if (ph.CurrentHealth != 60f || lucky.CurrentCharges != 1 || lucky.IsActive)
            {
                Debug.LogError($"[Phase 3 Test] FAILED: Non-lethal damage corrupted Lucky Last state! HP:{ph.CurrentHealth}, Charges:{lucky.CurrentCharges}, Active:{lucky.IsActive}");
                return;
            }
            Debug.Log($"[Phase 3 Test] Non-lethal damage verified. HP: {ph.CurrentHealth}, Charges remain {lucky.CurrentCharges}, Active: {lucky.IsActive}");

            // 5. Test Lethal Interception
            float dmgBefore = wm.GlobalDamageMultiplier;
            float speedBefore = pc.MoveSpeed;

            // Reset invulnerability window for synchronous test
            ph.ResetInvulnerability();

            // Deal 100 damage (lethal when at 60 HP)
            ph.TakeDamage(new DamageData(100f));

            if (!ph.IsAlive)
            {
                Debug.LogError("[Phase 3 Test] FAILED: Player died instead of being saved by Lucky Last!");
                return;
            }
            if (ph.CurrentHealth != 1f)
            {
                Debug.LogError($"[Phase 3 Test] FAILED: Player health should be 1 HP after Lucky Last, was {ph.CurrentHealth}!");
                return;
            }
            if (lucky.CurrentCharges != 0)
            {
                Debug.LogError($"[Phase 3 Test] FAILED: Lucky Last charge was not consumed! Remaining: {lucky.CurrentCharges}");
                return;
            }
            if (!lucky.IsActive)
            {
                Debug.LogError("[Phase 3 Test] FAILED: Lucky Last state was not activated!");
                return;
            }
            Debug.Log($"[Phase 3 Test] Lethal damage intercepted successfully! HP: {ph.CurrentHealth}, Charges: {lucky.CurrentCharges}, Active: {lucky.IsActive}");

            // 6. Verify Buffs during Lucky Last
            if (wm.GlobalDamageMultiplier <= dmgBefore || pc.MoveSpeed <= speedBefore)
            {
                Debug.LogError($"[Phase 3 Test] FAILED: Buffs not applied during Lucky Last! Dmg:{wm.GlobalDamageMultiplier} (was {dmgBefore}), Speed:{pc.MoveSpeed} (was {speedBefore})");
                return;
            }
            Debug.Log($"[Phase 3 Test] Lucky Last buffs verified! DamageMultiplier: {dmgBefore} -> {wm.GlobalDamageMultiplier}, MoveSpeed: {speedBefore} -> {pc.MoveSpeed}");

            // 7. Test Future Lethal Damage (When 0 Charges remain)
            bool playerDiedEventFired = false;
            ph.OnPlayerDied += () => { playerDiedEventFired = true; };

            // Reset invulnerability window for synchronous test
            ph.ResetInvulnerability();

            // Deal lethal hit while at 1 HP and 0 charges
            ph.TakeDamage(new DamageData(10f));

            if (ph.IsAlive || !playerDiedEventFired)
            {
                Debug.LogError($"[Phase 3 Test] FAILED: Player survived without charges! IsAlive:{ph.IsAlive}, EventFired:{playerDiedEventFired}");
                return;
            }
            Debug.Log("[Phase 3 Test] Clean player death without charges verified! Future death is not broken.");

            // 8. Test Charge Addition and Cap
            lucky.ResetSystem(); // Charges = 1
            lucky.AddCharge();   // Charges = 2 (max)
            lucky.AddCharge();   // Exceeds max, should remain 2
            if (lucky.CurrentCharges != 2)
            {
                Debug.LogError($"[Phase 3 Test] FAILED: Charge cap should be 2, got {lucky.CurrentCharges}!");
                return;
            }
            Debug.Log($"[Phase 3 Test] Charge addition and cap (max 2) verified! Charges: {lucky.CurrentCharges}");

            Debug.Log("==========================================");
            Debug.Log("[Phase 3 Test] ALL PHASE 3 LUCKY LAST TESTS PASSED!");
            Debug.Log("==========================================");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
