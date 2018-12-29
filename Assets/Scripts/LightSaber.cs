using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;



[RequireComponent( typeof( Interactable ) )]
public class LightSaber : MonoBehaviour
{
    public Color blade_color = Color.red;
    public Color inner_blade_color = Color.white;
    public bool trigger_bool;
    
    bool is_active;
    LineRenderer[] line_rends;
    CapsuleCollider blade_trigger;
    ParticleSystem sparks_instance;
    DevOptions.OptionsHolder<LightSaberOptions> options = new DevOptions.OptionsHolder<LightSaberOptions>();

    Rigidbody rb;
    float contact_timer;
    public List<Collider> contact_colliders = new List<Collider>();
    void OnTriggerEnter (Collider other) {
        if (other.isTrigger)
            return; 

        Debug.Log("Light saber trigger!!!");
        
        if (contact_colliders.Count == 0) {
            contact_timer = 0;
            contact_colliders.Add (other);
        }
        else{
            if (!contact_colliders.Contains(other)) {
                contact_colliders.Add (other);
            }
        }
        
        //if velocity high enough:
        if (!audio_shot.isPlaying) {
            audio_shot.PlaySoundEffect(options.o.blade_hit_audio);
        }
        //deal damage to collider

    }




    void CallSparks (Vector3 at_pos, Vector3 hit_normal) {

        if (!sparks_instance.gameObject.activeInHierarchy) {
            sparks_instance.gameObject.SetActive(true);
            sparks_instance.Play();
        }
        sparks_instance.transform.position = at_pos;
        sparks_instance.transform.rotation = Quaternion.LookRotation(hit_normal);
    }
    void DisableSparks () {
        sparks_instance.Stop();
        sparks_instance.gameObject.SetActive(false);
    }

    void OnTriggerStay (Collider other) {
        if (other.isTrigger)
            return; 
    }
    void OnTriggerExit (Collider other) {
        if (other.isTrigger)
            return; 

        contact_colliders.Remove(other);
        if (contact_colliders.Count == 0) {
            contact_timer = 0;
            DisableSparks();
        }        
    }




    float current_length, texture_offset;


    void BuildLineRend (string name, Material material) {
        GameObject new_obj = new GameObject(name);
        new_obj.transform.SetParent(transform);
        new_obj.transform.localPosition = Vector3.zero;
        new_obj.transform.localRotation = Quaternion.identity;
        LineRenderer lr = new_obj.AddComponent<LineRenderer>();
        lr.receiveShadows = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        lr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        lr.numCapVertices = 4;
        lr.sharedMaterial = material;
    }
    void BuildLineRenderers () {
        BuildLineRend("Inner Glow", options.o.inner_glow);
        BuildLineRend("Outer Glow", options.o.outer_glow);
        line_rends = GetComponentsInChildren<LineRenderer>();
        line_rends[0].gameObject.SetActive(false);
        line_rends[1].gameObject.SetActive(false);

        SetBladeColor(blade_color, inner_blade_color);
        SetRendererWidth(options.o.blade_size.x, options.o.inner_blade_mult);
    }

    void BuildBladeTrigger () {
        float radius = GetComponent<CapsuleCollider>().radius; //radius of handle collider
        blade_trigger = gameObject.AddComponent<CapsuleCollider>();
        blade_trigger.isTrigger = true;
        blade_trigger.radius = radius;
        blade_trigger.enabled = false;


        SetBladeTriggerHeight();
        

    }

    void Awake () {
        is_active = false;
        rb = GetComponent<Rigidbody>();
        interactable = GetComponent<Interactable>();			
		

        BuildAudioSources();
        BuildLineRenderers ();
        BuildBladeTrigger ();

        sparks_instance = Instantiate(options.o.sparks_prefab);
        sparks_instance.gameObject.SetActive(false);
    
    }
    bool renderers_enabled;


    void SetColor (int index, Color col) {
        line_rends[index].startColor = col;
        line_rends[index].endColor = col;
    }
    public void SetBladeColor (Color blade_color) {
        SetBladeColor(blade_color, Color.white);
    }
    public void SetBladeColor (Color blade_color, Color inner_blade_color) {
        SetColor(0, blade_color);
        SetColor(1, inner_blade_color);   
    }

    void EnableBladeRenderers (bool enabled) {
        if (renderers_enabled != enabled) {
            for (int i = 0; i < 2; i++) {
                line_rends[i].gameObject.SetActive(enabled);
            }
            renderers_enabled = enabled;
        }
    }

    AudioSource audio_shot, loop_source;

    void BuildAudioSources () {
        audio_shot = AudioHelpers.BuildAudioSource(gameObject, false);
        loop_source = AudioHelpers.BuildAudioSource(gameObject, true);
    }

    public void TriggerSaber (bool enabled) {
        if (enabled != is_active) {
            blade_trigger.enabled = enabled;
            is_active = enabled;

            audio_shot.PlaySoundEffect(enabled ? options.o.blade_enable_audio : options.o.blade_disable_audio);

            if (enabled) {
                loop_source.Play();
            }
            else {
                loop_source.Stop();
            }
        }
    }

    void SetBladeTriggerHeight () {
        blade_trigger.height = options.o.blade_size.y;
        blade_trigger.center = new Vector3(0, blade_trigger.height * .5f, 0);
    }

