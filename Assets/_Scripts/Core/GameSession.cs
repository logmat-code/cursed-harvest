using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest
{
    public enum MainMenuScreen
    {
        Home,
        WorldSelect,
        ArenaSelect,
        Upgrades,
        Credits
    }

    /// <summary>
    /// Authoritative persistent session manager bridging Frontend and Gameplay for Mobile v1.1.0.
    /// Manages 3 Worlds with 5 Arenas each (15 Arenas total).
    /// Backward-compatible with previous saves: preserves Essence, Meta Upgrades, Settings,
    /// and migrates previous HighestUnlockedWorld safely.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        private static GameSession _instance;

        public static GameSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameSession>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameSession");
                        _instance = go.AddComponent<GameSession>();
                    }
                }
                return _instance;
            }
        }

        private const string PrefHighestUnlockedWorld = "CH_HighestUnlockedWorld";
        private const string PrefHighestUnlockedArena = "CH_HighestUnlockedArena";
        private const string PrefWorldCompletedPrefix = "CH_World_Completed_";
        private const string PrefArenaCompletedPrefix = "CH_Arena_Completed_";
        private const string PrefSaveMigrationV11Done = "CH_SaveMigration_v1_1_Done";

        [Header("Active Run State")]
        [SerializeField] private int selectedWorldId = 1;      // 1..3
        [SerializeField] private int selectedArenaId = 1;      // 1..15
        [SerializeField] private MainMenuScreen initialMenuScreen = MainMenuScreen.Home;

        public int SelectedWorldId => selectedWorldId;
        public int SelectedArenaId => selectedArenaId;
        public WorldConfig CurrentWorldConfig
        {
            get
            {
                WorldConfig cfg = WorldConfig.GetConfig(selectedWorldId);
                if (cfg != null)
                {
                    cfg.activeArena = WorldConfig.GetArenaConfig(selectedArenaId);
                    cfg.activeArenaIndex = cfg.activeArena != null ? cfg.activeArena.arenaIndexInWorld : 1;
                }
                return cfg;
            }
        }

        public ArenaConfig CurrentArenaConfig => WorldConfig.GetArenaConfig(selectedArenaId);

        public MainMenuScreen InitialMenuScreen
        {
            get => initialMenuScreen;
            set => initialMenuScreen = value;
        }

        public static event Action<int> OnWorldProgressionChanged;
        public static event Action<int> OnArenaProgressionChanged;
        public static event Action<int> OnWorldCompletedEvent;
        public static event Action<int> OnArenaCompletedEvent;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            PerformSaveMigrationIfNeeded();

            // Sync initial difficulty config
            WorldDifficultyConfig.Current = WorldDifficultyConfig.FromWorldConfig(CurrentWorldConfig);
        }

        // ==================== SAVE DATA MIGRATION ====================

        private void PerformSaveMigrationIfNeeded()
        {
            if (PlayerPrefs.GetInt(PrefSaveMigrationV11Done, 0) == 1) return;

            // Migrate old HighestUnlockedWorld (which was 1..5 in v1.0)
            int oldUnlockedWorld = PlayerPrefs.GetInt(PrefHighestUnlockedWorld, 1);
            int newHighestArena = 1;

            if (oldUnlockedWorld >= 5)
            {
                newHighestArena = 15; // Full unlock if user had beaten World 4/5
            }
            else if (oldUnlockedWorld == 4)
            {
                newHighestArena = 11; // World 3 unlocked
            }
            else if (oldUnlockedWorld == 3)
            {
                newHighestArena = 6;  // World 2 unlocked
            }
            else if (oldUnlockedWorld == 2)
            {
                newHighestArena = 3;  // Partway into World 1
            }

            int currentHighestArena = PlayerPrefs.GetInt(PrefHighestUnlockedArena, 1);
            if (newHighestArena > currentHighestArena)
            {
                PlayerPrefs.SetInt(PrefHighestUnlockedArena, newHighestArena);
            }

            // Map old world completions to arenas
            for (int w = 1; w <= 5; w++)
            {
                if (PlayerPrefs.GetInt(PrefWorldCompletedPrefix + w, 0) == 1)
                {
                    PlayerPrefs.SetInt(PrefArenaCompletedPrefix + w, 1);
                }
            }

            // Clamp World ID to 1..3
            int clampedWorld = Mathf.Clamp(PlayerPrefs.GetInt(PrefHighestUnlockedWorld, 1), 1, 3);
            PlayerPrefs.SetInt(PrefHighestUnlockedWorld, clampedWorld);

            PlayerPrefs.SetInt(PrefSaveMigrationV11Done, 1);
            PlayerPrefs.Save();
            Debug.Log($"[GameSession] Save data migrated to v1.1.0 successfully. Highest Arena: {HighestUnlockedArena}, Highest World: {HighestUnlockedWorld}.");
        }

        // ==================== PROGRESSION SYSTEM ====================

        public int HighestUnlockedWorld
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefHighestUnlockedWorld, 1), 1, 3);
            set
            {
                int clamped = Mathf.Clamp(value, 1, 3);
                PlayerPrefs.SetInt(PrefHighestUnlockedWorld, clamped);
                PlayerPrefs.Save();
                OnWorldProgressionChanged?.Invoke(clamped);
            }
        }

        public int HighestUnlockedArena
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefHighestUnlockedArena, 1), 1, 15);
            set
            {
                int clamped = Mathf.Clamp(value, 1, 15);
                PlayerPrefs.SetInt(PrefHighestUnlockedArena, clamped);
                PlayerPrefs.Save();
                OnArenaProgressionChanged?.Invoke(clamped);

                // Update HighestUnlockedWorld accordingly
                int targetWorld = clamped <= 5 ? 1 : (clamped <= 10 ? 2 : 3);
                if (targetWorld > HighestUnlockedWorld)
                {
                    HighestUnlockedWorld = targetWorld;
                }
            }
        }

        public bool IsWorldUnlocked(int worldId)
        {
            if (worldId <= 1) return true;
            if (worldId > 3) return false;
            return HighestUnlockedWorld >= worldId;
        }

        public bool IsWorldCompleted(int worldId)
        {
            return PlayerPrefs.GetInt(PrefWorldCompletedPrefix + worldId, 0) == 1;
        }

        public bool IsArenaUnlocked(int globalArenaId)
        {
            if (globalArenaId <= 1) return true;
            if (globalArenaId > 15) return false;
            return HighestUnlockedArena >= globalArenaId;
        }

        public bool IsArenaCompleted(int globalArenaId)
        {
            return PlayerPrefs.GetInt(PrefArenaCompletedPrefix + globalArenaId, 0) == 1;
        }

        public void UnlockAllWorlds()
        {
            HighestUnlockedWorld = 3;
            HighestUnlockedArena = 15;
            for (int w = 1; w <= 3; w++) PlayerPrefs.SetInt(PrefWorldCompletedPrefix + w, 1);
            for (int a = 1; a <= 15; a++) PlayerPrefs.SetInt(PrefArenaCompletedPrefix + a, 1);
            PlayerPrefs.Save();
            OnWorldProgressionChanged?.Invoke(HighestUnlockedWorld);
            OnArenaProgressionChanged?.Invoke(HighestUnlockedArena);
        }

        public void CompleteArena(int globalArenaId)
        {
            Debug.Log($"[GameSession] Completing Arena {globalArenaId}!");
            PlayerPrefs.SetInt(PrefArenaCompletedPrefix + globalArenaId, 1);

            if (globalArenaId >= HighestUnlockedArena && globalArenaId < 15)
            {
                HighestUnlockedArena = globalArenaId + 1;
                Debug.Log($"[GameSession] Arena {globalArenaId + 1} UNLOCKED!");
            }

            // If this was Arena 5, 10, or 15, mark corresponding World completed
            if (globalArenaId == 5) CompleteWorld(1);
            else if (globalArenaId == 10) CompleteWorld(2);
            else if (globalArenaId == 15) CompleteWorld(3);
            else PlayerPrefs.Save();

            OnArenaCompletedEvent?.Invoke(globalArenaId);
        }

        public void CompleteWorld(int worldId)
        {
            Debug.Log($"[GameSession] Completing World {worldId}!");
            PlayerPrefs.SetInt(PrefWorldCompletedPrefix + worldId, 1);

            if (worldId >= HighestUnlockedWorld && worldId < 3)
            {
                HighestUnlockedWorld = worldId + 1;
                Debug.Log($"[GameSession] World {worldId + 1} UNLOCKED!");
            }
            PlayerPrefs.Save();

            OnWorldCompletedEvent?.Invoke(worldId);
            OnWorldProgressionChanged?.Invoke(HighestUnlockedWorld);
        }

        // ==================== RUN NAVIGATION ====================

        public void SetSelectedWorld(int worldId)
        {
            selectedWorldId = Mathf.Clamp(worldId, 1, 3);
            // Default selected arena to first arena in that world
            selectedArenaId = (selectedWorldId - 1) * 5 + 1;
            WorldConfig config = CurrentWorldConfig;
            WorldDifficultyConfig.Current = WorldDifficultyConfig.FromWorldConfig(config);
        }

        public void SetSelectedArena(int globalArenaId)
        {
            selectedArenaId = Mathf.Clamp(globalArenaId, 1, 15);
            selectedWorldId = (selectedArenaId - 1) / 5 + 1;
            WorldConfig config = CurrentWorldConfig;
            WorldDifficultyConfig.Current = WorldDifficultyConfig.FromWorldConfig(config);
        }

        public void StartRun(int globalArenaId)
        {
            SetSelectedArena(globalArenaId);
            ArenaConfig arena = CurrentArenaConfig;
            Debug.Log($"[GameSession] Starting Run for World {selectedWorldId}, Arena {selectedArenaId}: '{arena.arenaName}' ({arena.difficultyTier})...");
            Time.timeScale = 1f;

            SceneManager.LoadScene("MainGame");
        }

        public void RestartRun()
        {
            StartRun(selectedArenaId);
        }

        public void ReturnToArenaSelect()
        {
            Debug.Log("[GameSession] Returning to Arena Select...");
            initialMenuScreen = MainMenuScreen.ArenaSelect;
            Time.timeScale = 1f;

            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        /// <summary>v1.1.2: return straight to the Arena Select of the current World.</summary>
        public void ReturnToArenaSelect()
        {
            initialMenuScreen = MainMenuScreen.ArenaSelect;
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("MainMenu")) SceneManager.LoadScene("MainMenu");
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ReturnToWorldSelect()
        {
            Debug.Log("[GameSession] Returning to World Select...");
            initialMenuScreen = MainMenuScreen.WorldSelect;
            Time.timeScale = 1f;

            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void ReturnToHome()
        {
            Debug.Log("[GameSession] Returning to Home...");
            initialMenuScreen = MainMenuScreen.Home;
            Time.timeScale = 1f;

            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        // ==================== DEBUG UTILITIES ====================

        public void UnlockAllWorldsAndArenas()
        {
            HighestUnlockedWorld = 3;
            HighestUnlockedArena = 15;
            for (int i = 1; i <= 3; i++)
            {
                PlayerPrefs.SetInt(PrefWorldCompletedPrefix + i, 1);
            }
            for (int a = 1; a <= 15; a++)
            {
                PlayerPrefs.SetInt(PrefArenaCompletedPrefix + a, 1);
            }
            PlayerPrefs.Save();
            Debug.Log("[GameSession DEBUG] All 3 Worlds & 15 Arenas Unlocked & Marked Completed.");
            OnWorldProgressionChanged?.Invoke(3);
            OnArenaProgressionChanged?.Invoke(15);
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PrefHighestUnlockedWorld);
            PlayerPrefs.DeleteKey(PrefHighestUnlockedArena);
            PlayerPrefs.DeleteKey(PrefSaveMigrationV11Done);
            for (int i = 1; i <= 3; i++)
            {
                PlayerPrefs.DeleteKey(PrefWorldCompletedPrefix + i);
            }
            for (int a = 1; a <= 15; a++)
            {
                PlayerPrefs.DeleteKey(PrefArenaCompletedPrefix + a);
            }
            PlayerPrefs.Save();
            Debug.Log("[GameSession DEBUG] Progression Reset: Only Arena 1-1 Unlocked.");
            OnWorldProgressionChanged?.Invoke(1);
            OnArenaProgressionChanged?.Invoke(1);
        }
    }
}
