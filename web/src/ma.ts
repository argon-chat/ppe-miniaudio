/**
 * Typed, ergonomic wrapper over the miniaudio WASM module (the ma_wrap flat C ABI).
 *
 * miniaudio owns the browser audio device via its built-in Emscripten AudioWorklet
 * backend, so this file is purely the control API — there is no hand-written worklet.
 *
 * Usage:
 *   import createMiniaudio from './miniaudio.mjs';   // emscripten MODULARIZE factory
 *   import { MiniaudioEngine, SoundFlags } from './ma.js';
 *
 *   const engine = await MiniaudioEngine.create(createMiniaudio);
 *   playButton.onclick = async () => {
 *     engine.start();                                 // MUST be a user gesture
 *     const bytes = new Uint8Array(await (await fetch('shot.flac')).arrayBuffer());
 *     const shot = engine.loadFromMemory('shot', bytes, SoundFlags.Decode);
 *     shot.setPosition(5, 0, 0);
 *     shot.play();
 *   };
 *
 * The page must be served cross-origin-isolated (COOP/COEP) for the AudioWorklet's
 * Wasm Workers to start.
 */

/** Minimal view of the Emscripten module we depend on (subset of the generated .d.ts). */
export interface EmscriptenModule {
  _malloc(size: number): number;
  _free(ptr: number): void;
  HEAPU8: Uint8Array;
  HEAPF32: Float32Array;
  UTF8ToString(ptr: number): string;
  stringToUTF8(str: string, outPtr: number, maxBytesToWrite: number): void;
  lengthBytesUTF8(str: string): number;
  cwrap(ident: string, returnType: string | null, argTypes: string[]): (...args: number[]) => number;
}

/** The MODULARIZE + EXPORT_ES6 factory exported by miniaudio.mjs. */
export type MiniaudioModuleFactory = (moduleArg?: Record<string, unknown>) => Promise<EmscriptenModule>;

/** Mirrors ma_sound_flags. */
export enum SoundFlags {
  None = 0,
  Stream = 0x00000001,
  Decode = 0x00000002,
  Async = 0x00000004,
  WaitInit = 0x00000008,
  Looping = 0x00000020,
  NoPitch = 0x00002000,
  NoSpatialization = 0x00004000,
}

/** Mirrors ma_attenuation_model. */
export enum AttenuationModel {
  None = 0,
  Inverse = 1,
  Linear = 2,
  Exponential = 3,
}

export class MiniaudioError extends Error {
  constructor(public readonly op: string, public readonly result: number) {
    super(`miniaudio '${op}' failed (ma_result=${result})`);
    this.name = 'MiniaudioError';
  }
}

type Ptr = number;

interface Native {
  version_string(): Ptr;
  engine_init(): Ptr;
  engine_uninit(e: Ptr): void;
  engine_start(e: Ptr): number;
  engine_stop(e: Ptr): number;
  engine_set_volume(e: Ptr, v: number): void;
  engine_get_sample_rate(e: Ptr): number;
  register_memory(e: Ptr, name: Ptr, data: Ptr, size: number): number;
  unregister_memory(e: Ptr, name: Ptr): number;
  sound_load(e: Ptr, name: Ptr, flags: number): Ptr;
  sound_unload(s: Ptr): void;
  sound_play(s: Ptr): number;
  sound_stop(s: Ptr): number;
  sound_set_volume(s: Ptr, v: number): void;
  sound_set_pitch(s: Ptr, v: number): void;
  sound_set_pan(s: Ptr, v: number): void;
  sound_set_looping(s: Ptr, l: number): void;
  sound_is_playing(s: Ptr): number;
  sound_at_end(s: Ptr): number;
  sound_seek_to_frame(s: Ptr, f: number): number;
  sound_set_spatialization_enabled(s: Ptr, e: number): void;
  sound_set_position(s: Ptr, x: number, y: number, z: number): void;
  sound_set_velocity(s: Ptr, x: number, y: number, z: number): void;
  sound_set_attenuation_model(s: Ptr, m: number): void;
  sound_set_rolloff(s: Ptr, r: number): void;
  sound_set_min_distance(s: Ptr, d: number): void;
  sound_set_max_distance(s: Ptr, d: number): void;
  sound_set_doppler_factor(s: Ptr, f: number): void;
  listener_set_position(e: Ptr, i: number, x: number, y: number, z: number): void;
  listener_set_direction(e: Ptr, i: number, x: number, y: number, z: number): void;
  listener_set_velocity(e: Ptr, i: number, x: number, y: number, z: number): void;
  listener_set_world_up(e: Ptr, i: number, x: number, y: number, z: number): void;
}

