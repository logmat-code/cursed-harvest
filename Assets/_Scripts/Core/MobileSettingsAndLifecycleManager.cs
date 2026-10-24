using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CursedHarvest
{
    public enum HapticType
    {
        Light,      // Dash, Upgrade Purchase
        Medium,     // Level Up, Lucky Harvest
        Heavy       // Lucky Last Activation
    }

    public enum HapticPattern
    {
        LightTap,
        LightTick,
        Dash,
        LevelUp,
        HarvestLucky,
        LuckyLast,
        UpgradePurchase
    }

    /// <summary>
    /// Authoritative manager for Mobile Target Frame Rate (60 FPS), Persistent Settings (Music, SFX, Haptics),
    /// Lightweight Android Haptics, and Android App Lifecycle (OnApplicationPause / OnApplicationFocus).
    /// </summary>
    public class MobileSettingsAndLifecycleManager : MonoBehaviour
    {
        private static MobileSettingsAndLifecycleManager _instance;
        public static MobileSettingsAndLifecycleManager Instance
        {
            get
            {
                if (_instance == null || !_instance)
                {
                    _instance = FindFirstObjectByType<MobileSettingsAndLifecycleManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MobileSettingsAndLifecycleManager");
                        _instance = go.AddComponent<MobileSettingsAndLifecycleManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private const string PrefMusicEnabled = "CH_Setting_MusicEnabled";
        private const string PrefSfxEnabled = "CH_Setting_SfxEnabled";
        private const string PrefHapticsEnabled = "CH_Setting_HapticsEnabled";
        private const string PrefMusicVolume = "CH_Setting_MusicVolume";
        private const string PrefSfxVolume = "CH_Setting_SfxVolume";

        private bool musicEnabled = true;
        private bool sfxEnabled = true;
        private bool hapticsEnabled = true;
        private float musicVolume = 0.22f;
        private float sfxVolume = 0.85f;
        private int lastHapticFrame = -1;
        private HapticType lastHapticType = HapticType.Light;

        public bool MusicEnabled
        {
            get => musicEnabled;
            set => SetMusicEnabled(value);
        }
        public bool SfxEnabled
        {
            get => sfxEnabled;
            set => SetSfxEnabled(value);
        }
        public bool HapticsEnabled
        {
            get => hapticsEnabled;
            set => SetHapticsEnabled(value);
        }
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public int LastHapticFrame => lastHapticFrame;
        public HapticType LastHapticType => lastHapticType;

        public static event Action OnSettingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapMobileRuntime()
        {
            ApplyTargetFrameRateAndQuality();
        }

        public static void ApplyTargetFrameRateAndQuality()
        {
            // Rule 39 & 74: Lock mobile targetFrameRate to 60 FPS and disable vSync override so 120Hz displays stay consistent
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            ApplyTargetFrameRateAndQuality();
            LoadSettings();
            SyncWithAudioManager();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ExperienceManagerOnLevelUpHook();
            HarvestPlot.OnHarvestGambleResolved -= HandleHarvestResolved;
            HarvestPlot.OnHarvestGambleResolved += HandleHarvestResolved;
            LuckyLastSystem.OnLuckyLastActivated -= HandleLuckyLastActivated;
            LuckyLastSystem.OnLuckyLastActivated += HandleLuckyLastActivated;
            MetaProgressionManager.OnUpgradePurchased -= HandleUpgradePurchased;
            MetaProgressionManager.OnUpgradePurchased += HandleUpgradePurchased;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            HarvestPlot.OnHarvestGambleResolved -= HandleHarvestResolved;
            LuckyLastSystem.OnLuckyLastActivated -= HandleLuckyLastActivated;
            MetaProgressionManager.OnUpgradePurchased -= HandleUpgradePurchased;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyTargetFrameRateAndQuality();
            SyncWithAudioManager();
            ExperienceManagerOnLevelUpHook();
        }

        private void ExperienceManagerOnLevelUpHook()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                ExperienceManager xp = player.GetComponent<ExperienceManager>();
                if (xp != null)
                {
                    xp.OnLevelUp -= HandleLevelUp;
                    xp.OnLevelUp += HandleLevelUp;
                }
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            TriggerHaptic(HapticType.Medium);
        }

        private void HandleHarvestResolved(HarvestResult result)
        {
            if (result == HarvestResult.Lucky)
            {
                TriggerHaptic(HapticType.Medium);
            }
        }

        private void HandleLuckyLastActivated(float duration)
        {
            TriggerHaptic(HapticType.Heavy);
        }

        private void HandleUpgradePurchased(MetaUpgradeBranch branch, int newRank)
        {
            TriggerHaptic(HapticType.Light);
        }

        public void LoadSettings()
        {
            musicEnabled = PlayerPrefs.GetInt(PrefMusicEnabled, 1) == 1;
            sfxEnabled = PlayerPrefs.GetInt(PrefSfxEnabled, 1) == 1;
            hapticsEnabled = PlayerPrefs.GetInt(PrefHapticsEnabled, 1) == 1;
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusicVolume, 0.22f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVolume, 0.85f));
        }

        public void SaveSettings()
        {
            PlayerPrefs.SetInt(PrefMusicEnabled, musicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefSfxEnabled, sfxEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefHapticsEnabled, hapticsEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(PrefMusicVolume, musicVolume);
            PlayerPrefs.SetFloat(PrefSfxVolume, sfxVolume);
            PlayerPrefs.Save();
        }

        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            SaveSettings();
            SyncWithAudioManager();
            OnSettingsChanged?.Invoke();
        }

        public void SetSfxEnabled(bool enabled)
        {
            sfxEnabled = enabled;
            SaveSettings();
            SyncWithAudioManager();
            OnSettingsChanged?.Invoke();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            hapticsEnabled = enabled;
            SaveSettings();
            if (enabled) TriggerHaptic(HapticType.Light);
            OnSettingsChanged?.Invoke();
        }

        public void SyncWithAudioManager()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(musicEnabled ? musicVolume : 0f);
                AudioManager.Instance.SetSFXVolume(sfxEnabled ? sfxVolume : 0f);
                AudioManager.Instance.SetDroneMuted(!musicEnabled);
            }
        }

        public void TriggerHaptic(HapticPattern pattern)
        {
            HapticType mapped = pattern switch
            {
                HapticPattern.LuckyLast => HapticType.Heavy,
                HapticPattern.LevelUp => HapticType.Medium,
                HapticPattern.HarvestLucky => HapticType.Medium,
                _ => HapticType.Light
            };
            TriggerHaptic(mapped);
        }

        public void TriggerHaptic(HapticType type = HapticType.Light)
        {
            if (!hapticsEnabled) return;

            lastHapticFrame = Time.frameCount;
            lastHapticType = type;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                long durationMs = type switch
                {
                    HapticType.Light => 16L,
                    HapticType.Medium => 28L,
                    HapticType.Heavy => 45L,
                    _ => 16L
                };
                int amplitude = type switch
                {
                    HapticType.Light => 65,
                    HapticType.Medium => 120,
                    HapticType.Heavy => 200,
                    _ => 65
                };

                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                    {
                        using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                        {
                            int sdkInt = versionClass.GetStatic<int>("SDK_INT");
                            if (sdkInt >= 26)
                            {
                                using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                                using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude))
                                {
                                    vibrator.Call("vibrate", effect);
                                }
                            }
                            else
                            {
                                vibrator.Call("vibrate", durationMs);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Safely ignore on devices without vibrator service
            }
#endif
        }

        // ==================== ANDROID LIFECYCLE (RULES 47 & 48) ====================

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                HandleBackgrounding();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                HandleBackgrounding();
            }
        }

        public void SimulateBackgroundLifecycle(bool isBackgrounded)
        {
            if (isBackgrounded)
            {
                HandleBackgrounding();
            }
            // Rule 48: On return from background, keep Pause open — do NOT auto-resume!
        }

        private void HandleBackgrounding()
        {
            // Persist all saves immediately on backgrounding
            if (MetaProgressionManager.Instance != null)
            {
                MetaProgressionManager.Instance.SaveProgression();
            }
            SaveSettings();

            // If in gameplay and not already in a terminal/level-up modal, open Pause modal and keep it paused
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PauseOnAppBackground();
            }
        }
    }
}
