/*
 * ma_wrap — a small, flat, blittable C ABI over miniaudio's high-level engine API.
 *
 * This is the single source of truth for ALL bindings (C# via csbindgen, TypeScript
 * via emscripten --emit-tsd). It intentionally exposes only opaque `void*` handles and
 * primitive parameters so it marshals cleanly across P/Invoke and the wasm boundary —
 * no by-value structs, no anonymous unions, no 64-bit ints across the boundary.
 *
 * Scope: playback + spatialization for a 2D game engine. No capture/microphone.
 *
 * The wrapper is device-based on every platform. On native it opens the default
 * playback device; on the web (emscripten) the "device" is miniaudio's AudioWorklet
 * backend. The engine is created with noAutoStart, so the caller must invoke
 * ma_wrap_engine_start() explicitly (on web this MUST happen inside a user gesture).
 */
#ifndef MA_WRAP_H
#define MA_WRAP_H

#if defined(__EMSCRIPTEN__)
#  include <emscripten.h>
#  define MA_WRAP_API EMSCRIPTEN_KEEPALIVE
#elif defined(_WIN32)
#  define MA_WRAP_API __declspec(dllexport)
#else
#  define MA_WRAP_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Opaque handles. Treat as IntPtr (C#) / number (JS). Never dereference. */
typedef void* ma_engine_handle;
typedef void* ma_sound_handle;

/* All int results are miniaudio ma_result codes (0 == MA_SUCCESS). */

/* ---- version ---- */
MA_WRAP_API const char* ma_wrap_version_string(void);

/* ---- engine lifecycle ---- */
MA_WRAP_API ma_engine_handle ma_wrap_engine_init(void);
MA_WRAP_API void             ma_wrap_engine_uninit(ma_engine_handle engine);
MA_WRAP_API int              ma_wrap_engine_start(ma_engine_handle engine);
MA_WRAP_API int              ma_wrap_engine_stop(ma_engine_handle engine);
MA_WRAP_API void             ma_wrap_engine_set_volume(ma_engine_handle engine, float volume);
MA_WRAP_API unsigned int     ma_wrap_engine_get_sample_rate(ma_engine_handle engine);

/* ---- in-memory asset registration (used on the web, where there is no filesystem) ----
 * The data is NOT copied: the buffer must stay valid until ma_wrap_unregister_memory.
 * After registering, load it by passing `name` to ma_wrap_sound_load. */
MA_WRAP_API int ma_wrap_register_memory(ma_engine_handle engine, const char* name, const void* data, int size);
MA_WRAP_API int ma_wrap_unregister_memory(ma_engine_handle engine, const char* name);

/* ---- sounds (name = real file path on native, or a registered virtual name) ----
 * `flags` is a bitmask of ma_sound_flags (e.g. STREAM=1, DECODE=2, ASYNC=4,
 * NO_SPATIALIZATION=16384). Pass 0 for the common case. */
MA_WRAP_API ma_sound_handle ma_wrap_sound_load(ma_engine_handle engine, const char* name, unsigned int flags);
MA_WRAP_API void            ma_wrap_sound_unload(ma_sound_handle sound);
MA_WRAP_API int             ma_wrap_sound_play(ma_sound_handle sound);
MA_WRAP_API int             ma_wrap_sound_stop(ma_sound_handle sound);
MA_WRAP_API void            ma_wrap_sound_set_volume(ma_sound_handle sound, float volume);
MA_WRAP_API void            ma_wrap_sound_set_pitch(ma_sound_handle sound, float pitch);
MA_WRAP_API void            ma_wrap_sound_set_pan(ma_sound_handle sound, float pan);
MA_WRAP_API void            ma_wrap_sound_set_looping(ma_sound_handle sound, int looping);
MA_WRAP_API int             ma_wrap_sound_is_playing(ma_sound_handle sound);
MA_WRAP_API int             ma_wrap_sound_at_end(ma_sound_handle sound);
/* frame index is 32-bit (≈12h at 48kHz) to stay blittable across the wasm boundary. */
MA_WRAP_API int             ma_wrap_sound_seek_to_frame(ma_sound_handle sound, unsigned int frame);

/* ---- spatialization ---- */
MA_WRAP_API void ma_wrap_sound_set_spatialization_enabled(ma_sound_handle sound, int enabled);
MA_WRAP_API void ma_wrap_sound_set_position(ma_sound_handle sound, float x, float y, float z);
MA_WRAP_API void ma_wrap_sound_set_velocity(ma_sound_handle sound, float x, float y, float z);
/* model: 0=none, 1=inverse, 2=linear, 3=exponential (ma_attenuation_model). */
MA_WRAP_API void ma_wrap_sound_set_attenuation_model(ma_sound_handle sound, int model);
MA_WRAP_API void ma_wrap_sound_set_rolloff(ma_sound_handle sound, float rolloff);
MA_WRAP_API void ma_wrap_sound_set_min_distance(ma_sound_handle sound, float distance);
MA_WRAP_API void ma_wrap_sound_set_max_distance(ma_sound_handle sound, float distance);
MA_WRAP_API void ma_wrap_sound_set_doppler_factor(ma_sound_handle sound, float factor);

/* ---- listener ---- */
MA_WRAP_API void ma_wrap_listener_set_position(ma_engine_handle engine, unsigned int index, float x, float y, float z);
MA_WRAP_API void ma_wrap_listener_set_direction(ma_engine_handle engine, unsigned int index, float x, float y, float z);
MA_WRAP_API void ma_wrap_listener_set_velocity(ma_engine_handle engine, unsigned int index, float x, float y, float z);
MA_WRAP_API void ma_wrap_listener_set_world_up(ma_engine_handle engine, unsigned int index, float x, float y, float z);

#ifdef __cplusplus
}
#endif

#endif /* MA_WRAP_H */
