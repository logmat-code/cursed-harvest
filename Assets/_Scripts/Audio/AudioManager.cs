using System.Collections.Generic;
using UnityEngine;

namespace CursedHarvest
{
    public enum SoundType
    {
        CrossbowShoot,
        BombExplode,
        ScytheHit,
        FireTick,
        PlayerHit,
        PlayerDash,
        EnemyDeath,
        PickupXP,
        PickupSeed,
        LevelUp,
        LuckyLast,
        HarvestLucky,
        HarvestCursed,
        BossSlam,
        ButtonClick
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Settings")]
        [SerializeField] private float sfxVolume = 0.85f;
        [SerializeField] private float musicVolume = 0.35f;
        [SerializeField] private int maxSfxChannels = 16;

        private AudioSource[] sfxChannels;
        private int channelIndex;
        private AudioSource musicChannel;

        private readonly Dictionary<SoundType, AudioClip> clips = new Dictionary<SoundType, AudioClip>();
        private AudioClip musicClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioChannels();
            GenerateClips();
            StartAmbientMusic();
        }

        private void InitializeAudioChannels()
        {
            sfxChannels = new AudioSource[maxSfxChannels];
            for (int i = 0; i < maxSfxChannels; i++)
            {
                GameObject child = new GameObject($"SFX_Channel_{i}");
                child.transform.SetParent(transform);
                AudioSource src = child.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f; // 2D crisp UI/action audio
                sfxChannels[i] = src;
            }

            GameObject musicChild = new GameObject("Music_Channel");
            musicChild.transform.SetParent(transform);
            musicChannel = musicChild.AddComponent<AudioSource>();
            musicChannel.playOnAwake = false;
            musicChannel.spatialBlend = 0f;
            musicChannel.loop = true;
            musicChannel.volume = musicVolume;
        }

        private void GenerateClips()
        {
            clips[SoundType.CrossbowShoot] = SoundFXGenerator.GenerateCrossbowShot();
            clips[SoundType.BombExplode] = SoundFXGenerator.GenerateBombExplode();
            clips[SoundType.ScytheHit] = SoundFXGenerator.GenerateScytheHit();
            clips[SoundType.FireTick] = SoundFXGenerator.GenerateFireTick();
            clips[SoundType.PlayerHit] = SoundFXGenerator.GeneratePlayerHit();
            clips[SoundType.PlayerDash] = SoundFXGenerator.GeneratePlayerDash();
            clips[SoundType.EnemyDeath] = SoundFXGenerator.GenerateEnemyDeath();
            clips[SoundType.PickupXP] = SoundFXGenerator.GeneratePickupXP();
            clips[SoundType.PickupSeed] = SoundFXGenerator.GeneratePickupSeed();
            clips[SoundType.LevelUp] = SoundFXGenerator.GenerateLevelUp();
            clips[SoundType.LuckyLast] = SoundFXGenerator.GenerateLuckyLast();
            clips[SoundType.HarvestLucky] = SoundFXGenerator.GenerateHarvestLucky();
            clips[SoundType.HarvestCursed] = SoundFXGenerator.GenerateHarvestCursed();
            clips[SoundType.BossSlam] = SoundFXGenerator.GenerateBossSlam();
            clips[SoundType.ButtonClick] = SoundFXGenerator.GenerateButtonClick();

            musicClip = SoundFXGenerator.GenerateAmbientSpookyDrone();
        }

        public void StartAmbientMusic()
        {
            if (musicChannel != null && musicClip != null)
            {
                musicChannel.clip = musicClip;
                musicChannel.volume = musicVolume;
                musicChannel.Play();
            }
        }

        public void PlaySound(SoundType type, float volumeMultiplier = 1f, float pitchVariation = 0.08f)
        {
            if (!clips.TryGetValue(type, out AudioClip clip) || clip == null) return;
            if (sfxChannels == null || sfxChannels.Length == 0) return;

            AudioSource source = sfxChannels[channelIndex];
            channelIndex = (channelIndex + 1) % sfxChannels.Length;

            float pitch = 1f;
            if (pitchVariation > 0f)
            {
                pitch += Random.Range(-pitchVariation, pitchVariation);
            }

            source.pitch = pitch;
            source.PlayOneShot(clip, sfxVolume * volumeMultiplier);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicChannel != null)
            {
                musicChannel.volume = musicVolume;
            }
        }
    }
}
