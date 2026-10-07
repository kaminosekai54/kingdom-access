using System;
using System.Collections.Generic;
using KingdomAccess.Game;
using UnityEngine;

namespace KingdomAccess.Speech;

/// <summary>
/// Earcons: a short, recognisable sound for each kind of object (castle, wall, tower, farm,
/// shop, tree, camp, character, statue, treasure, danger, boat, puzzle, bomb, building), with a
/// suffix for the action (two hammer knocks to build, two rising notes to upgrade). Generated
/// by the mod. Mounts use their real cry taken from the game when it is loaded, otherwise a
/// generated neigh.
/// </summary>
internal static class Earcons
{
    private const int Rate = 44100;
    private static readonly Dictionary<string, float[]> Samples = new();
    private static readonly Dictionary<string, AudioClip> Clips = new();
    private static readonly System.Random Rng = new(7);
    private static string _dir;

    /// <summary>Folder of the sound files (Sounds\Earcons in the mod folder).</summary>
    public static void Initialize(string modDirectory) =>
        _dir = System.IO.Path.Combine(modDirectory ?? "", "Sounds", "Earcons");

    /// <summary>Sounds of the legend, in order: each family, then its "to build" and "to upgrade" states.</summary>
    public static readonly string[] Legend =
    {
        "castle", "castle_upgrade", "wall", "wall_build", "wall_upgrade", "tower", "tower_build", "tower_upgrade",
        "building", "building_build", "building_upgrade", "farm", "farm_build",
        "shop_bow", "shop_hammer", "shop_scythe", "shop_pike", "shop_shield", "shop_forge", "shop_ninja", "shop_workshop",
        "merchant", "tree", "camp", "character", "statue", "treasure", "danger", "boat", "puzzle", "bomb", "mount"
    };

    // ---------- Classification ----------

    public static string FamilyOf(ObjKind kind) => kind switch
    {
        ObjKind.Castle => "castle",
        ObjKind.Wall => "wall",
        ObjKind.Tower or ObjKind.Ballista => "tower",
        ObjKind.Farmhouse or ObjKind.Farmland => "farm",
        ObjKind.Forge => "shop_forge",
        ObjKind.Workshop => "shop_workshop",
        ObjKind.Dojo => "shop_ninja",
        ObjKind.Shop or ObjKind.Merchant => "merchant",
        ObjKind.Tree or ObjKind.Bush => "tree",
        ObjKind.BeggarCamp or ObjKind.Beggar => "camp",
        ObjKind.Hermit or ObjKind.Banker or ObjKind.Oracle or ObjKind.Dog => "character",
        ObjKind.Statue or ObjKind.CrownStatue => "statue",
        ObjKind.Chest or ObjKind.GemChest or ObjKind.GemGuard => "treasure",
        ObjKind.Portal or ObjKind.CaveNest => "danger",
        ObjKind.Boat or ObjKind.Wharf or ObjKind.Shipyard or ObjKind.Lighthouse or ObjKind.Bell => "boat",
        ObjKind.Puzzle => "puzzle",
        ObjKind.Bomb or ObjKind.CaveBomb => "bomb",
        ObjKind.Steed => "mount",
        _ => "building"
    };

    /// <summary>Shops: the sound of what they sell.</summary>
    private static string ShopFamily(Component c)
    {
        try
        {
            // The exact shop type is given by ShopTag (as for the shop names).
            var tag = c.GetComponent<ShopTag>();
            if (tag == null) return null;
            return tag.type switch
            {
                PayableShop.ShopType.Bow => "shop_bow",
                PayableShop.ShopType.Hammer => "shop_hammer",
                PayableShop.ShopType.Scythe => "shop_scythe",
                PayableShop.ShopType.PikeLeft or PayableShop.ShopType.PikeRight => "shop_pike",
                PayableShop.ShopType.ShieldShopLeft or PayableShop.ShopType.ShieldShopRight => "shop_shield",
                PayableShop.ShopType.Forge => "shop_forge",
                PayableShop.ShopType.NinjaLeft or PayableShop.ShopType.NinjaRight => "shop_ninja",
                PayableShop.ShopType.WorkshopLeft or PayableShop.ShopType.WorkshopRight => "shop_workshop",
                _ => null
            };
        }
        catch { return null; }
    }

