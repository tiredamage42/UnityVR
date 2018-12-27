using UnityEngine;

public static class AudioHelpers {
    public static void PlayClip (this AudioSource src, AudioClip clip){
        src.clip = clip;
        src.Play();	
    }
}