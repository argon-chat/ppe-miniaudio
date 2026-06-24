#!/usr/bin/env bash
# Builds the miniaudio WASM module with its built-in Emscripten AudioWorklet device
# backend. Requires emcc on PATH (emsdk, or the .NET wasm-tools Emscripten — see
# build-wasm.ps1 for the Windows/dotnet-pack path).
#
# Output (web/dist): miniaudio.mjs (ES6 factory), miniaudio.wasm, miniaudio.d.ts,
# plus the worklet/worker helper JS that emscripten emits.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/web/dist"
mkdir -p "$OUT"

emcc \
  "$ROOT/miniaudio/miniaudio.c" \
  "$ROOT/bindings/ma_wrap.c" \
  -I"$ROOT/miniaudio" \
  -O3 \
  -DMA_NO_ENCODING \
  -DMA_ENABLE_AUDIO_WORKLETS \
  -sAUDIO_WORKLET=1 \
  -sWASM_WORKERS=1 \
  -sASYNCIFY \
  -sMODULARIZE=1 \
  -sEXPORT_ES6=1 \
  -sEXPORT_NAME=createMiniaudio \
  -sALLOW_MEMORY_GROWTH=1 \
  -sEXPORTED_FUNCTIONS=_malloc,_free \
  -sEXPORTED_RUNTIME_METHODS=ccall,cwrap,UTF8ToString,stringToUTF8,lengthBytesUTF8,HEAPU8,HEAPF32 \
  --emit-tsd miniaudio.d.ts \
  -o "$OUT/miniaudio.mjs"

echo "built -> $OUT/miniaudio.mjs (+ .wasm, miniaudio.d.ts, worklet/worker JS)"
echo "NOTE: serve with COOP/COEP headers:"
echo "  Cross-Origin-Opener-Policy: same-origin"
echo "  Cross-Origin-Embedder-Policy: require-corp"