    /// <summary>
    /// Sound key of an object: its family (the shop type for shops), with "_build" or
    /// "_upgrade" when that is what paying would do. One sound per key, no suffix.
    /// </summary>
    public static string KeyFor(Component c, ObjKind kind, string actionKey)
    {
        string family = (kind == ObjKind.Shop ? ShopFamily(c) : null) ?? FamilyOf(kind);
        return actionKey switch
        {
            "act.build" => family + "_build",
            "act.upgrade" => family + "_upgrade",
            _ => family
        };
    }

    // ---------- Playing ----------

    /// <summary>Plays the sound of a key (see <see cref="KeyFor"/>).</summary>
    public static void Play(string key, float volume, float pan)
    {
        var clip = Get(key);
        if (clip != null) GameAudio.PlayClip(clip, volume, pan);
    }

    /// <summary>Plays a mount's cry (game sound when loaded, generated neigh otherwise).</summary>
    public static void PlayMount(Steed steed, float volume, float pan)
    {
        var clip = MountClip(steed) ?? Get("mount");
        if (clip != null) GameAudio.PlayClip(clip, volume, pan);
    }

    private static AudioClip Get(string key)
    {
        if (Clips.TryGetValue(key, out var c) && c != null) return c;
        try
        {
            c = GameAudio.MakeClip("ka_" + key, Samples_(key));
            Clips[key] = c;
            return c;
        }
        catch { return null; }
    }

    /// <summary>
    /// Samples of a key: its own file, else (for a state) the family sound shifted in pitch
    /// (lower to build, higher to upgrade), else a generated sound.
    /// </summary>
    private static float[] Samples_(string key)
    {
        var s = Build(key);
        if (s != null) return s;
        foreach (var (suffix, pitch) in new[] { ("_build", 0.72f), ("_upgrade", 1.4f) })
            if (key.EndsWith(suffix))
            {
                var baseSamples = Build(key.Substring(0, key.Length - suffix.Length)) ?? Generated("building");
                return Pitch(baseSamples, pitch);
            }
        return Generated(key.StartsWith("shop_") ? "shop" : key);
    }

    private static float[] Pitch(float[] s, float factor)
    {
        int n = Math.Max(1, (int)(s.Length / factor));
        n = Math.Min(n, (int)(Rate * 0.5f));
        var r = new float[n];
        for (int i = 0; i < n; i++)
        {
            float x = i * factor;
            int a = (int)x;
            if (a + 1 >= s.Length) break;
            r[i] = s[a] + (s[a + 1] - s[a]) * (x - a);
        }
        return r;
    }

    // ---------- Mount cries from the game ----------

    private static Dictionary<string, AudioClip> _gameClips;
    private static float _gameClipsAt = -100f;

    private static readonly Dictionary<string, string[]> MountCries = new()
    {
        { "griffin", new[] { "griffin_rear", "griffin_cry01" } },
        { "bear", new[] { "bear_reer" } },
        { "lizard", new[] { "lizard_rear" } },
        { "cerberus", new[] { "sfx_greece_cerberus_rears_01", "sfx_greece_cerberus_growl_01" } },
        { "donkey", new[] { "sfx_greece_donkey_rear_01" } },
        { "pegasus", new[] { "sfx_greece_pegasus_rear_01" } },
        { "spider", new[] { "sfx_greece_spider_rear_01" } },
        { "trap", new[] { "sfx_steed_beetle_rear_01" } },
        { "catcart", new[] { "sfx_steed_catcart_rear_01" } },
        { "gullinbursti", new[] { "boar_roar_rt" } },
    };

