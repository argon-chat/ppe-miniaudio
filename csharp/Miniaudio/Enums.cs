using System;

namespace Miniaudio;

/// <summary>Mirrors ma_sound_flags. Combine with bitwise OR.</summary>
[Flags]
public enum SoundFlags : uint
{
    None = 0,
    /// <summary>Stream from source instead of fully decoding into memory.</summary>
    Stream = 0x00000001,
    /// <summary>Decode up front (good for short SFX that are replayed often).</summary>
    Decode = 0x00000002,
    /// <summary>Load asynchronously.</summary>
    Async = 0x00000004,
    WaitInit = 0x00000008,
    Looping = 0x00000020,
    NoPitch = 0x00002000,
    NoSpatialization = 0x00004000,
}

/// <summary>Mirrors ma_attenuation_model.</summary>
public enum AttenuationModel
{
    None = 0,
    Inverse = 1,
    Linear = 2,
    Exponential = 3,
}
