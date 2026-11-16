using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace KingdomAccess.Speech;

/// <summary>
/// The mod's own audio output (NAudio), independent of the game: one Windows audio output and a
/// mixer into which every mod sound is added. The game's volume settings do not apply, and each
/// sound gets an exact volume and stereo position (pan -1 = left ear only, 1 = right ear only).
/// Falls back to the game's audio engine (GameAudio) if the output cannot be opened.
/// </summary>
internal static class ModAudio
{
    private const int Rate = 44100;
    private static WaveOutEvent _output;
    private static MixingSampleProvider _mixer;
    private static bool _ready, _failed;

    internal static IModLog Log;
    /// <summary>Master volume of the mod's sounds (0 to 1).</summary>
    public static float Volume = 1f;
    /// <summary>When false, the mod's sounds go through the game's audio engine instead.</summary>
    public static bool Enabled = true;

    private static bool Ready()
    {
        if (_ready) return true;
        if (_failed || !Enabled) return false;
        try
        {
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(Rate, 2)) { ReadFully = true };
            _output = new WaveOutEvent { DesiredLatency = 100 };
            _output.Init(_mixer);
            _output.Play();
            _ready = true;
            Log?.Info("[ModAudio] Own audio output ready (independent of the game volume).");
        }
        catch (Exception ex)
        {
            _failed = true;
            Log?.Error($"[ModAudio] Could not open the audio output, using the game's audio instead: {ex.Message}");
        }
        return _ready;
    }

    /// <summary>Plays mono samples (44.1 kHz). Returns false if the mod output is not available.</summary>
    public static bool Play(float[] mono, float volume, float pan)
    {
        if (mono == null || mono.Length == 0 || !Ready()) return false;
        try
        {
            _mixer.AddMixerInput(new OneShot(mono, Math.Clamp(volume, 0f, 1f) * Volume, Math.Clamp(pan, -1f, 1f)));
            return true;
        }
        catch { return false; }
    }

    /// <summary>One sound, played once, with a fixed volume and stereo position.</summary>
    private sealed class OneShot : ISampleProvider
    {
        private readonly float[] _samples;
        private readonly float _left, _right;
        private int _position;

        public OneShot(float[] samples, float volume, float pan)
        {
            _samples = samples;
            // Equal-power panning, scaled so that the centre is as loud as one side.
            double angle = (pan + 1) * Math.PI / 4;
            _left = volume * (float)Math.Min(1.0, Math.Cos(angle) * Math.Sqrt(2));
            _right = volume * (float)Math.Min(1.0, Math.Sin(angle) * Math.Sqrt(2));
        }

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(Rate, 2);

        public int Read(float[] buffer, int offset, int count)
        {
            int frames = Math.Min(count / 2, _samples.Length - _position);
            for (int i = 0; i < frames; i++)
            {
                float v = _samples[_position++];
                buffer[offset + 2 * i] = v * _left;
                buffer[offset + 2 * i + 1] = v * _right;
            }
            return frames * 2; // 0 once finished: the mixer then removes this sound
        }
    }
}