    private static AudioClip MountClip(Steed steed)
    {
        try
        {
            string key = steed.steedType.ToString().ToLowerInvariant().Replace("_norselands", "");
            if (key.StartsWith("p1") || key.StartsWith("p2")) key = key.Substring(2);
            var clips = GameClips();
            if (MountCries.TryGetValue(key, out var names))
                foreach (var n in names)
                    if (clips.TryGetValue(n, out var c) && c != null) return c;
            // Any loaded "rear" sound named after the mount, then the horse's.
            foreach (var kv in clips)
                if (kv.Key.Contains(key) && kv.Key.Contains("rear") && kv.Value != null) return kv.Value;
            if (clips.TryGetValue("horse_rear", out var horse) && horse != null) return horse;
        }
        catch { }
        return null;
    }

    /// <summary>Sounds currently loaded by the game, by name (refreshed every 30 seconds at most).</summary>
    private static Dictionary<string, AudioClip> GameClips()
    {
        if (_gameClips != null && Time.unscaledTime - _gameClipsAt < 30f) return _gameClips;
        _gameClipsAt = Time.unscaledTime;
        _gameClips = new Dictionary<string, AudioClip>();
        try
        {
            foreach (var c in Resources.FindObjectsOfTypeAll<AudioClip>())
                if (c != null && !string.IsNullOrEmpty(c.name)) _gameClips[c.name.ToLowerInvariant()] = c;
        }
        catch { }
        return _gameClips;
    }

    // ---------- Sound design ----------

    /// <summary>Sound file of a key (Sounds\\Earcons\\key.wav), or null.</summary>
    private static float[] Build(string id)
    {
        if (Samples.TryGetValue(id, out var s)) return s;
        s = LoadWav(id);
        if (s == null) return null;
        // The bomb tick is very short: three ticks are easier to recognise.
        if (id == "bomb") s = Concat(s, Silence(0.08f), s, Silence(0.08f), s);
        Samples[id] = s;
        return s;
    }

    /// <summary>Sound generated by the mod, used when no file exists.</summary>
    private static float[] Generated(string id)
    {
        float[] s = id switch
        {
            "castle" => Bell(196f, 0.45f, 1f),
            "wall" => Thud(),
            "tower" => Ting(1760f, 0.2f),
            "farm" => Swish(),
            "shop" => Concat(Ting(2637f, 0.06f), Ting(3136f, 0.08f)),
            "tree" => Concat(Knock(520f), Silence(0.05f), Knock(470f)),
            "camp" => Camp(),
            "character" => Voice(),
            "statue" => Mix(Ting(1047f, 0.3f), Ting(1568f, 0.3f), 0.6f),
            "treasure" => Concat(Ting(1319f, 0.04f), Ting(1568f, 0.04f), Ting(1976f, 0.04f), Ting(2637f, 0.08f)),
            "danger" => Buzz(),
            "boat" => Horn(),
            "puzzle" => Concat(Note(523f, 0.07f), Note(659f, 0.07f), Note(831f, 0.12f)),
            "bomb" => Concat(Click(), Silence(0.05f), Click(), Silence(0.05f), Click()),
            "mount" => Neigh(),
            "build" => Concat(Hammer(), Silence(0.05f), Hammer()),
            "upgrade" => Concat(Note(880f, 0.06f), Note(1319f, 0.09f)),
            _ => Concat(Note(330f, 0.06f), Note(440f, 0.08f))
        };
        Normalize(s, 0.9f);
        return s;
    }

