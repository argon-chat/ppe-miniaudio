const std = @import("std");

// Builds the miniaudio + ma_wrap curated ABI for the selected target, in TWO flavours:
//
//   * a SHARED library  (zig-out/bin/<lib>.dll + zig-out/lib import lib / .so / .dylib)
//       → loaded at runtime via [DllImport("miniaudio")]; this is what ships in
//         runtimes/<rid>/native and what the JIT / framework / self-contained builds use.
//   * a STATIC library  (zig-out/lib/miniaudio-static.lib | libminiaudio-static.a)
//       → linked straight into a host image (NativeAOT) via <NativeLibrary>; no .dll shipped.
//         Built with -DMA_WRAP_STATIC so the ma_wrap_* symbols are NOT dll-exported.
//
// Only the ma_wrap_* symbols are visible; everything else is hidden — a tiny, stable surface.
//
//   zig build                                    # native host (both flavours)
//   zig build -Dtarget=x86_64-windows-msvc -Doptimize=ReleaseFast   # AOT-linkable static on Win
//   zig build -Dtarget=x86_64-linux-gnu    -Doptimize=ReleaseFast
//   zig build -Dtarget=aarch64-macos       -Doptimize=ReleaseFast   # run on a Mac
//
// NOTE: for the AOT static lib, target the SAME C ABI as the host linker — on Windows that's
// `-msvc` (NativeAOT links with MSVC's link.exe + UCRT); a `-gnu` archive would clash with it.

const sources = [_][]const u8{
    "miniaudio/miniaudio.c",
    "bindings/ma_wrap.c",
};

const base_flags = [_][]const u8{
    // Hide everything by default; only the ma_wrap_* surface is exported (shared) / defined (static).
    "-fvisibility=hidden",
    // We only ever decode/play — no need for the WAV/FLAC encoders.
    "-DMA_NO_ENCODING",
};

pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize = b.standardOptimizeOption(.{});

    // ---- shared library (runtime DllImport) ----
    {
        const mod = makeModule(b, target, optimize, false);
        const lib = b.addLibrary(.{ .name = "miniaudio", .root_module = mod, .linkage = .dynamic });
        b.installArtifact(lib);
    }

    // ---- static library (NativeAOT link target) ----
    {
        const mod = makeModule(b, target, optimize, true);
        // Distinct name so the static archive doesn't clobber the shared lib's import lib in zig-out/lib.
        const lib = b.addLibrary(.{ .name = "miniaudio-static", .root_module = mod, .linkage = .static });
        b.installArtifact(lib);
    }
}

fn makeModule(
    b: *std.Build,
    target: std.Build.ResolvedTarget,
    optimize: std.builtin.OptimizeMode,
    static: bool,
) *std.Build.Module {
    const mod = b.createModule(.{
        .target = target,
        .optimize = optimize,
        .link_libc = true,
    });

    mod.addIncludePath(b.path("miniaudio"));
    mod.addCSourceFiles(.{
        .files = &sources,
        .flags = if (static) &(base_flags ++ [_][]const u8{"-DMA_WRAP_STATIC"}) else &base_flags,
    });

    // Per-platform link requirements (miniaudio runtime-links the backends via dlopen/LoadLibrary,
    // so the default build needs almost nothing):
    //   Windows : nothing (ole32/winmm are runtime-linked)
    //   macOS   : nothing (CoreAudio is runtime-linked)
    //   Linux   : -ldl -lpthread -lm
    switch (target.result.os.tag) {
        .linux => {
            mod.linkSystemLibrary("dl", .{});
            mod.linkSystemLibrary("pthread", .{});
            mod.linkSystemLibrary("m", .{});
        },
        else => {},
    }

    return mod;
}
