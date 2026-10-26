using System.Collections.Generic;
using UnityEngine;

public enum Sfx
{
    Flap, Score, PowerLife, PowerGun, PowerShrink, PowerGhost, PowerShield,
    Shoot, Explosion, Hit, ShieldBreak, GameOver, Record, EnemyWarn,
    Stinger, Laser, BombWhistle, BombBoom, PipeBreak, Click, Zone, Thunder, Charge
}

// Sonido chiptune sintetizado por código: efectos de 8 bits y música en bucle para el menú
// y para cada escenario. La percusión es una capa aparte que sube con la dificultad.
public class AudioManager : MonoBehaviour
{
    const int Rate = 22050;

    public static AudioManager Instance { get; private set; }

    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    readonly Dictionary<int, AudioClip[]> songs = new Dictionary<int, AudioClip[]>();
    readonly List<AudioSource> sfxSources = new List<AudioSource>();
    int nextSource;
    AudioSource music, drums;
    int currentSong = -1;
    float intensity;
    float musicFade = 1f;

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }

    public static AudioManager Create(Transform parent)
    {
        var go = new GameObject("Audio");
        go.transform.SetParent(parent, false);
        return go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        Instance = this;
        MusicVolume = PlayerPrefs.GetFloat(GameConfig.MusicVolumeKey, 0.6f);
        SfxVolume = PlayerPrefs.GetFloat(GameConfig.SfxVolumeKey, 0.8f);
        for (int i = 0; i < 10; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            sfxSources.Add(s);
        }
        music = gameObject.AddComponent<AudioSource>();
        drums = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { music, drums }) { s.loop = true; s.playOnAwake = false; }
        BuildSfx();
    }

    public void SetMusicVolume(float v)
    {
        MusicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(GameConfig.MusicVolumeKey, MusicVolume);
    }

    public void SetSfxVolume(float v)
    {
        SfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(GameConfig.SfxVolumeKey, SfxVolume);
    }

    // 0..1: cuánta percusión se escucha (sube con la puntuación).
    public void SetIntensity(float v) { intensity = Mathf.Clamp01(v); }

    // Baja la música temporalmente (pausa, fin de partida).
    public void SetMusicDucked(bool ducked) { musicFade = ducked ? 0.35f : 1f; }

    void Update()
    {
        music.volume = Mathf.MoveTowards(music.volume, MusicVolume * 0.55f * musicFade, Time.unscaledDeltaTime);
        float drumTarget = MusicVolume * 0.5f * musicFade * Mathf.Lerp(0.25f, 1f, intensity);
        drums.volume = Mathf.MoveTowards(drums.volume, drumTarget, Time.unscaledDeltaTime * 0.5f);
    }

    public void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
    {
        if (SfxVolume <= 0f || !clips.TryGetValue(sfx, out var clip)) return;
        var src = sfxSources[nextSource];
        nextSource = (nextSource + 1) % sfxSources.Count;
        src.pitch = pitch;
        src.PlayOneShot(clip, volume * SfxVolume);
    }

    // 0 = menú, 1 = cielo, 2 = fábrica, 3 = tormenta
    public void PlaySong(int index)
    {
        if (index == currentSong) return;
        currentSong = index;
        if (!songs.TryGetValue(index, out var pair))
        {
            pair = RenderSong(index);
            songs[index] = pair;
        }
        music.Stop();
        drums.Stop();
        music.clip = pair[0];
        drums.clip = pair[1];
        music.volume = 0f;
        drums.volume = 0f;
        double start = AudioSettings.dspTime + 0.1;
        music.PlayScheduled(start);
        drums.PlayScheduled(start);
    }

    // ------------------------------------------------------------ Síntesis de efectos

    static float Square(float phase, float duty) => (phase - Mathf.Floor(phase)) < duty ? 1f : -1f;
    static float Triangle(float phase) { float p = phase - Mathf.Floor(phase); return 4f * Mathf.Abs(p - 0.5f) - 1f; }
    static float Sine(float phase) => Mathf.Sin(phase * Mathf.PI * 2f);

    static float Midi(float note) => 440f * Mathf.Pow(2f, (note - 69f) / 12f);

    delegate float Voice(float t, float dur);

    static AudioClip Make(string name, float duration, Voice voice)
    {
        int n = Mathf.CeilToInt(duration * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float fade = Mathf.Clamp01((duration - t) / 0.01f); // evita "clics" al final
            data[i] = Mathf.Clamp(voice(t, duration) * fade, -1f, 1f);
        }
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Barrido de frecuencia con forma de onda y caída de volumen.
    static AudioClip Sweep(string name, float dur, float f0, float f1, int wave, float vol, float decay = 3f, float noise = 0f)
    {
        float phase = 0f;
        var rnd = new System.Random(name.GetHashCode());
        float lp = 0f;
        return Make(name, dur, (t, d) =>
        {
            float k = t / d;
            float f = Mathf.Lerp(f0, f1, k);
            phase += f / Rate;
            float s = wave == 0 ? Square(phase, 0.5f) : wave == 1 ? Square(phase, 0.25f) : wave == 2 ? Triangle(phase) : Sine(phase);
            if (noise > 0f)
            {
                float nz = (float)rnd.NextDouble() * 2f - 1f;
                lp += (nz - lp) * 0.35f;
                s = Mathf.Lerp(s, lp * 2f, noise);
            }
            return s * vol * Mathf.Exp(-k * decay);
        });
    }

    // Secuencia de notas cortas (arpegios, jingles).
    static AudioClip Notes(string name, float[] notes, float noteLen, int wave, float vol, float tail = 0.15f)
    {
        float dur = notes.Length * noteLen + tail;
        float phase = 0f;
        return Make(name, dur, (t, d) =>
        {
            int i = Mathf.Min(notes.Length - 1, (int)(t / noteLen));
            float local = t - i * noteLen;
            if (notes[i] <= 0f) return 0f; // silencio
            float f = Midi(notes[i]);
            phase += f / Rate;
            float s = wave == 0 ? Square(phase, 0.5f) : wave == 1 ? Square(phase, 0.25f) : wave == 2 ? Triangle(phase) : Sine(phase);
            bool last = i == notes.Length - 1;
            float env = Mathf.Exp(-local * (last ? 4f : 10f));
            return s * vol * env;
        });
    }

    static AudioClip Noise(string name, float dur, float vol, float smooth, float decay)
    {
        var rnd = new System.Random(name.GetHashCode());
        float lp = 0f;
        return Make(name, dur, (t, d) =>
        {
            float nz = (float)rnd.NextDouble() * 2f - 1f;
            lp += (nz - lp) * smooth;
            return lp * vol * Mathf.Exp(-t / d * decay);
        });
    }

    static AudioClip Mix(string name, params AudioClip[] parts)
    {
        int n = 0;
        foreach (var p in parts) n = Mathf.Max(n, p.samples);
        var data = new float[n];
        var tmp = new float[n];
        foreach (var p in parts)
        {
            p.GetData(tmp, 0);
            for (int i = 0; i < p.samples; i++) data[i] = Mathf.Clamp(data[i] + tmp[i], -1f, 1f);
        }
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void BuildSfx()
    {
        clips[Sfx.Flap] = Sweep("flap", 0.09f, 260f, 520f, 2, 0.35f, 2f, 0.35f);
        clips[Sfx.Score] = Notes("score", new float[] { 83, 88 }, 0.06f, 1, 0.22f, 0.08f);
        clips[Sfx.PowerLife] = Notes("life", new float[] { 72, 76, 79, 84 }, 0.06f, 0, 0.2f, 0.25f);
        clips[Sfx.PowerGun] = Mix("gun", Sweep("gun1", 0.25f, 180f, 900f, 1, 0.25f, 1f), Noise("gun2", 0.12f, 0.4f, 0.6f, 6f));
        clips[Sfx.PowerShrink] = Sweep("shrink", 0.35f, 1000f, 300f, 1, 0.22f, 1.5f);
        clips[Sfx.PowerGhost] = Notes("ghost", new float[] { 67, 71, 74, 78, 81 }, 0.07f, 3, 0.3f, 0.35f);
        clips[Sfx.PowerShield] = Mix("shield", Notes("sh1", new float[] { 79, 84 }, 0.08f, 3, 0.3f, 0.4f), Notes("sh2", new float[] { 86, 91 }, 0.08f, 3, 0.15f, 0.4f));
        clips[Sfx.Shoot] = Sweep("shoot", 0.13f, 1300f, 250f, 1, 0.25f, 2f);
        clips[Sfx.Explosion] = Mix("boom", Noise("boom1", 0.5f, 0.9f, 0.12f, 4f), Sweep("boom2", 0.3f, 150f, 40f, 0, 0.3f, 3f));
        clips[Sfx.Hit] = Mix("hit", Sweep("hit1", 0.3f, 320f, 60f, 0, 0.3f, 2f), Noise("hit2", 0.2f, 0.5f, 0.4f, 5f));
        clips[Sfx.ShieldBreak] = Mix("sbreak", Sweep("sb1", 0.3f, 1600f, 400f, 3, 0.3f, 2f), Noise("sb2", 0.25f, 0.4f, 0.8f, 6f));
        clips[Sfx.GameOver] = Notes("over", new float[] { 67, 64, 60, 55 }, 0.16f, 2, 0.4f, 0.4f);
        clips[Sfx.Record] = Notes("record", new float[] { 72, 76, 79, 84, 79, 84, 88 }, 0.08f, 0, 0.18f, 0.5f);
        clips[Sfx.EnemyWarn] = Notes("warn", new float[] { 90, 0, 90 }, 0.06f, 1, 0.12f, 0.05f);
        clips[Sfx.Stinger] = Sweep("stinger", 0.1f, 700f, 1500f, 1, 0.16f, 2f);
        clips[Sfx.Laser] = Sweep("laser", 0.22f, 1900f, 300f, 0, 0.14f, 1.5f);
        clips[Sfx.BombWhistle] = Sweep("whistle", 0.5f, 1500f, 600f, 3, 0.15f, 0.5f);
        clips[Sfx.BombBoom] = Mix("bboom", Noise("bb1", 0.6f, 1f, 0.08f, 3f), Sweep("bb2", 0.4f, 120f, 30f, 2, 0.4f, 2f));
        clips[Sfx.PipeBreak] = Mix("pbreak", Noise("pb1", 0.3f, 0.7f, 0.3f, 5f), Sweep("pb2", 0.25f, 400f, 90f, 0, 0.2f, 3f));
        clips[Sfx.Click] = Sweep("click", 0.04f, 700f, 700f, 1, 0.15f, 3f);
        clips[Sfx.Zone] = Notes("zone", new float[] { 72, 79, 84, 88, 91 }, 0.09f, 3, 0.3f, 0.6f);
        clips[Sfx.Thunder] = Noise("thunder", 1.6f, 1f, 0.04f, 2.5f);
        clips[Sfx.Charge] = Sweep("charge", 0.5f, 300f, 1200f, 3, 0.1f, 0f);
    }

    // ------------------------------------------------------------ Música

    class SongSpec
    {
        public float bpm;
        public int root;           // nota MIDI de la tónica
        public int[][] chords;     // acordes (semitonos desde la tónica)
        public int[] scale;        // escala para la melodía
        public int seed;
        public float leadDuty;
        public bool arpeggio;
        public bool calm;
    }

    static readonly int[] Major = { 0, 2, 4, 7, 9, 12, 14, 16 };          // pentatónica mayor
    static readonly int[] Minor = { 0, 3, 5, 7, 10, 12, 15, 17 };         // pentatónica menor

    static SongSpec Spec(int index)
    {
        switch (index)
        {
            case 1: // Cielo de las Tuberías: alegre, Do mayor
                return new SongSpec { bpm = 120, root = 60, seed = 11, leadDuty = 0.5f, arpeggio = true, scale = Major,
                    chords = new[] { new[] { 0, 4, 7 }, new[] { -3, 0, 4 }, new[] { -7, -3, 0 }, new[] { -5, -1, 2 } } };
            case 2: // Fábrica Flotante: menor e insistente
                return new SongSpec { bpm = 128, root = 57, seed = 23, leadDuty = 0.25f, arpeggio = false, scale = Minor,
                    chords = new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { -2, 2, 5 }, new[] { -5, -1, 2 } } };
            case 3: // Tormenta Nocturna: misteriosa
                return new SongSpec { bpm = 132, root = 62, seed = 37, leadDuty = 0.125f, arpeggio = true, scale = Minor,
                    chords = new[] { new[] { 0, 3, 7 }, new[] { -4, 0, 3 }, new[] { -2, 2, 5 }, new[] { -5, -2, 2 } } };
            default: // Menú: tranquilo
                return new SongSpec { bpm = 92, root = 65, seed = 5, leadDuty = 0.5f, arpeggio = true, calm = true, scale = Major,
                    chords = new[] { new[] { 0, 4, 7 }, new[] { -5, -1, 2 }, new[] { -3, 0, 4 }, new[] { -7, -3, 0 } } };
        }
    }

    static AudioClip[] RenderSong(int index)
    {
        var spec = Spec(index);
        const int bars = 8;
        float beat = 60f / spec.bpm;
        float eighth = beat / 2f;
        int steps = bars * 8;
        int n = Mathf.CeilToInt(steps * eighth * Rate);
        var main = new float[n];
        var perc = new float[n];
        var rnd = new System.Random(spec.seed);

        // Melodía: frase de 2 compases que se repite con variaciones
        var phrase = new int[16];
        int deg = 2;
        for (int i = 0; i < 16; i++)
        {
            if (rnd.NextDouble() < (spec.calm ? 0.35 : 0.2)) { phrase[i] = -1; continue; }
            deg = Mathf.Clamp(deg + rnd.Next(-2, 3), 0, spec.scale.Length - 1);
            phrase[i] = deg;
        }

        for (int step = 0; step < steps; step++)
        {
            int bar = step / 8;
            var chord = spec.chords[(bar / 2) % spec.chords.Length];
            int s0 = Mathf.RoundToInt(step * eighth * Rate);
            int len = Mathf.RoundToInt(eighth * Rate);

            // Melodía (las dos últimas frases cambian un poco)
            int m = phrase[step % 16];
            if (bar >= 4 && step % 16 >= 12 && m >= 0) m = Mathf.Min(spec.scale.Length - 1, m + 1);
            if (m >= 0)
            {
                float f = Midi(spec.root + 12 + spec.scale[m]);
                AddTone(main, s0, len, f, spec.leadDuty, 0.11f, 0);
            }
            // Bajo
            if (spec.calm ? step % 4 == 0 : true)
            {
                int bn = (step % 4 == 2) ? chord[2] : chord[0];
                AddTone(main, s0, len, Midi(spec.root - 12 + bn), 0.5f, 0.16f, 2);
            }
            // Arpegio suave en semicorcheas
            if (spec.arpeggio)
            {
                for (int k = 0; k < 2; k++)
                {
                    int note = chord[(step * 2 + k) % 3];
                    AddTone(main, s0 + k * len / 2, len / 2, Midi(spec.root + note + 12), 0.125f, 0.035f, 0);
                }
            }
            // Percusión (capa aparte)
            if (step % 4 == 0) AddKick(perc, s0);
            if (step % 4 == 2 && !spec.calm) AddNoise(perc, s0, 0.12f, 0.5f, 0.6f, rnd);
            AddNoise(perc, s0, 0.03f, 0.12f, 0.95f, rnd);
        }

        var a = AudioClip.Create("musica" + index, n, 1, Rate, false);
        a.SetData(main, 0);
        var b = AudioClip.Create("percusion" + index, n, 1, Rate, false);
        b.SetData(perc, 0);
        return new[] { a, b };
    }

    static void AddTone(float[] buf, int start, int len, float freq, float duty, float vol, int wave)
    {
        float phase = 0f;
        for (int i = 0; i < len && start + i < buf.Length; i++)
        {
            float t = i / (float)Rate;
            phase += freq / Rate;
            float s = wave == 2 ? Triangle(phase) : Square(phase, duty);
            float env = Mathf.Min(1f, t / 0.005f) * Mathf.Lerp(1f, 0.55f, i / (float)len) * Mathf.Clamp01((len - i) / (0.01f * Rate));
            buf[start + i] += s * vol * env;
        }
    }

    static void AddKick(float[] buf, int start)
    {
        float phase = 0f;
        int len = (int)(0.15f * Rate);
        for (int i = 0; i < len && start + i < buf.Length; i++)
        {
            float t = i / (float)Rate;
            phase += Mathf.Lerp(140f, 40f, t / 0.15f) / Rate;
            buf[start + i] += Sine(phase) * 0.5f * Mathf.Exp(-t * 20f);
        }
    }

    static void AddNoise(float[] buf, int start, float dur, float vol, float smooth, System.Random rnd)
    {
        int len = (int)(dur * Rate);
        float lp = 0f, prev = 0f;
        for (int i = 0; i < len && start + i < buf.Length; i++)
        {
            float t = i / (float)Rate;
            float nz = (float)rnd.NextDouble() * 2f - 1f;
            lp += (nz - lp) * smooth;
            float hp = lp - prev * 0.5f;
            prev = lp;
            buf[start + i] += hp * vol * Mathf.Exp(-t / dur * 5f);
        }
    }
}
