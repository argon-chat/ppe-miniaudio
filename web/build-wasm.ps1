#Requires -Version 7
<#
  Builds the miniaudio WASM module on Windows using the Emscripten that ships with
  the .NET "wasm-tools" workload — no emsdk install needed.

  Prereq:  dotnet workload install wasm-tools
  Usage:   pwsh web/build-wasm.ps1
  Output:  web/dist/{miniaudio.mjs,.wasm,.d.ts,.aw.js,.ww.js}
#>
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot

function Find-Pack([string]$pattern) {
  foreach ($r in @("$env:ProgramFiles\dotnet\packs", "$env:USERPROFILE\.dotnet\packs")) {
    if (Test-Path $r) {
      $d = Get-ChildItem $r -Directory | Where-Object Name -like $pattern |
           Sort-Object Name -Descending | Select-Object -First 1
      if ($d) {
        return (Get-ChildItem $d.FullName -Directory | Sort-Object Name -Descending |
                Select-Object -First 1).FullName
      }
    }
  }
  return $null
}

$sdk  = Find-Pack 'Microsoft.NET.Runtime.Emscripten.*.Sdk.win-x64'
$node = Find-Pack 'Microsoft.NET.Runtime.Emscripten.*.Node.win-x64'
$py   = Find-Pack 'Microsoft.NET.Runtime.Emscripten.*.Python.win-x64'
if (-not ($sdk -and $node -and $py)) {
  throw "Emscripten packs not found. Run: dotnet workload install wasm-tools"
}

$python  = Join-Path $py 'tools\python.exe'
$emcc    = Join-Path $sdk 'tools\emscripten\emcc.py'
$nodeExe = Get-ChildItem (Join-Path $node 'tools') -Recurse -Filter node.exe |
           Select-Object -First 1 -ExpandProperty FullName

$work     = Join-Path $env:LOCALAPPDATA 'ppe-miniaudio'
$cacheDir = Join-Path $work 'emcache'
$cfg      = Join-Path $work 'emscripten_config.py'
$dist     = Join-Path $repo 'web\dist'
New-Item -ItemType Directory -Force $cacheDir, $dist | Out-Null

@"
import os
LLVM_ROOT = r'$(Join-Path $sdk 'tools\bin')'
NODE_JS = r'$nodeExe'
BINARYEN_ROOT = r'$(Join-Path $sdk 'tools')'
FROZEN_CACHE = False
CACHE = r'$cacheDir'
COMPILER_ENGINE = NODE_JS
JS_ENGINES = [NODE_JS]
"@ | Set-Content -Encoding ascii $cfg

$env:EM_CONFIG = $cfg

$emccArgs = @(
  (Join-Path $repo 'miniaudio\miniaudio.c'),
  (Join-Path $repo 'bindings\ma_wrap.c'),
  "-I$(Join-Path $repo 'miniaudio')",
  '-O3', '-DMA_NO_ENCODING', '-DMA_ENABLE_AUDIO_WORKLETS',
  '-sAUDIO_WORKLET=1', '-sWASM_WORKERS=1', '-sASYNCIFY',
  '-sMODULARIZE=1', '-sEXPORT_ES6=1', '-sEXPORT_NAME=createMiniaudio',
  '-sALLOW_MEMORY_GROWTH=1',
  '-sEXPORTED_FUNCTIONS=_malloc,_free',
  '-sEXPORTED_RUNTIME_METHODS=ccall,cwrap,UTF8ToString,stringToUTF8,lengthBytesUTF8,HEAPU8,HEAPF32',
  '--emit-tsd', 'miniaudio.d.ts',
  '-o', (Join-Path $dist 'miniaudio.mjs')
)

Write-Host "emcc ($sdk)..."
& $python $emcc @emccArgs
if ($LASTEXITCODE -ne 0) { throw "emcc failed ($LASTEXITCODE)" }
Write-Host "built -> $dist"
Write-Host "Serve cross-origin-isolated: COOP=same-origin, COEP=require-corp"
