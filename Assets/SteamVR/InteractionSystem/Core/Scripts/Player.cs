using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Valve.VR.InteractionSystem{
	// Singleton representing the local VR player/user, with methods for getting
	// the player's hands, head, tracking origin, and guesses for various properties.
	public class Player : MonoBehaviour {
		public InputManager input_manager;
		[HideInInspector] public Collider headCollider;
		[HideInInspector] public Transform hmdTransform;
		[HideInInspector] public Hand rightHand, leftHand;
		
		static Player _instance;
		public static Player instance{
			get{
				if ( _instance == null ) _instance = FindObjectOfType<Player>();
				return _instance;
			}
		}

        // Get Player scale. Assumes it is scaled equally on all axes.
        public float scale { get { return transform.lossyScale.x; } }

		// Height of the eyes above the ground - useful for estimating player height.
		float eyeHeight{
			get{
				Vector3 eyeOffset = Vector3.Project( hmdTransform.position - transform.position, transform.up );
				return eyeOffset.magnitude / transform.lossyScale.x;
			}
		}
		// Guess for the world-space position of the player's feet, directly beneath the HMD.
		public Vector3 feetPositionGuess{
			get{
				return transform.position + Vector3.ProjectOnPlane( hmdTransform.position - transform.position, transform.up );
			}
		}
		// Guess for the world-space direction of the player's hips/torso. This is effectively just the gaze direction projected onto the floor plane.
		Vector3 bodyDirectionGuess {
			get {
				Vector3 direction = Vector3.ProjectOnPlane( hmdTransform.forward, transform.up );
				if ( Vector3.Dot( hmdTransform.up, transform.up ) < 0.0f ) {
					// upside-down. bending over backwards or bent over looking through legs
					direction = -direction;
				}
				return direction;
			}
		}
		bool initialized_refs = false;
		void InitializeReferences (){
			if (initialized_refs)
				return;
			Hand[] hands = GetComponentsInChildren<Hand>();
			for ( int j = 0; j < hands.Length; j++ ){
				if ( hands[j].handType == SteamVR_Input_Sources.LeftHand) leftHand = hands[j];
				if ( hands[j].handType == SteamVR_Input_Sources.RightHand) rightHand = hands[j];
			}
			hmdTransform = GetComponentInChildren<Camera>().transform;
			headCollider = hmdTransform.GetComponentInChildren<Collider>();
			initialized_refs = true;
		}

		void Awake() {
            SteamVR.Initialize(true); //force openvr
			InitializeReferences();
		}

		IEnumerator Start() {
			_instance = this;
            while (SteamVR_Behaviour.instance.forcingInitialization)
                yield return null;
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

			Gizmos.DrawIcon( leftHand.transform.position, "vr_interaction_system_left_hand.png" );
			Gizmos.DrawIcon( rightHand.transform.position, "vr_interaction_system_right_hand.png" );
			
		}
	}
}
