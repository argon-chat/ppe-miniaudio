using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Miniaudio.Native;

namespace Miniaudio;

/// <summary>
/// A miniaudio engine instance. Owns the playback device and the resource manager.
/// Created with no-auto-start, so call <see cref="Start"/> before sounds are audible
/// (on the web this must happen inside a user gesture).
/// </summary>
public sealed unsafe class MiniaudioEngine : IDisposable
{
    private IntPtr _handle;
    private readonly Dictionary<string, GCHandle> _pinned = new();

    public MiniaudioEngine()
    {
        _handle = (IntPtr)NativeMethods.ma_wrap_engine_init();
        if (_handle == IntPtr.Zero)
            throw new MiniaudioException("Failed to initialize miniaudio engine (no audio device?)");
    }

    /// <summary>The miniaudio version string (works without an audio device).</summary>
    public static string Version => Utf8ToString(NativeMethods.ma_wrap_version_string());

    public uint SampleRate => NativeMethods.ma_wrap_engine_get_sample_rate((void*)_handle);

    public void Start() => Check(NativeMethods.ma_wrap_engine_start((void*)_handle), "engine_start");
    public void Stop() => Check(NativeMethods.ma_wrap_engine_stop((void*)_handle), "engine_stop");
    public void SetMasterVolume(float volume) => NativeMethods.ma_wrap_engine_set_volume((void*)_handle, volume);

    /// <summary>
    /// Registers encoded audio bytes under <paramref name="name"/> so they can be loaded
    /// with <see cref="LoadSound"/> without a filesystem (the web path). The data is pinned
    /// and kept alive until <see cref="UnregisterMemory"/> or <see cref="Dispose"/>.
    /// </summary>
    public void RegisterMemory(string name, byte[] data)
    {
        var gch = GCHandle.Alloc(data, GCHandleType.Pinned);
        var namePtr = Marshal.StringToCoTaskMemUTF8(name);
        try
        {
            int r = NativeMethods.ma_wrap_register_memory(
                (void*)_handle, (byte*)namePtr, (void*)gch.AddrOfPinnedObject(), data.Length);
            Check(r, "register_memory");
            if (_pinned.TryGetValue(name, out var old)) old.Free();
            _pinned[name] = gch;
        }
        catch
        {
            gch.Free();
            throw;
        }
        finally
        {
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    public void UnregisterMemory(string name)
    {
        var namePtr = Marshal.StringToCoTaskMemUTF8(name);
        try { NativeMethods.ma_wrap_unregister_memory((void*)_handle, (byte*)namePtr); }
        finally { Marshal.FreeCoTaskMem(namePtr); }
        if (_pinned.Remove(name, out var gch)) gch.Free();
    }

    /// <summary>Loads a sound from a file path (native) or a registered virtual name (web).</summary>
    public Sound LoadSound(string nameOrPath, SoundFlags flags = SoundFlags.None)
    {
        var namePtr = Marshal.StringToCoTaskMemUTF8(nameOrPath);
        try
        {
            var sound = NativeMethods.ma_wrap_sound_load((void*)_handle, (byte*)namePtr, (uint)flags);
            if (sound == null)
                throw new MiniaudioException($"Failed to load sound '{nameOrPath}'");
            return new Sound((IntPtr)sound);
        }
        finally
        {
            Marshal.FreeCoTaskMem(namePtr);
        }
    }

    /// <summary>Registers <paramref name="data"/> under <paramref name="name"/> and loads it.</summary>
    public Sound LoadSoundFromMemory(string name, byte[] data, SoundFlags flags = SoundFlags.None)
    {
        RegisterMemory(name, data);
        return LoadSound(name, flags);
    }

    public void SetListenerPosition(float x, float y, float z, uint index = 0)
        => NativeMethods.ma_wrap_listener_set_position((void*)_handle, index, x, y, z);
    public void SetListenerDirection(float x, float y, float z, uint index = 0)
        => NativeMethods.ma_wrap_listener_set_direction((void*)_handle, index, x, y, z);
    public void SetListenerVelocity(float x, float y, float z, uint index = 0)
        => NativeMethods.ma_wrap_listener_set_velocity((void*)_handle, index, x, y, z);
    public void SetListenerWorldUp(float x, float y, float z, uint index = 0)
        => NativeMethods.ma_wrap_listener_set_world_up((void*)_handle, index, x, y, z);

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.ma_wrap_engine_uninit((void*)_handle);
            _handle = IntPtr.Zero;
        }
        foreach (var gch in _pinned.Values) gch.Free();
        _pinned.Clear();
        GC.SuppressFinalize(this);
    }

    internal static void Check(int result, string what)
    {
        if (result != 0) throw new MiniaudioException($"miniaudio call '{what}' failed", result);
    }

    internal static string Utf8ToString(byte* p)
    {
        if (p == null) return string.Empty;
        int len = 0;
        while (p[len] != 0) len++;
        return Encoding.UTF8.GetString(p, len);
    }
}
