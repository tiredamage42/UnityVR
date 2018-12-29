using UnityEngine;

[System.Serializable] public class SoundEffect {
    public AudioClip[] clips;
    public bool one_shot = true;
    public bool randomize = true;
}
public static class AudioHelpers {
    public static void PlayClip (this AudioSource src, AudioClip clip){
        src.clip = clip;
        src.Play();	
    }
    public static void PlaySoundEffect (this AudioSource src, SoundEffect effect) {
        if (effect.clips.Length == 0)
            return;

        AudioClip clip = effect.clips[Random.Range(0, effect.clips.Length)];

        src.pitch = effect.randomize ? Random.Range(0.95f, 1.05f) : 1.0f;

        if (effect.one_shot) {
            src.PlayOneShot(clip);
        }
        else {
            src.PlayClip(clip);
        }

    }


    public static AudioSource BuildAudioSource (
        GameObject go,
        bool loop, 
        float spatial_blend = 1.0f, 
        float volume = 1.0f,
        AudioRolloffMode roloff_mode = AudioRolloffMode.Linear,
        bool play_on_awake = false
    ) {

        AudioSource a = go.AddComponent<AudioSource>();
        a.playOnAwake = play_on_awake;
        a.spatialBlend = spatial_blend;
        a.rolloffMode = roloff_mode;
        a.loop = loop;
        a.volume = volume;
        return a;
    }




}