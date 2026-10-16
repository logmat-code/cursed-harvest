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
        EnemyHit,
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
        [SerializeField] private float musicVolume = 0.22f;
        [SerializeField] private float droneVolume = 0.04f;
        [SerializeField] private int maxSfxChannels = 16;

        [Header("Music Tracks")]
        [SerializeField] private AudioClip gameplayMusicClip;
        [SerializeField] private AudioClip bossMusicClip;

        private AudioSource[] sfxChannels;
        private int channelIndex;
        private AudioSource musicChannel;
        private AudioSource droneChannel;
        private Coroutine musicFadeCoroutine;

        private readonly Dictionary<SoundType, AudioClip> clips = new Dictionary<SoundType, AudioClip>();
        private AudioClip proceduralDroneClip;

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
            LoadMusicClipsIfMissing();
        }

        private void Start()
        {
            // Subscribe to game events for automatic music transitions
            BossController.OnBossSpawned += HandleBossSpawned;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null) ph.OnPlayerDied += HandlePlayerDied;
            }

            PlayGameplayMusic(1.0f);
            StartAmbientDrone();
        }

        private void OnDestroy()
        {
            BossController.OnBossSpawned -= HandleBossSpawned;
        }

        private void HandleBossSpawned(BossController boss)
        {
            PlayBossMusic(1.5f);
        }

        private void HandlePlayerDied()
        {
            StopMusic(2.0f);
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

            GameObject droneChild = new GameObject("Drone_Channel");
            droneChild.transform.SetParent(transform);
            droneChannel = droneChild.AddComponent<AudioSource>();
            droneChannel.playOnAwake = false;
            droneChannel.spatialBlend = 0f;
            droneChannel.loop = true;
            droneChannel.volume = droneVolume;
        }

        private void LoadMusicClipsIfMissing()
        {
#if UNITY_EDITOR
            if (gameplayMusicClip == null)
            {
                gameplayMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Audio/Music/Echoes_of_Time_v2.mp3");
            }
            if (bossMusicClip == null)
            {
                bossMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Audio/Music/Grave_Blow.mp3");
            }
#endif
        }

        private void GenerateClips()
        {
            clips[SoundType.CrossbowShoot] = SoundFXGenerator.GenerateCrossbowShot();
            clips[SoundType.BombExplode] = SoundFXGenerator.GenerateBombExplode();
            clips[SoundType.ScytheHit] = SoundFXGenerator.GenerateScytheHit();
            clips[SoundType.FireTick] = SoundFXGenerator.GenerateFireTick();
            clips[SoundType.PlayerHit] = SoundFXGenerator.GeneratePlayerHit();
            clips[SoundType.PlayerDash] = SoundFXGenerator.GeneratePlayerDash();
            clips[SoundType.EnemyHit] = SoundFXGenerator.GenerateEnemyHit();
            clips[SoundType.EnemyDeath] = SoundFXGenerator.GenerateEnemyDeath();
            clips[SoundType.PickupXP] = SoundFXGenerator.GeneratePickupXP();
            clips[SoundType.PickupSeed] = SoundFXGenerator.GeneratePickupSeed();
            clips[SoundType.LevelUp] = SoundFXGenerator.GenerateLevelUp();
            clips[SoundType.LuckyLast] = SoundFXGenerator.GenerateLuckyLast();
            clips[SoundType.HarvestLucky] = SoundFXGenerator.GenerateHarvestLucky();
            clips[SoundType.HarvestCursed] = SoundFXGenerator.GenerateHarvestCursed();
            clips[SoundType.BossSlam] = SoundFXGenerator.GenerateBossSlam();
            clips[SoundType.ButtonClick] = SoundFXGenerator.GenerateButtonClick();

            proceduralDroneClip = SoundFXGenerator.GenerateAmbientSpookyDrone();
        }

        public void PlayGameplayMusic(float fadeDuration = 1.0f)
        {
            if (gameplayMusicClip == null)
            {
                LoadMusicClipsIfMissing();
            }
            if (gameplayMusicClip != null)
            {
                PlayTrack(gameplayMusicClip, fadeDuration);
            }
        }

        public void PlayBossMusic(float fadeDuration = 1.5f)
        {
            if (bossMusicClip == null)
            {
                LoadMusicClipsIfMissing();
            }
            if (bossMusicClip != null)
            {
                PlayTrack(bossMusicClip, fadeDuration);
            }
        }

        public void StopMusic(float fadeDuration = 1.0f)
        {
            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeOutRoutine(fadeDuration));
        }

        private void PlayTrack(AudioClip targetClip, float fadeDuration)
        {
            if (musicChannel == null) return;
            if (musicChannel.clip == targetClip && musicChannel.isPlaying) return;

            if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(CrossfadeTrackRoutine(targetClip, fadeDuration));
        }

        private System.Collections.IEnumerator CrossfadeTrackRoutine(AudioClip newClip, float duration)
        {
            float startVol = musicChannel.isPlaying ? musicChannel.volume : 0f;
            float halfDuration = Mathf.Max(0.05f, duration * 0.5f);

            if (musicChannel.isPlaying)
            {
                float t = 0f;
                while (t < halfDuration)
                {
                    t += Time.unscaledDeltaTime;
                    musicChannel.volume = Mathf.Lerp(startVol, 0f, t / halfDuration);
                    yield return null;
                }
            }

            musicChannel.Stop();
            musicChannel.clip = newClip;
            musicChannel.Play();

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicChannel.volume = Mathf.Lerp(0f, musicVolume, elapsed / halfDuration);
                yield return null;
            }
            musicChannel.volume = musicVolume;
            musicFadeCoroutine = null;
        }

        private System.Collections.IEnumerator FadeOutRoutine(float duration)
        {
            float startVol = musicChannel.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                musicChannel.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            musicChannel.Stop();
            musicChannel.volume = musicVolume;
            musicFadeCoroutine = null;
        }

        public void StartAmbientDrone()
        {
            if (droneChannel != null && proceduralDroneClip != null)
            {
                droneChannel.clip = proceduralDroneClip;
                droneChannel.volume = droneVolume;
                droneChannel.Play();
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
