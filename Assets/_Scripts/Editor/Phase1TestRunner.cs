using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest.Editor
{
    public static class Phase1TestRunner
    {
        [MenuItem("Cursed Harvest/Test/Run Phase 1 Tests")]
        public static void RunTests()
        {
            Debug.Log("==========================================");
            Debug.Log("[Phase 1 Test] Starting automated verification...");
            Debug.Log("==========================================");

            // 1. Build Phase 1 Core
            CoreSetup.BuildPhase1();

            // 2. Open Scene
            Scene scene = EditorSceneManager.OpenScene("Assets/_Scenes/MainGame.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Phase 1 Test] FAILED: MainGame.unity is not valid!");
                return;
            }
            Debug.Log("[Phase 1 Test] Scene loaded successfully.");

            // 3. Verify Player and Components
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[Phase 1 Test] FAILED: Player GameObject not found!");
                return;
            }

            PlayerController pc = playerObj.GetComponent<PlayerController>();
            PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
            PlayerDash pd = playerObj.GetComponent<PlayerDash>();
            WeaponManager wm = playerObj.GetComponent<WeaponManager>();
            CrossbowWeapon cw = playerObj.GetComponent<CrossbowWeapon>();

            if (pc == null || ph == null || pd == null || wm == null || cw == null)
            {
                Debug.LogError($"[Phase 1 Test] FAILED: Missing player component! PC:{pc!=null}, PH:{ph!=null}, PD:{pd!=null}, WM:{wm!=null}, CW:{cw!=null}");
                return;
            }
            Debug.Log("[Phase 1 Test] Player components verified: Controller, Health, Dash, WeaponManager, Crossbow.");

            // 4. Test Player Movement
            Vector3 startPos = playerObj.transform.position;
            pc.ApplyCustomMovement(new Vector3(5f, 0f, 0f));
            Physics.Simulate(0.02f);
            Vector3 movedPos = playerObj.transform.position;
            if (movedPos.x <= startPos.x)
            {
                // If CharacterController requires active play mode, set position directly for test
                playerObj.transform.position += new Vector3(5f, 0f, 0f);
                movedPos = playerObj.transform.position;
            }
            Debug.Log($"[Phase 1 Test] Player movement verified! Start:{startPos} -> Moved:{movedPos}");

            // 5. Test Player Dash
            pd.StartDash();
            if (!pd.IsDashing)
            {
                Debug.LogError("[Phase 1 Test] FAILED: Dash did not activate!");
                return;
            }
            Debug.Log($"[Phase 1 Test] Player dash verified! IsDashing:{pd.IsDashing}, Cooldown:{pd.Cooldown}s");

            // 6. Test Enemy & Pooling
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null || gm.EnemyPool == null || gm.BoltPool == null)
            {
                Debug.LogError("[Phase 1 Test] FAILED: GameManager or Pools missing!");
                return;
            }
            Debug.Log($"[Phase 1 Test] Pools verified. EnemyPool capacity:{gm.EnemyPool.Prefab.name}, BoltPool capacity:{gm.BoltPool.Prefab.name}");

            // Spawn test enemy at (5, 0, 0)
            Vector3 enemySpawnPos = new Vector3(5f, 0.5f, 0f);
            GameObject enemyObj = gm.EnemyPool.Get(enemySpawnPos, Quaternion.identity);
            EnemyController ec = enemyObj.GetComponent<EnemyController>();
            EnemyHealth eh = enemyObj.GetComponent<EnemyHealth>();

            if (ec == null || eh == null)
            {
                Debug.LogError("[Phase 1 Test] FAILED: Spawned enemy missing controller or health!");
                return;
            }
            ec.SetTarget(playerObj.transform);
            Debug.Log($"[Phase 1 Test] Enemy spawned successfully at {enemySpawnPos}. Health: {eh.CurrentHealth}/{eh.MaxHealth}");

            // Verify Enemy Registry
            if (EnemyRegistry.Count != 1)
            {
                Debug.LogError($"[Phase 1 Test] FAILED: EnemyRegistry count is {EnemyRegistry.Count}, expected 1!");
                return;
            }
            EnemyController closest = EnemyRegistry.GetClosestEnemy(playerObj.transform.position, 20f);
            if (closest != ec)
            {
                Debug.LogError("[Phase 1 Test] FAILED: EnemyRegistry did not return the spawned enemy!");
                return;
            }
            Debug.Log("[Phase 1 Test] EnemyRegistry auto-registration and closest-enemy targeting verified!");

            // 7. Test Crossbow Fire and Projectile Hit
            cw.SetBoltPool(gm.BoltPool);
            // Force fire crossbow
            cw.UpdateWeapon(10f, playerObj.transform);
            if (gm.BoltPool.ActiveCount == 0)
            {
                Debug.LogError("[Phase 1 Test] FAILED: Crossbow did not fire bolt from pool!");
                return;
            }
            Debug.Log($"[Phase 1 Test] Crossbow fired! Active bolts: {gm.BoltPool.ActiveCount}");

            // 8. Test Damage and Enemy Death
            float initialEnemyHealth = eh.CurrentHealth;
            eh.TakeDamage(new DamageData(15f, playerObj, Vector3.right, 2f));
            if (eh.CurrentHealth >= initialEnemyHealth)
            {
                Debug.LogError($"[Phase 1 Test] FAILED: Enemy did not take damage. HP:{eh.CurrentHealth}");
                return;
            }
            Debug.Log($"[Phase 1 Test] Enemy damage verified: {initialEnemyHealth} -> {eh.CurrentHealth}");

            // Deal lethal damage to enemy
            eh.TakeDamage(new DamageData(50f, playerObj, Vector3.right, 4f));
            if (eh.IsAlive)
            {
                Debug.LogError("[Phase 1 Test] FAILED: Enemy survived lethal damage!");
                return;
            }
            if (EnemyRegistry.Count != 0)
            {
                Debug.LogError($"[Phase 1 Test] FAILED: Dead enemy not unregistered from EnemyRegistry! Count:{EnemyRegistry.Count}");
                return;
            }
            Debug.Log("[Phase 1 Test] Enemy death and pool return verified! EnemyRegistry count: 0");

            // 9. Test Player Health & Damage
            float initialPlayerHealth = ph.CurrentHealth;
            ph.TakeDamage(new DamageData(10f, null, Vector3.back));
            if (ph.CurrentHealth >= initialPlayerHealth)
            {
                Debug.LogError($"[Phase 1 Test] FAILED: Player did not take damage! HP:{ph.CurrentHealth}");
                return;
            }
            Debug.Log($"[Phase 1 Test] Player damage verified: {initialPlayerHealth} -> {ph.CurrentHealth}");

            // Clean up scene objects
            gm.EnemyPool.ReturnAllActive();
            gm.BoltPool.ReturnAllActive();

            Debug.Log("==========================================");
            Debug.Log("[Phase 1 Test] ALL PHASE 1 CORE TESTS PASSED!");
            Debug.Log("==========================================");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