const NUM = 'number';

function bind(m: EmscriptenModule): Native {
  const c = (name: string, ret: string | null, args: string[]) =>
    m.cwrap('ma_wrap_' + name, ret, args) as never;
  return {
    version_string: c('version_string', NUM, []),
    engine_init: c('engine_init', NUM, []),
    engine_uninit: c('engine_uninit', null, [NUM]),
    engine_start: c('engine_start', NUM, [NUM]),
    engine_stop: c('engine_stop', NUM, [NUM]),
    engine_set_volume: c('engine_set_volume', null, [NUM, NUM]),
    engine_get_sample_rate: c('engine_get_sample_rate', NUM, [NUM]),
    register_memory: c('register_memory', NUM, [NUM, NUM, NUM, NUM]),
    unregister_memory: c('unregister_memory', NUM, [NUM, NUM]),
    sound_load: c('sound_load', NUM, [NUM, NUM, NUM]),
    sound_unload: c('sound_unload', null, [NUM]),
    sound_play: c('sound_play', NUM, [NUM]),
    sound_stop: c('sound_stop', NUM, [NUM]),
    sound_set_volume: c('sound_set_volume', null, [NUM, NUM]),
    sound_set_pitch: c('sound_set_pitch', null, [NUM, NUM]),
    sound_set_pan: c('sound_set_pan', null, [NUM, NUM]),
    sound_set_looping: c('sound_set_looping', null, [NUM, NUM]),
    sound_is_playing: c('sound_is_playing', NUM, [NUM]),
    sound_at_end: c('sound_at_end', NUM, [NUM]),
    sound_seek_to_frame: c('sound_seek_to_frame', NUM, [NUM, NUM]),
    sound_set_spatialization_enabled: c('sound_set_spatialization_enabled', null, [NUM, NUM]),
    sound_set_position: c('sound_set_position', null, [NUM, NUM, NUM, NUM]),
    sound_set_velocity: c('sound_set_velocity', null, [NUM, NUM, NUM, NUM]),
    sound_set_attenuation_model: c('sound_set_attenuation_model', null, [NUM, NUM]),
    sound_set_rolloff: c('sound_set_rolloff', null, [NUM, NUM]),
    sound_set_min_distance: c('sound_set_min_distance', null, [NUM, NUM]),
    sound_set_max_distance: c('sound_set_max_distance', null, [NUM, NUM]),
    sound_set_doppler_factor: c('sound_set_doppler_factor', null, [NUM, NUM]),
    listener_set_position: c('listener_set_position', null, [NUM, NUM, NUM, NUM, NUM]),
    listener_set_direction: c('listener_set_direction', null, [NUM, NUM, NUM, NUM, NUM]),
    listener_set_velocity: c('listener_set_velocity', null, [NUM, NUM, NUM, NUM, NUM]),
    listener_set_world_up: c('listener_set_world_up', null, [NUM, NUM, NUM, NUM, NUM]),
  };
}

function check(result: number, op: string): void {
  if (result !== 0) throw new MiniaudioError(op, result);
}

function withCString<T>(m: EmscriptenModule, s: string, fn: (ptr: number) => T): T {
  const len = m.lengthBytesUTF8(s) + 1;
  const ptr = m._malloc(len);
  try {
    m.stringToUTF8(s, ptr, len);
    return fn(ptr);
  } finally {
    m._free(ptr);
  }
}

export class MiniaudioEngine {
  private readonly registered = new Map<string, number>();

  private constructor(
    private readonly m: EmscriptenModule,
    private readonly n: Native,
    private readonly handle: number,
  ) {}

  static async create(factory: MiniaudioModuleFactory): Promise<MiniaudioEngine> {
    const m = await factory();
    const n = bind(m);
    const handle = n.engine_init();
    if (handle === 0) throw new Error('ma_wrap_engine_init failed');
    return new MiniaudioEngine(m, n, handle);
  }

  get version(): string {
    return this.m.UTF8ToString(this.n.version_string());
  }

  get sampleRate(): number {
    return this.n.engine_get_sample_rate(this.handle);
  }

  /** Begin audio output. On the web this MUST be called from a user gesture. */
  start(): void {
    check(this.n.engine_start(this.handle), 'engine_start');
  }

  stop(): void {
    check(this.n.engine_stop(this.handle), 'engine_stop');
  }

