# ppe-miniaudio

[miniaudio](https://github.com/mackron/miniaudio) packaged for a 2D game engine:
a single curated C ABI compiled to **native shared libraries** (Windows / Linux / macOS,
consumed from **C#**) and to **WebAssembly** (consumed from **TypeScript**), with
**auto-generated C# bindings** and **generated TypeScript typings**.

Scope: high-level **playback + spatialization** (engine, sounds, 3D positioning, listener,
attenuation, volume, pitch, looping). No capture/microphone.

## Why this shape

- **One curated wrapper.** [`bindings/ma_wrap.h`](bindings/ma_wrap.h) exposes ~32 flat
  `extern "C"` functions over `ma_engine`/`ma_sound` using opaque `void*` handles — instead of
  binding miniaudio's 2,300+ symbols. This single header is the source of truth for **both** the
  C# and TypeScript bindings, and only `ma_wrap_*` is exported (everything else is hidden).
- **Zig builds the native libs.** One `build.zig` cross-compiles Windows + Linux from any host;
  macOS is built natively (CoreAudio headers are needed at compile time).
- **Emscripten owns web audio.** The web build uses miniaudio's built-in AudioWorklet device
  backend, so the engine code path is identical native ↔ web.

## Layout

```
build.zig                       native shared lib (miniaudio + ma_wrap)
bindings/ma_wrap.{h,c}          curated flat C ABI — the single source of truth
csharp/
  codegen/                      Rust crate: bindgen + csbindgen → NativeMethods.g.cs
  Miniaudio/                    C# library (generated P/Invoke + resolver + Engine/Sound API)
  Miniaudio.SmokeTest/          console smoke test (interop gate + playback)
web/
  build-wasm.sh / .ps1          emcc build (.ps1 uses the .NET wasm-tools Emscripten)
  src/ma.ts                     typed wrapper (Engine/Sound) over the WASM module
runtimes/{rid}/native/          built native libs, for NuGet packaging
```

## Prerequisites

| Tool | Used for | Notes |
|---|---|---|
| [Zig](https://ziglang.org) 0.16 | native libs | cross-compiles win64 + linux64 from one host |
| .NET 8+ SDK | C# library & test | |
| Rust + cargo | C# binding codegen only | not needed to *use* the library |
| LLVM / libclang | csbindgen (bindgen) | Windows: `winget install LLVM.LLVM`, set `LIBCLANG_PATH` |
| Emscripten | WASM build | emsdk, **or** `dotnet workload install wasm-tools` (see `build-wasm.ps1`) |
| Node + TypeScript | TS typings/typecheck | |

Clone with submodules: `git submodule update --init --recursive`.

## Build

### Native (Zig)
```sh
zig build -Doptimize=ReleaseFast                              # host
zig build -Doptimize=ReleaseFast -Dtarget=x86_64-linux-gnu   # → libminiaudio.so
zig build -Doptimize=ReleaseFast -Dtarget=x86_64-windows-gnu # → miniaudio.dll
zig build -Doptimize=ReleaseFast -Dtarget=aarch64-macos      # → libminiaudio.dylib (run on a Mac)
```
Copy the output into `runtimes/<rid>/native/` (the C# project globs that tree). Link requirements
are handled by `build.zig`: Linux `-ldl -lpthread -lm`; Windows/macOS link nothing by default
(miniaudio runtime-links the backends).

### C# bindings (regenerate after changing `ma_wrap.h`)
```sh
# Windows: set LIBCLANG_PATH first, e.g. $env:LIBCLANG_PATH = 'C:\Program Files\LLVM\bin'
cargo build --manifest-path csharp/codegen/Cargo.toml   # → csharp/Miniaudio/NativeMethods.g.cs
```
`NativeMethods.g.cs` is committed, so consumers of the C# library don't need Rust.

### WASM
```sh
bash web/build-wasm.sh        # emsdk on PATH
# or on Windows with the .NET wasm-tools workload:
pwsh web/build-wasm.ps1
```
Outputs to `web/dist/`: `miniaudio.mjs` (ES6 factory), `miniaudio.wasm`, `miniaudio.d.ts`, and the
emscripten AudioWorklet/Wasm-Worker helper JS.

> The web page **must** be served cross-origin-isolated (the AudioWorklet uses Wasm Workers):
> `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: require-corp`.

## Use

### C#
```csharp
using Miniaudio;

using var engine = new MiniaudioEngine();   // opens the default playback device
engine.Start();

using var shot = engine.LoadSound("shot.flac", SoundFlags.Decode);
shot.SpatializationEnabled = true;
shot.SetPosition(5, 0, 0);                  // to the listener's right
engine.SetListenerPosition(0, 0, 0);
shot.Play();
```
Native libraries are resolved per-OS via a `DllImportResolver` from `runtimes/{rid}/native/`.

### TypeScript
```ts
import createMiniaudio from './miniaudio.mjs';      // emscripten factory
import { MiniaudioEngine, SoundFlags } from './ma.js';

const engine = await MiniaudioEngine.create(createMiniaudio);
playButton.onclick = async () => {
  engine.start();                                   // MUST be a user gesture
  const bytes = new Uint8Array(await (await fetch('shot.flac')).arrayBuffer());
  const shot = engine.loadFromMemory('shot', bytes, SoundFlags.Decode);
  shot.setPosition(5, 0, 0);
  shot.play();
};
```

## Verification

- **Native/C#:** `dotnet run --project csharp/Miniaudio.SmokeTest` — prints the miniaudio version
  (interop gate) and plays a bundled FLAC, orbiting the listener to demonstrate spatialization.
  Headless machines skip playback but still assert interop.
- **WASM/TS:** `cd web && npm run typecheck`; serve a COOP/COEP page and play on a click.

CI ([`.github/workflows/build.yml`](.github/workflows/build.yml)) builds all three native libs +
WASM, runs the smoke test on each OS, typechecks the TS, and fails if the committed C# bindings
drift from `ma_wrap.h`.
