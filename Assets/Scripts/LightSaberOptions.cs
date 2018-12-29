//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]
public class LightSaberOptions : DevOptionsObj
{
    public override System.Type ParentType()
    {
        return typeof(LightSaberOptions);
    }
   
    public float hard_swing_velocity_threshold = 1.0f;
    public float min_swing_velocity = .25f;


    public Vector2 blade_size = new Vector2(.1f, 1f);
    public float inner_blade_mult = .075f;
    public Vector2 open_close_speed = new Vector2 (1f, 1f);
    public float blade_epsilon_threshold = .01f;
    public float texture_offset_speed = 1.0f;
    public LayerMask blade_layer_mask;
    public float stay_damage_frequency = .5f;
    public Material inner_glow, outer_glow;
    public ParticleSystem sparks_prefab;

    public SoundEffect blade_enable_audio, blade_disable_audio, blade_hit_audio, blade_idle_audio, blade_swing_audio_light, blade_swing_audio_heavy;
    
    
   
}
