/*
 * Implementation of the ma_wrap flat C ABI. Includes the full miniaudio header
 * (declarations only — the implementation is compiled separately from miniaudio.c)
 * and casts the opaque void* handles back to the real miniaudio types.
 *
 * Handles are heap-allocated with ma_malloc so the binding side only ever holds a
 * pointer-sized token; the struct layout never crosses the ABI boundary.
 */
#include "miniaudio.h"
#include "ma_wrap.h"

#define ENGINE(h) ((ma_engine*)(h))
#define SOUND(h)  ((ma_sound*)(h))

/* ---- version ---- */
const char* ma_wrap_version_string(void)
{
    return ma_version_string();
}

/* ---- engine lifecycle ---- */
ma_engine_handle ma_wrap_engine_init(void)
{
    ma_engine_config config = ma_engine_config_init();
    ma_engine* pEngine;

    /* Require an explicit start so the web build can begin audio inside a user gesture. */
    config.noAutoStart = MA_TRUE;

    pEngine = (ma_engine*)ma_malloc(sizeof(*pEngine), NULL);
    if (pEngine == NULL) {
        return NULL;
    }

    if (ma_engine_init(&config, pEngine) != MA_SUCCESS) {
        ma_free(pEngine, NULL);
        return NULL;
    }

    return (ma_engine_handle)pEngine;
}

void ma_wrap_engine_uninit(ma_engine_handle engine)
{
    if (engine == NULL) {
        return;
    }
    ma_engine_uninit(ENGINE(engine));
    ma_free(engine, NULL);
}

int ma_wrap_engine_start(ma_engine_handle engine)
{
    return (int)ma_engine_start(ENGINE(engine));
}

int ma_wrap_engine_stop(ma_engine_handle engine)
{
    return (int)ma_engine_stop(ENGINE(engine));
}

void ma_wrap_engine_set_volume(ma_engine_handle engine, float volume)
{
    ma_engine_set_volume(ENGINE(engine), volume);
}

unsigned int ma_wrap_engine_get_sample_rate(ma_engine_handle engine)
{
    return (unsigned int)ma_engine_get_sample_rate(ENGINE(engine));
}

/* ---- in-memory asset registration ---- */
int ma_wrap_register_memory(ma_engine_handle engine, const char* name, const void* data, int size)
{
    ma_resource_manager* pRM = ma_engine_get_resource_manager(ENGINE(engine));
    if (pRM == NULL) {
        return (int)MA_INVALID_OPERATION;
    }
    return (int)ma_resource_manager_register_encoded_data(pRM, name, data, (size_t)size);
}

int ma_wrap_unregister_memory(ma_engine_handle engine, const char* name)
{
    ma_resource_manager* pRM = ma_engine_get_resource_manager(ENGINE(engine));
    if (pRM == NULL) {
        return (int)MA_INVALID_OPERATION;
    }
    return (int)ma_resource_manager_unregister_data(pRM, name);
}

/* ---- sounds ---- */
ma_sound_handle ma_wrap_sound_load(ma_engine_handle engine, const char* name, unsigned int flags)
{
    ma_sound* pSound = (ma_sound*)ma_malloc(sizeof(*pSound), NULL);
    if (pSound == NULL) {
        return NULL;
    }

    if (ma_sound_init_from_file(ENGINE(engine), name, flags, NULL, NULL, pSound) != MA_SUCCESS) {
        ma_free(pSound, NULL);
        return NULL;
    }

    return (ma_sound_handle)pSound;
}

void ma_wrap_sound_unload(ma_sound_handle sound)
{
    if (sound == NULL) {
        return;
    }
    ma_sound_uninit(SOUND(sound));
    ma_free(sound, NULL);
}

int ma_wrap_sound_play(ma_sound_handle sound)
{
    return (int)ma_sound_start(SOUND(sound));
}

int ma_wrap_sound_stop(ma_sound_handle sound)
{
    return (int)ma_sound_stop(SOUND(sound));
}

void ma_wrap_sound_set_volume(ma_sound_handle sound, float volume)
{
    ma_sound_set_volume(SOUND(sound), volume);
}

void ma_wrap_sound_set_pitch(ma_sound_handle sound, float pitch)
{
    ma_sound_set_pitch(SOUND(sound), pitch);
}

void ma_wrap_sound_set_pan(ma_sound_handle sound, float pan)
{
    ma_sound_set_pan(SOUND(sound), pan);
}

void ma_wrap_sound_set_looping(ma_sound_handle sound, int looping)
{
    ma_sound_set_looping(SOUND(sound), looping ? MA_TRUE : MA_FALSE);
}

int ma_wrap_sound_is_playing(ma_sound_handle sound)
{
    return (int)ma_sound_is_playing(SOUND(sound));
}

int ma_wrap_sound_at_end(ma_sound_handle sound)
{
    return (int)ma_sound_at_end(SOUND(sound));
}

int ma_wrap_sound_seek_to_frame(ma_sound_handle sound, unsigned int frame)
{
    return (int)ma_sound_seek_to_pcm_frame(SOUND(sound), (ma_uint64)frame);
}

void ma_wrap_sound_set_fade(ma_sound_handle sound, float volumeBeg, float volumeEnd, unsigned int milliseconds)
{
    ma_sound_set_fade_in_milliseconds(SOUND(sound), volumeBeg, volumeEnd, (ma_uint64)milliseconds);
}

/* ---- spatialization ---- */
void ma_wrap_sound_set_spatialization_enabled(ma_sound_handle sound, int enabled)
{
    ma_sound_set_spatialization_enabled(SOUND(sound), enabled ? MA_TRUE : MA_FALSE);
}

void ma_wrap_sound_set_position(ma_sound_handle sound, float x, float y, float z)
{
    ma_sound_set_position(SOUND(sound), x, y, z);
}

void ma_wrap_sound_set_velocity(ma_sound_handle sound, float x, float y, float z)
{
    ma_sound_set_velocity(SOUND(sound), x, y, z);
}

void ma_wrap_sound_set_attenuation_model(ma_sound_handle sound, int model)
{
    ma_sound_set_attenuation_model(SOUND(sound), (ma_attenuation_model)model);
}

void ma_wrap_sound_set_rolloff(ma_sound_handle sound, float rolloff)
{
    ma_sound_set_rolloff(SOUND(sound), rolloff);
}

void ma_wrap_sound_set_min_distance(ma_sound_handle sound, float distance)
{
    ma_sound_set_min_distance(SOUND(sound), distance);
}

void ma_wrap_sound_set_max_distance(ma_sound_handle sound, float distance)
{
    ma_sound_set_max_distance(SOUND(sound), distance);
}

void ma_wrap_sound_set_doppler_factor(ma_sound_handle sound, float factor)
{
    ma_sound_set_doppler_factor(SOUND(sound), factor);
}

/* ---- listener ---- */
void ma_wrap_listener_set_position(ma_engine_handle engine, unsigned int index, float x, float y, float z)
{
    ma_engine_listener_set_position(ENGINE(engine), index, x, y, z);
}

void ma_wrap_listener_set_direction(ma_engine_handle engine, unsigned int index, float x, float y, float z)
{
    ma_engine_listener_set_direction(ENGINE(engine), index, x, y, z);
}

void ma_wrap_listener_set_velocity(ma_engine_handle engine, unsigned int index, float x, float y, float z)
{
    ma_engine_listener_set_velocity(ENGINE(engine), index, x, y, z);
}

void ma_wrap_listener_set_world_up(ma_engine_handle engine, unsigned int index, float x, float y, float z)
{
    ma_engine_listener_set_world_up(ENGINE(engine), index, x, y, z);
}