    /// <summary>Reads Sounds\Earcons\{id}.wav (16-bit PCM, mono or stereo), or null.</summary>
    private static float[] LoadWav(string id)
    {
        try
        {
            if (_dir == null) return null;
            string path = System.IO.Path.Combine(_dir, id + ".wav");
            if (!System.IO.File.Exists(path)) return null;
            byte[] b = System.IO.File.ReadAllBytes(path);
            int channels = 1, bits = 16, pos = 12;
            while (pos + 8 <= b.Length)
            {
                string chunk = System.Text.Encoding.ASCII.GetString(b, pos, 4);
                int size = BitConverter.ToInt32(b, pos + 4);
                if (chunk == "fmt ") { channels = BitConverter.ToInt16(b, pos + 10); bits = BitConverter.ToInt16(b, pos + 22); }
                else if (chunk == "data" && bits == 16)
                {
                    int frames = size / (2 * channels);
                    var s = new float[frames];
                    for (int i = 0; i < frames; i++)
                    {
                        float sum = 0f;
                        for (int c = 0; c < channels; c++) sum += BitConverter.ToInt16(b, pos + 8 + (i * channels + c) * 2) / 32768f;
                        s[i] = sum / channels;
                    }
                    return s;
                }
                pos += 8 + size + (size & 1);
            }
        }
        catch { }
        return null;
    }

    private static float[] New(float seconds) => new float[Math.Max(1, (int)(Rate * seconds))];
    private static float[] Silence(float seconds) => New(seconds);

    private static float[] Concat(params float[][] parts)
    {
        int n = 0;
        foreach (var p in parts) n += p.Length;
        var r = new float[n];
        int o = 0;
        foreach (var p in parts) { Array.Copy(p, 0, r, o, p.Length); o += p.Length; }
        return r;
    }