    void SetRendererWidth (float blade_width, float inner_blade_mult) {
        line_rends[0].widthMultiplier = blade_width;
        line_rends[1].widthMultiplier = blade_width * inner_blade_mult;
    }

    void SetRendererPosition (Vector3 start_pos, Vector3 end_pos) {
        for (int i = 0; i < 2; i++) {
            line_rends[i].SetPosition(0, start_pos);
            line_rends[i].SetPosition(1, end_pos);
        }
    }
    void SetRendererTextureOffset () {
        texture_offset -= Time.deltaTime * options.o.texture_offset_speed;
        if (texture_offset < -10f) texture_offset += 10f;
        line_rends[0].sharedMaterial.SetTextureOffset("_MainTex", new Vector2(texture_offset, 0.0f));
    }

    void DealDamageStay () {
        //do something with colliders
        for (int i = 0; i < contact_colliders.Count; i++) {

        }
    }


    void CheckVelocityForSwing (float velocity) {
        if (velocity < options.o.min_swing_velocity)
            return;
        audio_shot.PlaySoundEffect(velocity < options.o.hard_swing_velocity_threshold ? options.o.blade_swing_audio_light : options.o.blade_swing_audio_heavy);
    }
            


    // Update is called once per frame
    void Update()
    {
        if (trigger_bool) {
            TriggerSaber(!is_active);
            trigger_bool = false;
        }

        if (blade_trigger.enabled) {
            SetBladeTriggerHeight();
        }
        

        float targ_length = is_active ? options.o.blade_size.y : 0.0f;
        if (current_length != targ_length) {
            float speed = is_active ? options.o.open_close_speed.x : options.o.open_close_speed.y;
            current_length = Mathf.Lerp (current_length, targ_length, Time.deltaTime * speed);
        }

        if (current_length > options.o.blade_epsilon_threshold) {
            EnableBladeRenderers(true);
            SetBladeColor(blade_color, inner_blade_color);
            SetRendererWidth(options.o.blade_size.x, options.o.inner_blade_mult);

            Vector3 start_pos = transform.position;
            SetRendererPosition(start_pos, start_pos + transform.up * current_length);
            SetRendererTextureOffset();    
        }
        else
        {
            EnableBladeRenderers(false);
        }


        if (contact_colliders.Count != 0) {
            contact_timer += Time.deltaTime;

            if (contact_timer >= options.o.stay_damage_frequency) {
                DealDamageStay ();
                
                contact_timer = 0;
            }

            RaycastHit hit;
            Ray ray = new Ray (transform.position, transform.up);
            if (Physics.SphereCast(ray, blade_trigger.radius, out hit, options.o.blade_size.y, options.o.blade_layer_mask)) {
                CallSparks(hit.point, hit.normal);
            }
        }    
    }





        protected bool attached = false;
        
        protected RigidbodyInterpolation hadInterpolation = RigidbodyInterpolation.None;

        [HideInInspector] public Interactable interactable;


        
        protected virtual void OnHandHoverBegin( Hand hand )
		{
			bool showHint = true;
			if ( showHint )
			{
                hand.ShowGrabHint();
			}
		}


        //-------------------------------------------------
        protected virtual void OnHandHoverEnd( Hand hand )
		{
            hand.HideGrabHint();
		}


        //-------------------------------------------------
        protected virtual void HandHoverUpdate( Hand hand )
        {
            bool grabbing = Player.instance.input_manager.GetGripDown(hand);

            //GrabTypes startingGrabType = hand.GetGrabStarting();
            
            //if (startingGrabType != GrabTypes.None)
            if (grabbing)
            
            {
				hand.AttachInteractable( interactable ); //startingGrabType, attachmentOffset );
                hand.HideGrabHint();
            }
		}

        //-------------------------------------------------
        protected virtual void OnAttachedToHand( Hand hand )
		{
            //Debug.Log("Pickup: " + hand.GetGrabStarting().ToString());
            hadInterpolation = rb.interpolation;

            attached = true;


			hand.HoverLock( null );
            
            rb.interpolation = RigidbodyInterpolation.None;
		    
            Debug.Log("TRIGGERING SABER");
            TriggerSaber(true);
        }


       

       

        protected virtual void HandAttachedUpdate(Hand hand)
        {
            Vector3 velocity = hand.GetTrackedObjectVelocity();
            Vector3 angularVelocity = hand.GetTrackedObjectAngularVelocity();



            CheckVelocityForSwing(Mathf.Max(velocity.magnitude, angularVelocity.magnitude));
    

            bool grabbing_end = Player.instance.input_manager.GetGripUp(hand);


            //if (hand.IsGrabEnding(this.gameObject))
            if (grabbing_end)
            
            {
                hand.DetachObject(gameObject, false);
            }
        }

        protected virtual void OnHandFocusAcquired( Hand hand )
		{
		}
        protected virtual void OnHandFocusLost( Hand hand )
		{
		}
        protected virtual void OnDetachedFromHand(Hand hand)
        {
            attached = false;

            
        
            hand.HoverUnlock(null);
            
            rb.interpolation = hadInterpolation;

            Vector3 velocity = hand.GetTrackedObjectVelocity();
            Vector3 angularVelocity = hand.GetTrackedObjectAngularVelocity();
                    

          
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;

            TriggerSaber(false);
        }

	



}
