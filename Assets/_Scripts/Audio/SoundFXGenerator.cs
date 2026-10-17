using System;
using UnityEngine;

namespace CursedHarvest
{
    public static class SoundFXGenerator
    {
        private const int SampleRate = 44100;

        public static AudioClip GenerateCrossbowShot()
        {
            float duration = 0.13f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Pitch sweep down: 880Hz -> 200Hz
                float freq = Mathf.Lerp(880f, 200f, progress);
                float phase = 2f * Mathf.PI * freq * t;
                float tone = Mathf.Sin(phase);

                // Noise snap
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Max(0f, 1f - progress * 4f);

                // Exponential decay envelope
                float envelope = Mathf.Exp(-16f * progress);
                samples[i] = (tone * 0.7f + noise * 0.3f) * envelope * 0.75f;
            }

            AudioClip clip = AudioClip.Create("SFX_Crossbow", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateEnemyHit()
        {
            float duration = 0.09f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Thud tone: 320Hz -> 80Hz
                float freq = Mathf.Lerp(320f, 80f, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Quick grit
                float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Max(0f, 1f - progress * 3f);

                float envelope = Mathf.Exp(-20f * progress);
                samples[i] = (tone * 0.65f + noise * 0.35f) * envelope * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_EnemyHit", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateBombExplode()
        {
            float duration = 0.5f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Low bass drop: 90Hz -> 30Hz
                float bassFreq = Mathf.Lerp(90f, 30f, progress);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t);

                // Crunchy noise
                float noise = (UnityEngine.Random.value * 2f - 1f);

                // Fast attack, heavy punch decay
                float envelope = Mathf.Exp(-7f * progress);
                samples[i] = Mathf.Clamp((bass * 0.6f + noise * 0.4f) * envelope * 0.9f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_BombExplode", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateScytheHit()
        {
            float duration = 0.16f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Metallic combination
                float tone = Mathf.Sin(2f * Mathf.PI * 620f * t) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * 1240f * t) * 0.3f +
                             Mathf.Sin(2f * Mathf.PI * 1860f * t) * 0.2f;

                float envelope = Mathf.Exp(-14f * progress);
                samples[i] = tone * envelope * 0.7f;
            }

            AudioClip clip = AudioClip.Create("SFX_ScytheHit", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateFireTick()
        {
            float duration = 0.07f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float progress = (float)i / totalSamples;
                float noise = (UnityEngine.Random.value * 2f - 1f);
                float env = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.5f);
                samples[i] = noise * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("SFX_FireTick", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GeneratePlayerHit()
        {
            float duration = 0.18f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Low thud
                float freq = Mathf.Lerp(260f, 70f, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.3f;
                float env = Mathf.Exp(-12f * progress);

                samples[i] = (tone * 0.7f + noise) * env * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_PlayerHit", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GeneratePlayerDash()
        {
            float duration = 0.22f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Whoosh curve: quick rise, soft fall
                float whooshEnv = Mathf.Sin(progress * Mathf.PI);
                float noise = (UnityEngine.Random.value * 2f - 1f);
                float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 550f, progress) * t) * 0.3f;

                samples[i] = (noise * 0.7f + tone) * whooshEnv * 0.6f;
            }

            AudioClip clip = AudioClip.Create("SFX_Dash", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateEnemyDeath()
        {
            float duration = 0.18f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                float freq = Mathf.Lerp(180f, 40f, progress);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                float env = Mathf.Exp(-10f * progress);

                samples[i] = (tone + noise) * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SFX_EnemyDeath", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GeneratePickupXP()
        {
            float duration = 0.12f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // High crystal bell: 1318Hz (E6)
                float tone = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.7f +
                             Mathf.Sin(2f * Mathf.PI * 2637f * t) * 0.3f;
                float env = Mathf.Exp(-14f * progress);

                samples[i] = tone * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SFX_PickupXP", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GeneratePickupSeed()
        {
            float duration = 0.22f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Double tone: 880Hz then 1320Hz
                float freq = progress < 0.45f ? 880f : 1320f;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float env = Mathf.Exp(-8f * (progress < 0.45f ? progress : progress - 0.45f));

                samples[i] = tone * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SFX_PickupSeed", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateLevelUp()
        {
            float duration = 0.65f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }; // C5, E5, G5, C6
            float noteDuration = duration / notes.Length;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Clamp((int)(t / noteDuration), 0, notes.Length - 1);
                float noteT = t - noteIndex * noteDuration;
                float noteProgress = noteT / noteDuration;

                float freq = notes[noteIndex];
                float tone = Mathf.Sin(2f * Mathf.PI * freq * noteT) * 0.7f +
                             Mathf.Sin(4f * Mathf.PI * freq * noteT) * 0.3f;
                float env = Mathf.Exp(-7f * noteProgress);

                samples[i] = tone * env * 0.75f;
            }

            AudioClip clip = AudioClip.Create("SFX_LevelUp", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateLuckyLast()
        {
            float duration = 1.2f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Rising powerup sweep
                float sweepFreq = Mathf.Lerp(120f, 960f, Mathf.Pow(progress, 1.8f));
                float sweep = Mathf.Sin(2f * Mathf.PI * sweepFreq * t);

                // Deep resonant drone
                float sub = Mathf.Sin(2f * Mathf.PI * 65.4f * t);

                // Distortion grit
                float mixed = sweep * 0.6f + sub * 0.4f;
                float distorted = Mathf.Clamp(mixed * 2.2f, -1f, 1f);

                // Shimmer vibrato
                float vibrato = 1f + 0.2f * Mathf.Sin(2f * Mathf.PI * 14f * t);

                // Swell then ring
                float env = progress < 0.3f ? (progress / 0.3f) : Mathf.Exp(-3.5f * (progress - 0.3f));

                samples[i] = distorted * vibrato * env * 0.85f;
            }

            AudioClip clip = AudioClip.Create("SFX_LuckyLast", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateHarvestLucky()
        {
            float duration = 0.55f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            // Triumphant major chord (E5 659Hz, G#5 830Hz, B5 987Hz)
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                float chord = (Mathf.Sin(2f * Mathf.PI * 659.25f * t) +
                               Mathf.Sin(2f * Mathf.PI * 830.61f * t) +
                               Mathf.Sin(2f * Mathf.PI * 987.77f * t)) / 3f;

                float env = Mathf.Exp(-6f * progress);
                samples[i] = chord * env * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_HarvestLucky", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateHarvestCursed()
        {
            float duration = 0.75f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Ominous dissonant tritone (311Hz and 440Hz with beating)
                float f1 = Mathf.Lerp(311.1f, 220f, progress);
                float f2 = Mathf.Lerp(440.0f, 311f, progress);
                float tone = (Mathf.Sin(2f * Mathf.PI * f1 * t) + Mathf.Sin(2f * Mathf.PI * f2 * t)) * 0.5f;

                float env = Mathf.Exp(-4.5f * progress);
                samples[i] = tone * env * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SFX_HarvestCursed", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateBossSlam()
        {
            float duration = 0.65f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Sub-rumble: 60Hz -> 20Hz
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(60f, 20f, progress) * t);
                float rumble = (UnityEngine.Random.value * 2f - 1f) * 0.5f;
                float env = Mathf.Exp(-5f * progress);

                samples[i] = Mathf.Clamp((sub * 0.7f + rumble * 0.3f) * env * 0.95f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_BossSlam", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateButtonClick()
        {
            float duration = 0.04f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                float tone = Mathf.Sin(2f * Mathf.PI * 1200f * t);
                float env = Mathf.Exp(-30f * progress);
                samples[i] = tone * env * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SFX_ButtonClick", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateAmbientSpookyDrone()
        {
            // 4-second seamless ambient drone loop
            float duration = 4.0f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;

                // Slow atmospheric Halloween minor harmony: C2 (65.4Hz), Eb2 (77.78Hz), G2 (98Hz)
                float d1 = Mathf.Sin(2f * Mathf.PI * 65.41f * t);
                float d2 = Mathf.Sin(2f * Mathf.PI * 77.78f * t + 0.5f);
                float d3 = Mathf.Sin(2f * Mathf.PI * 98.00f * t + 1.2f);

                // Subtle slow LFO beating
                float lfo = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);

                samples[i] = (d1 * 0.45f + d2 * 0.35f + d3 * 0.2f) * lfo * 0.28f;
            }

            AudioClip clip = AudioClip.Create("Music_SpookyDrone", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip GenerateHeal()
        {
            float duration = 0.24f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = t / duration;

                // Ascending harmonic chime: C5 (523Hz) -> E5 (659Hz) -> G5 (784Hz)
                float freq = Mathf.Lerp(523.25f, 783.99f, Mathf.Pow(progress, 0.75f));
                float tone1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * freq * 1.5f * t) * 0.35f; // Pure 5th harmonic sparkle

                // Smooth bell envelope: quick clean attack, smooth exponential decay
                float attack = Mathf.Clamp01(t / 0.012f);
                float decay = Mathf.Exp(-8.5f * progress);
                samples[i] = (tone1 + tone2) * attack * decay * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SFX_Heal", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
