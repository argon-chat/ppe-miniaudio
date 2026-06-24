using Miniaudio;

// 1) Interop gate: works on any platform, no audio device required.
Console.WriteLine($"miniaudio version: {MiniaudioEngine.Version}");
if (string.IsNullOrWhiteSpace(MiniaudioEngine.Version))
{
    Console.Error.WriteLine("FAIL: empty version string — native interop is broken.");
    return 1;
}

// 2) Playback + spatialization (best-effort; headless CI runners have no device).
string flac = Path.Combine(AppContext.BaseDirectory, "data", "16-44100-stereo.flac");

MiniaudioEngine engine;
try
{
    engine = new MiniaudioEngine();
}
catch (MiniaudioException ex)
{
    Console.WriteLine($"[skip] no audio device ({ex.Message}); interop OK, skipping playback.");
    return 0;
}

using (engine)
{
    Console.WriteLine($"engine sample rate: {engine.SampleRate}");
    engine.Start();

    using var sound = engine.LoadSound(flac, SoundFlags.Decode);
    sound.SpatializationEnabled = true;
    sound.AttenuationModel = AttenuationModel.Inverse;
    engine.SetListenerPosition(0, 0, 0);
    sound.Play();

    Console.WriteLine("playing ~3s, orbiting the listener (you should hear it pan)...");
    for (int i = 0; i < 30 && sound.IsPlaying; i++)
    {
        float t = i / 30f * MathF.Tau;
        sound.SetPosition(MathF.Sin(t) * 5f, 0f, MathF.Cos(t) * 5f);
        Thread.Sleep(100);
    }
}

Console.WriteLine("done.");
return 0;
