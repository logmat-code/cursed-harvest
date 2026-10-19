using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest
{
    public enum MainMenuScreen
    {
        Home,
        WorldSelect,
        Upgrades,
        Credits
    }

    /// <summary>
    /// Authoritative persistent session manager that bridges Frontend and Gameplay.
    /// Survives scene transitions via DontDestroyOnLoad.
    /// Manages active World selection, World unlock progression, and scene transitions.
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

        private const string PrefHighestUnlocked = "CH_HighestUnlockedWorld";
        private const string PrefWorldCompletedPrefix = "CH_World_Completed_";

        [Header("Active Run State")]
        [SerializeField] private int selectedWorldId = 1;
        [SerializeField] private MainMenuScreen initialMenuScreen = MainMenuScreen.Home;

        public int SelectedWorldId => selectedWorldId;
        public WorldConfig CurrentWorldConfig => WorldConfig.GetConfig(selectedWorldId);
        public MainMenuScreen InitialMenuScreen
        {
            get => initialMenuScreen;
            set => initialMenuScreen = value;
        }

        public static event Action<int> OnWorldProgressionChanged;
        public static event Action<int> OnWorldCompletedEvent;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Sync initial difficulty config
            WorldDifficultyConfig.Current = WorldDifficultyConfig.FromWorldConfig(CurrentWorldConfig);
        }

        // ==================== PROGRESSION SYSTEM ====================

        public int HighestUnlockedWorld
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefHighestUnlocked, 1), 1, 5);
            set
            {
                int clamped = Mathf.Clamp(value, 1, 5);
                PlayerPrefs.SetInt(PrefHighestUnlocked, clamped);
                PlayerPrefs.Save();
                OnWorldProgressionChanged?.Invoke(clamped);
            }
        }

        public bool IsWorldUnlocked(int worldId)
        {
            if (worldId <= 1) return true;
            if (worldId > 5) return false;

            // An unlocked world must have its requirement met or highest unlocked >= worldId
            WorldConfig config = WorldConfig.GetConfig(worldId);
            if (config != null && config.unlockRequirement > 0)
            {
                return IsWorldCompleted(config.unlockRequirement) || HighestUnlockedWorld >= worldId;
            }

            return HighestUnlockedWorld >= worldId;
        }

        public bool IsWorldCompleted(int worldId)
        {
            return PlayerPrefs.GetInt(PrefWorldCompletedPrefix + worldId, 0) == 1;
        }

        public void CompleteWorld(int worldId)
        {
            Debug.Log($"[GameSession] Completing World {worldId}!");
            PlayerPrefs.SetInt(PrefWorldCompletedPrefix + worldId, 1);

            if (worldId >= HighestUnlockedWorld && worldId < 5)
            {
                HighestUnlockedWorld = worldId + 1;
                Debug.Log($"[GameSession] World {worldId + 1} UNLOCKED!");
            }
            else
            {
                PlayerPrefs.Save();
            }

            OnWorldCompletedEvent?.Invoke(worldId);
            OnWorldProgressionChanged?.Invoke(HighestUnlockedWorld);
        }

        // ==================== RUN NAVIGATION ====================

        public void SetSelectedWorld(int worldId)
        {
            selectedWorldId = Mathf.Clamp(worldId, 1, 5);
            WorldConfig config = WorldConfig.GetConfig(selectedWorldId);
            WorldDifficultyConfig.Current = WorldDifficultyConfig.FromWorldConfig(config);
        }

        public void StartRun(int worldId)
        {
            SetSelectedWorld(worldId);
            WorldConfig config = CurrentWorldConfig;
            Debug.Log($"[GameSession] Starting Run for World {selectedWorldId}: '{config.displayName}' ({config.difficultyTier})...");
            Time.timeScale = 1f;

            // Load MainGame
            SceneManager.LoadScene("MainGame");
        }

        public void RestartRun()
        {
            StartRun(selectedWorldId);
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
                // Fallback: If MainMenu scene is not yet in build settings, reopen current or show UI
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

        public void UnlockAllWorlds()
        {
            HighestUnlockedWorld = 5;
            for (int i = 1; i <= 5; i++)
            {
                PlayerPrefs.SetInt(PrefWorldCompletedPrefix + i, 1);
            }
            PlayerPrefs.Save();
            Debug.Log("[GameSession DEBUG] All 5 Worlds Unlocked & Marked Completed.");
            OnWorldProgressionChanged?.Invoke(5);
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PrefHighestUnlocked);
            for (int i = 1; i <= 5; i++)
            {
                PlayerPrefs.DeleteKey(PrefWorldCompletedPrefix + i);
            }
            PlayerPrefs.Save();
            Debug.Log("[GameSession DEBUG] Progression Reset: Only World 1 Unlocked.");
            OnWorldProgressionChanged?.Invoke(1);
        }
    }
}