    private static float[] Mix(float[] a, float[] b, float gainB)
    {
        var r = new float[Math.Max(a.Length, b.Length)];
        for (int i = 0; i < r.Length; i++) r[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] * gainB : 0f);
        return r;
    }

    private static void Normalize(float[] s, float peak)
    {
        float max = 0f;
        foreach (var v in s) max = Math.Max(max, Math.Abs(v));
        if (max <= 0f) return;
        float k = peak / max;
        for (int i = 0; i < s.Length; i++) s[i] *= k;
    }

    private static float Env(int i, int n, float attack, float decay)
    {
        float t = i / (float)Rate;
        float a = Math.Min(1f, t / Math.Max(0.001f, attack));
        return a * (float)Math.Exp(-t * decay) * Math.Min(1f, (n - i) / 300f);
    }

    private static float[] Note(float f, float dur)
    {
        var s = New(dur);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            s[i] = Env(i, s.Length, 0.005f, 12f) * ((float)Math.Sin(2 * Math.PI * f * t) + 0.25f * (float)Math.Sin(4 * Math.PI * f * t));
        }
        return s;
    }

    private static float[] Ting(float f, float dur)
    {
        var s = New(dur);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            s[i] = Env(i, s.Length, 0.002f, 14f) * ((float)Math.Sin(2 * Math.PI * f * t) + 0.4f * (float)Math.Sin(2 * Math.PI * f * 2.76f * t));
        }
        return s;
    }

    private static float[] Bell(float f, float dur, float amp)
    {
        var s = New(dur);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float e = Env(i, s.Length, 0.003f, 6f);
            s[i] = amp * e * ((float)Math.Sin(2 * Math.PI * f * t) + 0.6f * (float)Math.Sin(2 * Math.PI * f * 2f * t)
                                + 0.4f * (float)Math.Sin(2 * Math.PI * f * 2.76f * t) + 0.25f * (float)Math.Sin(2 * Math.PI * f * 5.4f * t));
        }
        return s;
    }

    private static float[] Thud()
    {
        var s = New(0.16f);
        float lp = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            lp += 0.08f * ((float)(Rng.NextDouble() * 2 - 1) - lp);
            float e = Env(i, s.Length, 0.002f, 28f);
            s[i] = e * (lp * 2.5f + 0.9f * (float)Math.Sin(2 * Math.PI * (90f - 40f * t) * t));
        }
        return s;
    }

    private static float[] Knock(float f)
    {
        var s = New(0.07f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            s[i] = Env(i, s.Length, 0.001f, 60f) * ((float)Math.Sin(2 * Math.PI * f * t) + 0.3f * (float)(Rng.NextDouble() * 2 - 1));
        }
        return s;
    }

    private static float[] Hammer()
    {
        var s = New(0.08f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            s[i] = Env(i, s.Length, 0.001f, 45f) * (0.6f * (float)(Rng.NextDouble() * 2 - 1) + (float)Math.Sin(2 * Math.PI * 1900f * t) * 0.5f
                                                     + (float)Math.Sin(2 * Math.PI * 300f * t) * 0.6f);
        }
        return s;
    }

    private static float[] Swish()
    {
        var s = New(0.22f);
        float lp = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float p = i / (float)s.Length;
            float cut = 0.03f + 0.4f * p; // brighter as the blade sweeps
            lp += cut * ((float)(Rng.NextDouble() * 2 - 1) - lp);
            float e = (float)Math.Sin(Math.PI * p);
            s[i] = e * lp * 2f;
        }
        return s;
    }

    private static float[] Camp()
    {
        var s = New(0.28f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float e = Env(i, s.Length, 0.04f, 5f);
            float crackle = Rng.NextDouble() < 0.004 ? (float)(Rng.NextDouble() * 2 - 1) * 2f : 0f;
            s[i] = e * (0.6f * (float)Math.Sin(2 * Math.PI * 147f * t) + 0.4f * (float)Math.Sin(2 * Math.PI * 220f * t)) + crackle * 0.5f;
        }
        return s;
    }

    private static float[] Voice()
    {
        var s = New(0.22f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float f = 140f + 30f * t / 0.22f;
            float e = Env(i, s.Length, 0.02f, 4f);
            float v = 0f;
            for (int h = 1; h <= 8; h++)
            {
                float hf = f * h;
                float formant = (float)(Math.Exp(-Math.Pow((hf - 500f) / 180f, 2)) + 0.6 * Math.Exp(-Math.Pow((hf - 1100f) / 250f, 2)));
                v += formant * (float)Math.Sin(2 * Math.PI * hf * t);
            }
            s[i] = e * v;
        }
        return s;
    }

    private static float[] Buzz()
    {
        var s = New(0.24f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float e = Env(i, s.Length, 0.005f, 7f) * (0.75f + 0.25f * (float)Math.Sin(2 * Math.PI * 18f * t));
            float a = Math.Sign(Math.Sin(2 * Math.PI * 110f * t)), b = Math.Sign(Math.Sin(2 * Math.PI * 117f * t));
            s[i] = e * 0.5f * (a + b);
        }
        return s;
    }

    private static float[] Horn()
    {
        var s = New(0.3f);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Rate;
            float f = 98f + 12f * t / 0.3f;
            float e = Env(i, s.Length, 0.04f, 3.5f);
            s[i] = e * ((float)Math.Sin(2 * Math.PI * f * t) + 0.5f * (float)Math.Sin(4 * Math.PI * f * t) + 0.3f * (float)Math.Sin(6 * Math.PI * f * t));
        }
        return s;
    }

    private static float[] Click()
    {
        var s = New(0.02f);
        for (int i = 0; i < s.Length; i++) s[i] = Env(i, s.Length, 0.0005f, 200f) * (float)Math.Sin(2 * Math.PI * 2500f * i / Rate);
        return s;
    }

    private static float[] Neigh()
    {
        var s = New(0.32f);
        double phase = 0;
        for (int i = 0; i < s.Length; i++)
        {
            float p = i / (float)s.Length;
            float f = 600f + 350f * (float)Math.Sin(Math.PI * p) + 60f * (float)Math.Sin(2 * Math.PI * 22f * i / Rate);
            phase += 2 * Math.PI * f / Rate;
            float e = (float)Math.Sin(Math.PI * p);
            s[i] = e * (float)(Math.Sin(phase) + 0.4 * Math.Sin(2 * phase));
        }
        return s;
    }
}