  setMasterVolume(volume: number): void {
    this.n.engine_set_volume(this.handle, volume);
  }

  /**
   * Registers encoded audio bytes under `name`. The bytes are copied into wasm memory
   * and kept alive (miniaudio does not copy) until `unregisterMemory`/`dispose`.
   */
  registerMemory(name: string, data: Uint8Array): void {
    const ptr = this.m._malloc(data.length);
    this.m.HEAPU8.set(data, ptr);
    const r = withCString(this.m, name, (np) => this.n.register_memory(this.handle, np, ptr, data.length));
    if (r !== 0) {
      this.m._free(ptr);
      throw new MiniaudioError('register_memory', r);
    }
    const old = this.registered.get(name);
    if (old !== undefined) this.m._free(old);
    this.registered.set(name, ptr);
  }

  unregisterMemory(name: string): void {
    withCString(this.m, name, (np) => this.n.unregister_memory(this.handle, np));
    const ptr = this.registered.get(name);
    if (ptr !== undefined) {
      this.m._free(ptr);
      this.registered.delete(name);
    }
  }

  /** Load by registered virtual name (web) or file path (when a VFS is mounted). */
  load(nameOrPath: string, flags: SoundFlags = SoundFlags.None): Sound {
    const h = withCString(this.m, nameOrPath, (np) => this.n.sound_load(this.handle, np, flags >>> 0));
    if (h === 0) throw new Error(`failed to load sound '${nameOrPath}'`);
    return new Sound(this.n, h);
  }

  loadFromMemory(name: string, data: Uint8Array, flags: SoundFlags = SoundFlags.None): Sound {
    this.registerMemory(name, data);
    return this.load(name, flags);
  }

  setListenerPosition(x: number, y: number, z: number, index = 0): void {
    this.n.listener_set_position(this.handle, index, x, y, z);
  }
  setListenerDirection(x: number, y: number, z: number, index = 0): void {
    this.n.listener_set_direction(this.handle, index, x, y, z);
  }
  setListenerVelocity(x: number, y: number, z: number, index = 0): void {
    this.n.listener_set_velocity(this.handle, index, x, y, z);
  }
  setListenerWorldUp(x: number, y: number, z: number, index = 0): void {
    this.n.listener_set_world_up(this.handle, index, x, y, z);
  }

  dispose(): void {
    this.n.engine_uninit(this.handle);
    for (const ptr of this.registered.values()) this.m._free(ptr);
    this.registered.clear();
  }
}

export class Sound {
  /** @internal */
  constructor(private readonly n: Native, private readonly handle: number) {}

  play(): void {
    check(this.n.sound_play(this.handle), 'sound_play');
  }
  stop(): void {
    check(this.n.sound_stop(this.handle), 'sound_stop');
  }

  get isPlaying(): boolean {
    return this.n.sound_is_playing(this.handle) !== 0;
  }
  get atEnd(): boolean {
    return this.n.sound_at_end(this.handle) !== 0;
  }

  set volume(v: number) {
    this.n.sound_set_volume(this.handle, v);
  }
  set pitch(v: number) {
    this.n.sound_set_pitch(this.handle, v);
  }
  set pan(v: number) {
    this.n.sound_set_pan(this.handle, v);
  }
  set looping(v: boolean) {
    this.n.sound_set_looping(this.handle, v ? 1 : 0);
  }

  seekToFrame(frame: number): void {
    check(this.n.sound_seek_to_frame(this.handle, frame >>> 0), 'sound_seek');
  }

  // ---- spatialization ----
  set spatializationEnabled(v: boolean) {
    this.n.sound_set_spatialization_enabled(this.handle, v ? 1 : 0);
  }
  setPosition(x: number, y: number, z: number): void {
    this.n.sound_set_position(this.handle, x, y, z);
  }
  setVelocity(x: number, y: number, z: number): void {
    this.n.sound_set_velocity(this.handle, x, y, z);
  }
  set attenuationModel(m: AttenuationModel) {
    this.n.sound_set_attenuation_model(this.handle, m);
  }
  set rolloff(r: number) {
    this.n.sound_set_rolloff(this.handle, r);
  }
  set minDistance(d: number) {
    this.n.sound_set_min_distance(this.handle, d);
  }
  set maxDistance(d: number) {
    this.n.sound_set_max_distance(this.handle, d);
  }
  set dopplerFactor(f: number) {
    this.n.sound_set_doppler_factor(this.handle, f);
  }

  dispose(): void {
    this.n.sound_unload(this.handle);
  }
}
