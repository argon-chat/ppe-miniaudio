using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Miniaudio.Native;

// Maps the single DllImport name "miniaudio" to the correct per-OS file, probing
// the NuGet runtimes/{rid}/native convention next to the app. Exactly one resolver
// is registered per assembly (a second registration would throw).
internal static class NativeLibraryResolver
{
    private const string LibName = "miniaudio";
    private static int _registered;

    // The ModuleInitializer runs when this binding assembly is first used, which is
    // exactly when we want the resolver registered. CA2255 is advisory for libraries;
    // suppress it deliberately.
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Register()
#pragma warning restore CA2255
    {
        if (Interlocked.Exchange(ref _registered, 1) != 0) return;
        NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, Resolve);
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibName, StringComparison.Ordinal))
            return IntPtr.Zero;

        string fileName = NativeFileName();
        string rid = Rid();

        foreach (var dir in ProbeDirs(assembly))
        {
            string withRid = Path.Combine(dir, "runtimes", rid, "native", fileName);
            if (File.Exists(withRid) && NativeLibrary.TryLoad(withRid, out var h1)) return h1;

            string flat = Path.Combine(dir, fileName);
            if (File.Exists(flat) && NativeLibrary.TryLoad(flat, out var h2)) return h2;
        }

        // Last resort: let the OS loader search its default paths.
        return NativeLibrary.TryLoad(fileName, assembly, searchPath, out var h) ? h : IntPtr.Zero;
    }

    private static IEnumerable<string> ProbeDirs(Assembly assembly)
    {
        yield return AppContext.BaseDirectory;
        var asmDir = Path.GetDirectoryName(assembly.Location);
        if (!string.IsNullOrEmpty(asmDir)) yield return asmDir!;
    }

    private static string NativeFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "miniaudio.dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "libminiaudio.dylib";
        return "libminiaudio.so";
    }

    private static string Rid()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win"
                  : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx"
                  : "linux";
        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
        };
        return $"{os}-{arch}";
    }
}
