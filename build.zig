const std = @import("std");

// Builds a single shared library (miniaudio + the ma_wrap curated ABI) for the
// selected target. Only the ma_wrap_* symbols are exported (everything else is
// hidden), giving consumers a tiny, stable surface.
//
//   zig build                                   # native host
//   zig build -Dtarget=x86_64-linux-gnu  -Doptimize=ReleaseFast
//   zig build -Dtarget=x86_64-windows-gnu -Doptimize=ReleaseFast
//   zig build -Dtarget=aarch64-macos     -Doptimize=ReleaseFast   # run on a Mac
//
// Output: zig-out/lib (and zig-out/bin for the .dll on Windows).
pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize = b.standardOptimizeOption(.{});

    const mod = b.createModule(.{
        .target = target,
        .optimize = optimize,
        .link_libc = true,
    });

    mod.addIncludePath(b.path("miniaudio"));
    mod.addCSourceFiles(.{
        .files = &.{
            "miniaudio/miniaudio.c",
            "bindings/ma_wrap.c",
        },
        .flags = &.{
            // Hide everything by default; only __attribute__((visibility("default")))
            // / __declspec(dllexport) (i.e. the ma_wrap_* functions) are exported.
            "-fvisibility=hidden",
            // We only ever decode/play — no need for the WAV/FLAC encoders.
            "-DMA_NO_ENCODING",
        },
    });

    // Per-platform link requirements (miniaudio runtime-links the backends via
    // dlopen/LoadLibrary, so the default build needs almost nothing):
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

    const lib = b.addLibrary(.{
        .name = "miniaudio",
        .root_module = mod,
        .linkage = .dynamic,
    });

    b.installArtifact(lib);
}
