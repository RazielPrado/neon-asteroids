using System.Collections.Generic;
using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    public class NeonAudio : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] AudioClip[] musicTracks;
        [SerializeField] bool shuffle = true;
        [SerializeField] float crossfadeSeconds = 4f;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.45f;
        [SerializeField] float musicFadeIn = 2.5f;

        [SerializeField, HideInInspector] AudioClip music;
        [SerializeField] float pausedMusicCutoff = 900f;

        [Header("Sound Effects")]
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.7f;

        [Header("Custom Clips")]
        [SerializeField] AudioClip[] shootClips;
        [SerializeField] AudioClip[] smallExplosionClips;
        [SerializeField] AudioClip[] bigExplosionClips;
        [SerializeField] AudioClip deathClip;
        [SerializeField] AudioClip hyperspaceClip;
        [SerializeField] AudioClip waveStartClip;
        [SerializeField] AudioClip extraLifeClip;

        [Header("Menu Sounds")]
        [SerializeField] AudioClip menuMoveClip;
        [SerializeField] AudioClip menuConfirmClip;
        [SerializeField, Range(0f, 1f)] float uiVolume = 0.7f;

        const int VoiceCount = 16;
        const int SampleRate = 44100;

        AudioSource uiSource;

        readonly AudioSource[] musicSources = new AudioSource[2];
        readonly AudioLowPassFilter[] musicFilters = new AudioLowPassFilter[2];
        readonly float[] musicGain = new float[2];
        readonly float[] musicGainTarget = new float[2];
        readonly float[] musicGainSpeed = new float[2];
        int activeMusic;
        readonly List<AudioClip> playlist = new List<AudioClip>();
        readonly List<int> playOrder = new List<int>();
        int playOrderPos, lastTrackIndex = -1;
        readonly List<AudioSource> voices = new List<AudioSource>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        int nextVoice;
        bool paused;
        float pauseDuck = 1f;
        bool appFocused = true;
        float focusGrace, lastMusicTime;

        AudioClip[] synthShoot, synthExplosionS, synthExplosionM, synthExplosionL;
        AudioClip synthDeath, synthHyper, synthWave, synthExtraLife, synthMenuMove, synthMenuConfirm;
        System.Random rng;

        void Awake()
        {
            rng = new System.Random(1337);

            for (int i = 0; i < 2; i++)
            {
                var musicGO = new GameObject($"Music_{(char)('A' + i)}");
                musicGO.transform.SetParent(transform, false);
                var src = musicGO.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.volume = 0f;
                musicSources[i] = src;

                var filter = musicGO.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = 22000f;
                musicFilters[i] = filter;
            }

            var sfxGO = new GameObject("SFX");
            sfxGO.transform.SetParent(transform, false);
            for (int i = 0; i < VoiceCount; i++)
            {
                var s = sfxGO.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                voices.Add(s);
            }

            uiSource = sfxGO.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;
            uiSource.ignoreListenerPause = true;

            BuildSynthClips();

            if (musicTracks != null)
                foreach (var clip in musicTracks)
                    if (clip != null) playlist.Add(clip);
            if (playlist.Count == 0 && music != null) playlist.Add(music);

            if (playlist.Count > 0) StartTrack(musicFadeIn);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdatePlaylist();

            pauseDuck = Mathf.MoveTowards(pauseDuck, paused ? 0.6f : 1f, dt * 2f);
            float cutoff = paused ? pausedMusicCutoff : 22000f;
            float filterLerp = 1f - Mathf.Exp(-dt * 8f);

            for (int i = 0; i < 2; i++)
            {
                musicGain[i] = Mathf.MoveTowards(musicGain[i], musicGainTarget[i], musicGainSpeed[i] * dt);
                musicSources[i].volume = musicVolume * musicGain[i] * pauseDuck;

                if (i != activeMusic && musicGainTarget[i] <= 0f && musicGain[i] <= 0f && musicSources[i].clip != null)
                {
                    musicSources[i].Stop();
                    musicSources[i].clip = null;
                }

                musicFilters[i].cutoffFrequency = Mathf.Lerp(musicFilters[i].cutoffFrequency, cutoff, filterLerp);
            }
        }

        void UpdatePlaylist()
        {
            if (playlist.Count == 0) return;

            var current = musicSources[activeMusic];
            if (current.clip == null) return;

            if (playlist.Count == 1)
            {
                current.loop = true;
                return;
            }

            if (!appFocused) return;
            if (focusGrace > 0f)
            {
                focusGrace -= Time.unscaledDeltaTime;
                lastMusicTime = current.time;
                return;
            }

            float fade = SafeCrossfade(current.clip);
            float remaining = current.clip.length - current.time;

            if (current.isPlaying && remaining <= fade)
            {
                StartTrack(fade);
            }
            else if (!current.isPlaying && lastMusicTime >= current.clip.length - Mathf.Max(fade, 1f))
            {
                StartTrack(fade);
            }

            lastMusicTime = current.time;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            appFocused = hasFocus;
            if (hasFocus) focusGrace = 0.75f;
        }

        void OnApplicationPause(bool isPaused)
        {
            if (!isPaused) focusGrace = 0.75f;
        }

        void StartTrack(float fadeSeconds)
        {
            AudioClip next = playlist[NextTrackIndex()];
            fadeSeconds = Mathf.Max(0.05f, fadeSeconds);

            int outgoing = activeMusic;
            activeMusic = 1 - activeMusic;
            var src = musicSources[activeMusic];

            src.clip = next;
            src.time = 0f;
            src.loop = false;
            src.Play();
            musicGain[activeMusic] = 0f;
            musicGainTarget[activeMusic] = 1f;
            musicGainSpeed[activeMusic] = 1f / fadeSeconds;

            musicGainTarget[outgoing] = 0f;
            musicGainSpeed[outgoing] = 1f / fadeSeconds;
            lastMusicTime = 0f;
        }

        float SafeCrossfade(AudioClip clip) => Mathf.Min(crossfadeSeconds, clip.length * 0.5f);

        int NextTrackIndex()
        {
            int count = playlist.Count;
            if (count == 1) return 0;

            if (!shuffle)
            {
                lastTrackIndex = (lastTrackIndex + 1) % count;
                return lastTrackIndex;
            }

            if (playOrderPos >= playOrder.Count)
            {
                playOrder.Clear();
                for (int i = 0; i < count; i++) playOrder.Add(i);
                for (int i = count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (playOrder[i], playOrder[j]) = (playOrder[j], playOrder[i]);
                }
                if (playOrder[0] == lastTrackIndex)
                    (playOrder[0], playOrder[count - 1]) = (playOrder[count - 1], playOrder[0]);
                playOrderPos = 0;
            }

            lastTrackIndex = playOrder[playOrderPos++];
            return lastTrackIndex;
        }

        public void PlayShoot() =>
            Play("shoot", Pick(shootClips, synthShoot), 0.32f, 0.92f, 1.08f, 0.05f);

        public void PlayExplosion(int size)
        {
            AudioClip clip = size >= 3
                ? Pick(bigExplosionClips, synthExplosionL)
                : size == 2 ? Pick(bigExplosionClips, synthExplosionM) : Pick(smallExplosionClips, synthExplosionS);

            float vol = size >= 3 ? 0.85f : size == 2 ? 0.65f : 0.5f;
            Play("explosion" + size, clip, vol, 0.9f, 1.1f, 0.03f);
        }

        public void PlayDeath() => Play("death", deathClip != null ? deathClip : synthDeath, 1f, 0.97f, 1.03f, 0.2f);
        public void PlayHyperspace() => Play("hyper", hyperspaceClip != null ? hyperspaceClip : synthHyper, 0.55f, 0.95f, 1.05f, 0.1f);
        public void PlayWaveStart() => Play("wave", waveStartClip != null ? waveStartClip : synthWave, 0.5f, 1f, 1f, 0.3f);
        public void PlayExtraLife() => Play("life", extraLifeClip != null ? extraLifeClip : synthExtraLife, 0.45f, 1f, 1f, 0.3f);

        public void PlayMenuMove() => PlayUI(menuMoveClip != null ? menuMoveClip : synthMenuMove, 0.6f);
        public void PlayMenuConfirm() => PlayUI(menuConfirmClip != null ? menuConfirmClip : synthMenuConfirm, 0.8f);

        void PlayUI(AudioClip clip, float volume)
        {
            if (clip == null) return;
            uiSource.PlayOneShot(clip, volume * uiVolume);
        }

        public void SetPaused(bool value)
        {
            paused = value;
            foreach (var v in voices)
            {
                if (paused) v.Pause();
                else v.UnPause();
            }
        }

        AudioClip Pick(AudioClip[] custom, AudioClip[] synth)
        {
            var pool = custom != null && custom.Length > 0 ? custom : synth;
            return pool[Random.Range(0, pool.Length)];
        }

        void Play(string key, AudioClip clip, float volume, float pitchMin, float pitchMax, float minInterval)
        {
            if (clip == null || paused) return;

            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(key, out float last) && now - last < minInterval) return;
            lastPlayed[key] = now;

            AudioSource voice = null;
            for (int i = 0; i < voices.Count; i++)
            {
                var v = voices[(nextVoice + i) % voices.Count];
                if (!v.isPlaying) { voice = v; break; }
            }
            if (voice == null) voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Count;

            voice.clip = clip;
            voice.pitch = Random.Range(pitchMin, pitchMax);
            voice.volume = volume * sfxVolume * Random.Range(0.9f, 1f);
            voice.Play();
        }

        void BuildSynthClips()
        {
            synthShoot = new[] { SynthShoot(0), SynthShoot(1), SynthShoot(2) };
            synthExplosionS = new[] { SynthExplosion(1, 0), SynthExplosion(1, 1) };
            synthExplosionM = new[] { SynthExplosion(2, 0), SynthExplosion(2, 1) };
            synthExplosionL = new[] { SynthExplosion(3, 0), SynthExplosion(3, 1) };
            synthDeath = SynthDeath();
            synthHyper = SynthHyperspace();
            synthWave = SynthWaveSwell();
            synthExtraLife = SynthExtraLife();
            synthMenuMove = SynthBlip(660f, 0.045f, false);
            synthMenuConfirm = SynthBlip(880f, 0.12f, true);
        }

        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static float Triangle(float phase) => 4f * Mathf.Abs(Mathf.Repeat(phase, 1f) - 0.5f) - 1f;

        static AudioClip ToClip(string name, float[] data, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            float k = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= k;

            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioClip SynthShoot(int variant)
        {
            float dur = 0.1f + variant * 0.01f;
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float f0 = 950f + variant * 110f, f1 = 240f;
            float phase = 0f, lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / dur;
                float f = f0 * Mathf.Pow(f1 / f0, k);
                phase += f / SampleRate;

                float s = Triangle(phase) * 0.6f + Mathf.Sin(phase * Mathf.PI) * 0.4f;
                float env = Mathf.Exp(-k * 5f) * Mathf.Min(1f, t * 400f);
                lp += (s * env - lp) * 0.3f;
                data[i] = lp;
            }
            return ToClip($"sfx_shoot_{variant}", data, 0.7f);
        }

        AudioClip SynthExplosion(int size, int variant)
        {
            float s01 = (size - 1) / 2f;
            float dur = 0.16f + 0.14f * size + variant * 0.03f;
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];

            float holdStart = Mathf.Lerp(6500f, 2800f, s01) * (1f + variant * 0.12f);
            float holdEnd = Mathf.Lerp(900f, 260f, s01);
            float popTop = Mathf.Lerp(560f, 230f, s01), popEnd = 55f;
            float decay = Mathf.Lerp(5.5f, 3.2f, s01);

            float held = 0f, holdPhase = 1f, phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / dur;

                float rate = holdStart * Mathf.Pow(holdEnd / holdStart, k);
                holdPhase += rate / SampleRate;
                if (holdPhase >= 1f) { holdPhase -= 1f; held = Noise(); }

                float pf = popTop * Mathf.Pow(popEnd / popTop, Mathf.Min(1f, k * 2.5f));
                phase += pf / SampleRate;
                float pop = Triangle(phase) * Mathf.Exp(-k * 9f);

                float env = Mathf.Exp(-k * decay) * Mathf.Min(1f, t * 500f);
                float smp = (held * 0.7f + pop * 0.65f) * env;
                lp += (smp - lp) * 0.45f;
                data[i] = lp;
            }
            return ToClip($"sfx_explosion_{size}_{variant}", data, 0.8f);
        }

        AudioClip SynthDeath()
        {
            float dur = 1.2f;
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float lpN = 0f, lpN2 = 0f, lpS = 0f, phase = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / dur;

                lpN += (Noise() - lpN) * 0.05f;
                lpN2 += (lpN - lpN2) * 0.05f;

                float f = 220f * Mathf.Pow(40f / 220f, k);
                phase += f / SampleRate;
                float saw = Mathf.Repeat(phase, 1f) * 2f - 1f;
                lpS += (saw - lpS) * 0.08f;

                float env = Mathf.Exp(-k * 2.5f) * Mathf.Min(1f, t * 200f);
                data[i] = (lpN2 * 4f + lpS * 0.5f) * env;
            }
            return ToClip("sfx_death", data, 0.9f);
        }

        AudioClip SynthHyperspace()
        {
            float dur = 0.4f;
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float phase = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / dur;
                float f = 300f * Mathf.Pow(5f, k) * (1f + Mathf.Sin(t * Mathf.PI * 2f * 28f) * 0.03f);
                phase += f / SampleRate;

                float s = Mathf.Sin(phase * Mathf.PI * 2f) * 0.7f + Triangle(phase * 0.5f) * 0.3f;
                data[i] = s * Mathf.Sin(k * Mathf.PI);
            }
            return ToClip("sfx_hyperspace", data, 0.6f);
        }

        AudioClip SynthWaveSwell()
        {
            float dur = 1.2f;
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float[] notes = { 220f, 329.63f, 523.25f };
            float[] detune = { 0.996f, 1.004f };
            var phases = new float[notes.Length * detune.Length];
            float lp = 0f, lp2 = 0f;

            for (int i = 0; i < n; i++)
            {
                float k = i / (float)n;
                float smp = 0f;
                for (int j = 0; j < notes.Length; j++)
                for (int d = 0; d < detune.Length; d++)
                {
                    int idx = j * detune.Length + d;
                    phases[idx] += notes[j] * detune[d] / SampleRate;
                    smp += Triangle(phases[idx]) * (j == 2 ? 0.6f : 1f);
                }
                smp /= phases.Length;

                float cutoff = Mathf.Lerp(0.015f, 0.2f, Mathf.Clamp01(k * 1.8f));
                lp += (smp - lp) * cutoff;
                lp2 += (lp - lp2) * cutoff;

                float env = k < 0.35f ? k / 0.35f : Mathf.Exp(-(k - 0.35f) * 4f);
                data[i] = lp2 * env;
            }
            return ToClip("sfx_wave", data, 0.55f);
        }

        AudioClip SynthExtraLife()
        {
            float dur = 0.55f;
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            const float step = 0.07f;
            float lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float smp = 0f;
                for (int j = 0; j < notes.Length; j++)
                {
                    float nt = t - j * step;
                    if (nt < 0f) continue;
                    float env = Mathf.Exp(-nt * 9f) * Mathf.Min(1f, nt * 400f);
                    smp += Triangle(nt * notes[j]) * env;
                }
                lp += (smp - lp) * 0.35f;
                data[i] = lp;
            }
            return ToClip("sfx_extralife", data, 0.55f);
        }

        AudioClip SynthBlip(float freq, float dur, bool twoNotes)
        {
            int n = Mathf.CeilToInt(dur * SampleRate);
            var data = new float[n];
            float phase = 0f, lp = 0f;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / dur;
                float f = twoNotes && k > 0.4f ? freq * 1.5f : freq;
                phase += f / SampleRate;
                float env = Mathf.Exp(-k * 4f) * Mathf.Min(1f, t * 800f);
                lp += (Triangle(phase) * env - lp) * 0.4f;
                data[i] = lp;
            }
            return ToClip(twoNotes ? "sfx_menu_confirm" : "sfx_menu_move", data, 0.5f);
        }
    }
}
