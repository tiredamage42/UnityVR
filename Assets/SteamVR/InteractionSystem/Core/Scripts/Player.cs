using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Valve.VR.InteractionSystem{
	// Singleton representing the local VR player/user, with methods for getting
	// the player's hands, head, tracking origin, and guesses for various properties.
	public class Player : MonoBehaviour {
		
		[Tooltip( "These objects are enabled when SteamVR is available" )]
		public GameObject rigSteamVR;

		[Tooltip( "These objects are enabled when SteamVR is not available, or when the user toggles out of VR" )]
		public GameObject rig2DFallback;

		[HideInInspector] public Hand[] hands;
		[HideInInspector] public Collider headCollider;
		Transform[] hmdTransforms;
		Transform head_follow_t;
		bool debug_mode;

		static Player _instance;
		public static Player instance{
			get{
				if ( _instance == null )
					_instance = FindObjectOfType<Player>();
				return _instance;
			}
		}

		public int handCount{
			get{
				int count = 0;
				for ( int i = 0; i < hands.Length; i++ ){
					if ( hands[i].gameObject.activeInHierarchy )
						count++;
				}
				return count;
			}
		}

		public Hand GetHand( int i ) {
			for ( int j = 0; j < hands.Length; j++ ) {
				if ( !hands[j].gameObject.activeInHierarchy )
					continue;	
				if ( i > 0 ) {
					i--;
					continue;
				}
				return hands[j];
			}
			return null;
		}

		public Hand leftHand{
			get{
				for ( int j = 0; j < hands.Length; j++ ){
					if ( !hands[j].gameObject.activeInHierarchy )
						continue;
					if ( hands[j].handType != SteamVR_Input_Sources.LeftHand)
						continue;
					return hands[j];
				}
				return null;
			}
		}

		public Hand rightHand{
			get{
				for ( int j = 0; j < hands.Length; j++ ) {
					if ( !hands[j].gameObject.activeInHierarchy )
						continue;
					if ( hands[j].handType != SteamVR_Input_Sources.RightHand)
						continue;
					return hands[j];
				}
				return null;
			}
		}

        // Get Player scale. Assumes it is scaled equally on all axes.
        public float scale { get { return transform.lossyScale.x; } }

        // Get the HMD transform. This might return the fallback camera transform if SteamVR is unavailable or disabled.
        public Transform hmdTransform{
			get{
                if (hmdTransforms != null){
                    for (int i = 0; i < hmdTransforms.Length; i++){
                        if (hmdTransforms[i].gameObject.activeInHierarchy)
                            return hmdTransforms[i];
                    }
                }
				return null;
			}
		}

		// Height of the eyes above the ground - useful for estimating player height.
		float eyeHeight{
			get{
				Transform hmd = hmdTransform;
				Vector3 eyeOffset = Vector3.Project( hmd.position - transform.position, transform.up );
				return eyeOffset.magnitude / transform.lossyScale.x;
			}
		}

		// Guess for the world-space position of the player's feet, directly beneath the HMD.
		public Vector3 feetPositionGuess{
			get{
				Transform hmd = hmdTransform;
				return transform.position + Vector3.ProjectOnPlane( hmd.position - transform.position, transform.up );
			}
		}

		// Guess for the world-space direction of the player's hips/torso. This is effectively just the gaze direction projected onto the floor plane.
		Vector3 bodyDirectionGuess {
			get {
				Transform hmd = hmdTransform;
				Vector3 direction = Vector3.ProjectOnPlane( hmd.forward, transform.up );
				if ( Vector3.Dot( hmd.up, transform.up ) < 0.0f ) {
					// upside-down. bending over backwards or bent over looking through legs
					direction = -direction;
				}
				return direction;
			}
		}


		bool _initialized_refs;


		void InitializeReferences (){
			if (_initialized_refs)
				return;

			head_follow_t = GetComponentInChildren<AudioListener>().transform;

			headCollider = head_follow_t.GetComponentInChildren<Collider>();
			
			hands = GetComponentsInChildren<Hand>();
			
			Camera[] hmd_cams = GetComponentsInChildren<Camera>();
			hmdTransforms = new Transform[hmd_cams.Length];
			for (int i = 0; i < hmd_cams.Length; i++){
				hmdTransforms[i] = hmd_cams[i].transform;
			}
			_initialized_refs = true;
		}

		void Awake() {
            SteamVR.Initialize(true); //force openvr
			InitializeReferences();
		}

		IEnumerator Start() {
			_instance = this;
            while (SteamVR_Behaviour.instance.forcingInitialization)
                yield return null;
			ActivateRig( SteamVR.instance == null );
		}

		void ActivateRig( bool debug_mode ) {
			this.debug_mode = debug_mode;

			rigSteamVR.SetActive( !debug_mode );
			rig2DFallback.SetActive( debug_mode );

			head_follow_t.transform.parent = hmdTransform;
			head_follow_t.transform.localPosition = Vector3.zero;
			head_follow_t.transform.localRotation = Quaternion.identity;
		}

		public void PlayerShotSelf() {
			//Do something appropriate here
		}

		void OnDrawGizmos() {
			
			if ( this != instance ) return;
			InitializeReferences();
		

			Gizmos.DrawIcon( feetPositionGuess, "vr_interaction_system_feet.png" );

			Gizmos.color = Color.red;
			Gizmos.DrawLine( feetPositionGuess, feetPositionGuess + transform.up * eyeHeight );

			// Body direction arrow
			Gizmos.color = Color.yellow;
			Vector3 startForward = feetPositionGuess + transform.up * eyeHeight * 0.75f;
			Gizmos.DrawLine( startForward, startForward + bodyDirectionGuess * 2 );

			int count = handCount;
			for ( int i = 0; i < count; i++ ) {
				Hand hand = GetHand( i );
				if ( hand.handType == SteamVR_Input_Sources.LeftHand) {
					Gizmos.DrawIcon( hand.transform.position, "vr_interaction_system_left_hand.png" );
				}
				else if ( hand.handType == SteamVR_Input_Sources.RightHand) {
					Gizmos.DrawIcon( hand.transform.position, "vr_interaction_system_right_hand.png" );
				}
			}
		}
		void OnGUI(){
            if (Debug.isDebugBuild)
                Draw2DDebug();
		}
		void Draw2DDebug() {
			if ( !SteamVR.active ) return;
			int width = 100;
			int height = 25;
			int left = Screen.width / 2 - width / 2;
			int top = Screen.height - height - 10;
			if ( GUI.Button( new Rect( left, top, width, height ), ( !debug_mode ) ? "2D Debug" : "VR" ) ) {
				ActivateRig( !debug_mode );
			}
		}
	}
}
