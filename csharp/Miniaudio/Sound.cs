using System;
using Miniaudio.Native;

namespace Miniaudio;

/// <summary>A loaded sound. Create via <see cref="MiniaudioEngine.LoadSound"/>.</summary>
public sealed unsafe class Sound : IDisposable
{
    private IntPtr _handle;

    internal Sound(IntPtr handle) => _handle = handle;

    private void* H => (void*)_handle;

    public void Play() => MiniaudioEngine.Check(NativeMethods.ma_wrap_sound_play(H), "sound_play");
    public void Stop() => MiniaudioEngine.Check(NativeMethods.ma_wrap_sound_stop(H), "sound_stop");

    public bool IsPlaying => NativeMethods.ma_wrap_sound_is_playing(H) != 0;
    public bool AtEnd => NativeMethods.ma_wrap_sound_at_end(H) != 0;

    public float Volume { set => NativeMethods.ma_wrap_sound_set_volume(H, value); }
    public float Pitch { set => NativeMethods.ma_wrap_sound_set_pitch(H, value); }
    public float Pan { set => NativeMethods.ma_wrap_sound_set_pan(H, value); }
    public bool Looping { set => NativeMethods.ma_wrap_sound_set_looping(H, value ? 1 : 0); }

    /// <summary>Seek to a PCM frame (32-bit; ~12h at 48kHz).</summary>
    public void SeekToFrame(uint frame)
        => MiniaudioEngine.Check(NativeMethods.ma_wrap_sound_seek_to_frame(H, frame), "sound_seek");

    /// <summary>Fade linear volume <paramref name="from"/> → <paramref name="to"/> over
    /// <paramref name="milliseconds"/>. <paramref name="from"/> &lt; 0 starts at the current volume.</summary>
    public void Fade(float from, float to, uint milliseconds)
        => NativeMethods.ma_wrap_sound_set_fade(H, from, to, milliseconds);

    // ---- spatialization ----
    public bool SpatializationEnabled { set => NativeMethods.ma_wrap_sound_set_spatialization_enabled(H, value ? 1 : 0); }
    public void SetPosition(float x, float y, float z) => NativeMethods.ma_wrap_sound_set_position(H, x, y, z);
    public void SetVelocity(float x, float y, float z) => NativeMethods.ma_wrap_sound_set_velocity(H, x, y, z);
    public AttenuationModel AttenuationModel { set => NativeMethods.ma_wrap_sound_set_attenuation_model(H, (int)value); }
    public float Rolloff { set => NativeMethods.ma_wrap_sound_set_rolloff(H, value); }
    public float MinDistance { set => NativeMethods.ma_wrap_sound_set_min_distance(H, value); }
    public float MaxDistance { set => NativeMethods.ma_wrap_sound_set_max_distance(H, value); }
    public float DopplerFactor { set => NativeMethods.ma_wrap_sound_set_doppler_factor(H, value); }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.ma_wrap_sound_unload(H);
            _handle = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
