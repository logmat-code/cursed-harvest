using System;
using UnityEngine;

namespace CursedHarvest
{
    public enum MobileQualityTier
    {
        MobileBalanced,     // Default for modern Android: 1.0x native render scale, 2x MSAA, 30m shadows (1 cascade), 60 FPS
        MobileHigh,         // 1.0x native, 4x MSAA, 45m shadows (2 cascades), full resolution
        MobilePerformance   // 0.80x internal render scale, 0x MSAA / 2x MSAA, 22m shadows, optimized for low-end / thermal stability
    }

    /// <summary>
    /// Manages authoritative mobile visual resolution and performance quality tiers for Android Portrait.
    /// Ensures 1080p and 1440p displays maintain crystal clear 3D rendering and sharp UI without blurry downsampling.
    /// Default tier is MobileBalanced to eliminate thermal throttling and frame spikes while preserving full 1080p native clarity.
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
        private MobileQualityTier currentTier = MobileQualityTier.MobileBalanced;

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
            int saved = PlayerPrefs.GetInt(PrefsQualityTierKey, (int)MobileQualityTier.MobileBalanced);
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
            MobileQualityTier nextTier = currentTier switch
            {
                MobileQualityTier.MobileBalanced => MobileQualityTier.MobileHigh,
                MobileQualityTier.MobileHigh => MobileQualityTier.MobilePerformance,
                _ => MobileQualityTier.MobileBalanced
            };
            SetQualityTier(nextTier);
        }

        private void ApplyQualityTier(MobileQualityTier tier, bool notify)
        {
            // Target rock-solid 60 FPS on Android mobile
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0; // Disable vsync so 60 FPS cap is authoritative

            switch (tier)
            {
                case MobileQualityTier.MobileHigh:
                    // MOBILE HIGH: 4x MSAA, 45m shadows (2 cascades), 1.0x native
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.shadowDistance = 45f;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.globalTextureMipmapLimit = 0;
                    ScalableBufferManager.ResizeBuffers(1.0f, 1.0f);
                    break;

                case MobileQualityTier.MobilePerformance:
                    // MOBILE PERFORMANCE: 0.80x render scale, 22m shadows, no cascades
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.shadowDistance = 22f;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.globalTextureMipmapLimit = 0;
                    ScalableBufferManager.ResizeBuffers(0.80f, 0.80f);
                    break;

                case MobileQualityTier.MobileBalanced:
                default:
                    // MOBILE BALANCED (DEFAULT): 1.0x full native render, 2x MSAA, 30m shadows (1 cascade)
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.shadowDistance = 30f;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.globalTextureMipmapLimit = 0;
                    ScalableBufferManager.ResizeBuffers(1.0f, 1.0f);
                    break;
            }

            if (notify)
            {
                OnQualityTierChanged?.Invoke(currentTier);
            }
        }
    }
}
