using System;
using UnityEngine;

namespace CursedHarvest
{
    public enum MobileQualityTier
    {
        MobileHigh,         // Native full resolution (1.0x), 4x MSAA, full shadows, anisotropic textures
        MobilePerformance   // 0.80x internal render scale, 2x MSAA, balanced shadows, stable 60 FPS
    }

    /// <summary>
    /// Manages authoritative mobile visual resolution and performance quality tiers for Android Portrait.
    /// Ensures 1080p and 1440p displays maintain crystal clear 3D rendering and sharp UI without blurry downsampling.
    /// </summary>
    public class MobileQualityManager : MonoBehaviour
    {
        private static MobileQualityManager _instance;
        public static MobileQualityManager Instance
        {
            get
            {
                if (_instance == null || !_instance)
                {
                    _instance = FindFirstObjectByType<MobileQualityManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("MobileQualityManager");
                        _instance = go.AddComponent<MobileQualityManager>();
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private const string PrefsQualityTierKey = "CH_MobileQualityTier";
        private MobileQualityTier currentTier = MobileQualityTier.MobileHigh;

        public MobileQualityTier CurrentTier => currentTier;
        public static event Action<MobileQualityTier> OnQualityTierChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            LoadAndApplyQuality();
        }

        public void LoadAndApplyQuality()
        {
            int saved = PlayerPrefs.GetInt(PrefsQualityTierKey, (int)MobileQualityTier.MobileHigh);
            currentTier = (MobileQualityTier)saved;
            ApplyQualityTier(currentTier, false);
        }

        public void SetQualityTier(MobileQualityTier tier)
        {
            currentTier = tier;
            PlayerPrefs.SetInt(PrefsQualityTierKey, (int)currentTier);
            PlayerPrefs.Save();
            ApplyQualityTier(currentTier, true);
        }

        public void ToggleQualityTier()
        {
            SetQualityTier(currentTier == MobileQualityTier.MobileHigh ? MobileQualityTier.MobilePerformance : MobileQualityTier.MobileHigh);
        }

        private void ApplyQualityTier(MobileQualityTier tier, bool notify)
        {
            // Target stable 60 FPS on mobile
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0; // Disable vsync so 60 FPS cap is authoritative

            if (tier == MobileQualityTier.MobileHigh)
            {
                // MOBILE HIGH: Prioritize absolute sharpness on 1080p/1440p AMOLED displays
                QualitySettings.antiAliasing = 4; // 4x MSAA for crisp polygonal edges
                QualitySettings.shadowDistance = 45f;
                QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.shadowCascades = 2;
                QualitySettings.globalTextureMipmapLimit = 0; // Full texture resolution, no downsampling

                // Native full render scale
                ScalableBufferManager.ResizeBuffers(1.0f, 1.0f);
            }
            else
            {
                // MOBILE PERFORMANCE: Optimized for sustained low-thermal 60 FPS
                QualitySettings.antiAliasing = 2; // 2x MSAA
                QualitySettings.shadowDistance = 28f;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowCascades = 1;
                QualitySettings.globalTextureMipmapLimit = 0;

                // 0.80x internal render scale for 3D world (UI Canvas remains full 1080p/1440p native)
                ScalableBufferManager.ResizeBuffers(0.80f, 0.80f);
            }

            if (notify)
            {
                OnQualityTierChanged?.Invoke(currentTier);
            }
        }
    }
}
