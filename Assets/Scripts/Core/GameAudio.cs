using System;
using UnityEngine;

namespace Emberfall
{
    public enum SoundCue { Attack, Cast, Hit, Dodge, Loot, LevelUp, Victory, Death }

    /// <summary>Small synthesized mono cues, bounded to eight simultaneous voices.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private const int CueCount = 8;
        private const int SampleRate = 22050;
        private const float DefaultVolume = .16f;
        private const double Tau = Math.PI * 2.0;
        private static readonly float[] Durations = { .10f, .27f, .09f, .19f, .38f, .64f, .95f, .65f };
        private static readonly float[] MinimumIntervals = { .075f, .12f, .075f, .15f, .16f, .25f, .5f, .5f };
        private static GameAudio instance;
        private static bool muted;
        private static bool quitting;
        private AudioSource[] voices;
        private AudioClip[] clips;
        private float[] lastPlayed;
        private int nextVoice;

        public static bool Muted
        {
            get { return muted; }
            set
            {
                muted = value;
                if (muted && instance != null) instance.StopVoices();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // With domain reload disabled, Play can rediscover any surviving scene instance.
            if (instance != null)
            {
                instance.StopVoices();
                instance.ResetThrottle();
            }
            instance = null;
            muted = false;
            quitting = false;
        }

        public static void Play(SoundCue cue)
        {
            int index = (int)cue;
            if (muted || quitting || index < 0 || index >= CueCount || !Application.isPlaying) return;
            if (instance == null)
            {
#if UNITY_6000_0_OR_NEWER
                instance = FindAnyObjectByType<GameAudio>();
#else
                instance = FindObjectOfType<GameAudio>();
#endif
                if (instance == null)
                {
                    GameObject root = new GameObject("Emberfall Audio");
                    instance = root.AddComponent<GameAudio>();
                }
            }
            instance.PlayInternal(index);
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsurePool();
        }

        private void EnsurePool()
        {
            if (voices != null) return;
            voices = new AudioSource[VoiceCount];
            clips = new AudioClip[CueCount];
            lastPlayed = new float[CueCount];
            for (int i = 0; i < CueCount; i++) lastPlayed[i] = float.NegativeInfinity;
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
                voice.dopplerLevel = 0f;
                voice.volume = DefaultVolume;
                voices[i] = voice;
            }
        }

        private void PlayInternal(int cue)
        {
            EnsurePool();
            float now = Time.unscaledTime;
            if (now - lastPlayed[cue] < MinimumIntervals[cue]) return;
            AudioSource voice = null;
            for (int offset = 0; offset < VoiceCount; offset++)
            {
                int candidate = (nextVoice + offset) % VoiceCount;
                if (voices[candidate] != null && !voices[candidate].isPlaying)
                {
                    voice = voices[candidate];
                    nextVoice = (candidate + 1) % VoiceCount;
                    break;
                }
            }
            // Never stack additional one-shots on a busy source: the actual voice cap stays eight.
            if (voice == null) return;
            if (clips[cue] == null) clips[cue] = Synthesize((SoundCue)cue);
            lastPlayed[cue] = now;
            voice.PlayOneShot(clips[cue]);
        }

        private static AudioClip Synthesize(SoundCue cue)
        {
            float duration = Durations[(int)cue];
            int length = Math.Max(1, (int)(duration * SampleRate));
            var samples = new float[length];
            uint noiseState = 0x6d2b79f5u + (uint)cue * 997u;
            double phase = 0;
            double smoothedNoise = 0;
            float peak = .001f;
            for (int i = 0; i < samples.Length; i++)
            {
                double time = i / (double)SampleRate;
                double progress = time / duration;
                noiseState ^= noiseState << 13;
                noiseState ^= noiseState >> 17;
                noiseState ^= noiseState << 5;
                double noise = (noiseState / (double)uint.MaxValue) * 2 - 1;
                smoothedNoise += (noise - smoothedNoise) * .18;
                double frequency;
                double value;
                switch (cue)
                {
                    case SoundCue.Attack:
                        frequency = 450 - 300 * progress;
                        phase += Tau * frequency / SampleRate;
                        value = (Math.Sin(phase) * .28 + noise * .55) * Math.Exp(-progress * 4);
                        break;
                    case SoundCue.Cast:
                        frequency = 350 + progress * 850;
                        phase += Tau * frequency / SampleRate;
                        value = (Math.Sin(phase) + Math.Sin(phase * 1.5) * .25) * Math.Sin(Math.PI * progress) * .55;
                        break;
                    case SoundCue.Hit:
                        frequency = 145 - progress * 75;
                        phase += Tau * frequency / SampleRate;
                        value = (Math.Sin(phase) * .7 + noise * .45) * Math.Exp(-progress * 5);
                        break;
                    case SoundCue.Dodge:
                        value = (noise - smoothedNoise) * Math.Sin(Math.PI * progress) * Math.Exp(-progress * 1.5);
                        break;
                    case SoundCue.Loot:
                        value = Bell(time, 740) + Bell(time - .10, 1110) * .8;
                        break;
                    case SoundCue.LevelUp:
                        value = Bell(time, 523.25) + Bell(time - .10, 659.25) + Bell(time - .20, 783.99) + Bell(time - .30, 1046.5);
                        break;
                    case SoundCue.Victory:
                        value = Bell(time, 523.25) * .65 + Bell(time - .10, 659.25) * .65 + Bell(time - .20, 783.99) * .65;
                        value += Bell(time - .34, 523.25) + Bell(time - .34, 659.25) * .7 + Bell(time - .34, 1046.5) * .7;
                        break;
                    default:
                        frequency = 230 - progress * 155;
                        phase += Tau * frequency / SampleRate;
                        value = (Math.Sin(phase) + Math.Sin(phase * .5) * .4) * Math.Exp(-progress * 2.2);
                        break;
                }
                // Short ramps eliminate discontinuities at both ends of every clip.
                double envelope = Math.Min(1, time / .004) * Math.Min(1, (duration - time) / .025);
                samples[i] = (float)(value * Math.Max(0, envelope));
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }
            float normalizer = .8f / peak;
            for (int i = 0; i < samples.Length; i++) samples[i] *= normalizer;
            AudioClip clip = AudioClip.Create("Emberfall " + cue, length, 1, SampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private static double Bell(double time, double frequency)
        {
            if (time < 0) return 0;
            double attack = Math.Min(1, time / .007);
            return (Math.Sin(Tau * frequency * time) + .18 * Math.Sin(Tau * frequency * 2 * time)) * Math.Exp(-time * 8.5) * attack;
        }

        private void StopVoices()
        {
            if (voices == null) return;
            foreach (AudioSource voice in voices) if (voice != null) voice.Stop();
        }

        private void ResetThrottle()
        {
            if (lastPlayed == null) return;
            for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = float.NegativeInfinity;
        }

        private void ReleaseClips()
        {
            if (clips == null) return;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null) Destroy(clips[i]);
                clips[i] = null;
            }
        }

        private void OnEnable() { ResetThrottle(); }

        private void OnDisable()
        {
            // Unity invokes OnDisable before a script/domain reload as well as on scene teardown.
            StopVoices();
            ReleaseClips();
        }

        private void OnApplicationQuit() { quitting = true; }

        private void OnDestroy()
        {
            StopVoices();
            ReleaseClips();
            if (instance == this) instance = null;
        }
    }
}
