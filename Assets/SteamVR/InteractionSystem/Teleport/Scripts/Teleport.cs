using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{

	/*
	Maybe: raycast down from position to double check floor (on actual teleport)
	 */
	public class Teleport : MonoBehaviour
    {
        [SteamVR_DefaultAction("Teleport", "default")]
        public SteamVR_Action_Boolean teleportAction;
        public LayerMask traceLayerMask;
		public Material pointVisibleMaterial;
		public Material pointLockedMaterial;
		public Material pointHighlightedMaterial;
		public Transform destinationReticleTransform;
		public Transform invalidReticleTransform;
		public Color pointerValidColor;
		public Color pointerInvalidColor;
		public Color pointerLockedColor;
		public float teleportFadeTime = 0.1f;
		public float meshFadeTime = 0.2f;
		public float arcDistance = 10.0f;

		[Header( "Audio Sources" )]
		public AudioSource pointerAudioSource;
		public AudioSource headAudioSource;
		public AudioSource reticleAudioSource;

		[Header( "Sounds" )]
		public AudioClip teleportSound;
		public AudioClip pointerStartSound;
		public AudioClip pointerStopSound;
		public AudioClip goodHighlightSound;
		public AudioClip badHighlightSound;

		private LineRenderer pointerLineRenderer;
		private GameObject teleportPointerObject;
		Player player = null;
		TeleportArc teleportArc = null;

		bool visible = false;

		TeleportMarkerBase[] teleportMarkers;
		private bool teleporting = false;
		
		private float meshAlphaPercent = 1.0f;
		private float pointerShowStartTime = 0.0f;
		private float pointerHideStartTime = 0.0f;
		private bool meshFading = false;
		private float fullTintAlpha;

		private float invalidReticleMinScale = 0.2f;
		private float invalidReticleMaxScale = 1.0f;
		private float invalidReticleMinScaleDistance = 0.4f;
		private float invalidReticleMaxScaleDistance = 2.0f;
		
		private Coroutine hintCoroutine = null;

		private bool originalHoverLockState = false;
		private Interactable originalHoveringInteractable = null;
		private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;

		
		private static Teleport _instance;
		public static Teleport instance{
			get{
				if ( _instance == null ) _instance = GameObject.FindObjectOfType<Teleport>();
				return _instance;
			}
		}

		void Awake()
		{
			_instance = this;

			
			pointerLineRenderer = GetComponentInChildren<LineRenderer>();
			teleportPointerObject = pointerLineRenderer.gameObject;

			int tintColorID = Shader.PropertyToID( "_TintColor" );
			fullTintAlpha = pointVisibleMaterial.GetColor( tintColorID ).a;

			teleportArc = GetComponent<TeleportArc>();
		
			float invalidReticleStartingScale = invalidReticleTransform.localScale.x;
			invalidReticleMinScale *= invalidReticleStartingScale;
			invalidReticleMaxScale *= invalidReticleStartingScale;

			
		}
		void CheckForSpawnPoint() {
			for (int i = 0; i < teleportMarkers.Length; i++){
				TeleportMarkerBase teleportMarker = teleportMarkers[i];
				TeleportPoint teleportPoint = teleportMarker as TeleportPoint;
				if ( teleportPoint && teleportPoint.playerSpawnPoint ) {
					StartCoroutine( TeleportPlayer (teleportPoint.transform.position, true, false, "" ) );
					break;
				}
			}
		}

		void Start()
		{
			teleportMarkers = GameObject.FindObjectsOfType<TeleportMarkerBase>();

			HidePointer();

			player = InteractionSystem.Player.instance;
			if ( player == null ) {
				Debug.LogError( "Teleport: No Player instance found in map." );
				Destroy( this.gameObject );
				return;
			}

			headAudioSource.transform.SetParent( player.hmdTransform );
			headAudioSource.transform.localPosition = Vector3.zero;

			CheckForSpawnPoint();

			Invoke( "ShowTeleportHint", 5.0f );
		}
		void OnDisable()
		{
			HidePointer();
		}
		
		void Update()
		{
			
			bool teleport_button_down = false;
		
			if ( visible )
			{
				if ( WasTeleportButtonReleased( player.rightHand ) )
				{
					if (!teleporting ){
						if (potential_teleport_valid)
						{
							//Pointing at an unlocked teleport marker
							StartCoroutine( TeleportPlayer (potential_teleport_point, false, current_teleport_marker.scene_teleport, current_teleport_marker.switchToScene));
							CancelTeleportHint();
						}
					}
					
				}
			}

			if ( WasTeleportButtonPressed( player.rightHand ) )
			{
				teleport_button_down = true;
			}
			
		

			//If something is attached to the hand that is preventing teleport
			if ( allowTeleportWhileAttached && !allowTeleportWhileAttached.teleportAllowed )
			{
				HidePointer();
			}
			else
			{
				//button pressed and not visible yet
				if ( !visible && teleport_button_down)
				{
					//Begin showing the pointer
					ShowPointer();
					
				}
				else if ( visible )
				{
					if ( !teleport_button_down && !IsTeleportButtonDown( player.rightHand ) )
					{
						HidePointer();
					}
				}
			}

			if ( visible ) {
				UpdatePointer();
				if ( meshFading ) 
					UpdateTeleportColors();
			}
		}

		void ShowPointer( )
		{
			current_teleport_marker = null;
			pointerShowStartTime = Time.time;
			visible = true;
			meshFading = true;
			teleportPointerObject.SetActive( false );
			teleportArc.Show();
			pointerAudioSource.PlayClip(pointerStartSound);
			
			for (int i = 0; i < teleportMarkers.Length; i++){
				if ( teleportMarkers[i].markerActive && Vector3.Distance( teleportMarkers[i].transform.position, player.feetPositionGuess ) > 1.0f ){
					teleportMarkers[i].gameObject.SetActive( true );
					teleportMarkers[i].Highlight( false );
				}
			}
			
			if ( player.rightHand.currentAttachedObject != null )
			{
				allowTeleportWhileAttached = player.rightHand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
			}

			//Keep track of any existing hovering interactable on the hand
			originalHoverLockState = player.rightHand.hoverLocked;
			originalHoveringInteractable = player.rightHand.hoveringInteractable;

			if ( ShouldOverrideHoverLock() )
			{
				player.rightHand.HoverLock( null );
			}

			pointerAudioSource.transform.SetParent( player.rightHand.transform );
			pointerAudioSource.transform.localPosition = Vector3.zero;
		
			
		}

		void HidePointer()
		{
			if ( visible )
			{
				pointerHideStartTime = Time.time;
			}

			visible = false;
				if ( ShouldOverrideHoverLock() )
				{
					//Restore the original hovering interactable on the hand
					player.rightHand.HoverLock( originalHoverLockState ? originalHoveringInteractable : null );
					
				}
				pointerAudioSource.PlayClip(pointerStopSound);
			teleportPointerObject.SetActive( false );

			teleportArc.Hide();

			for (int i = 0; i < teleportMarkers.Length; i++){
				if ( teleportMarkers[i].markerActive )
					teleportMarkers[i].gameObject.SetActive( false );
			}

			destinationReticleTransform.gameObject.SetActive( false );
			invalidReticleTransform.gameObject.SetActive( false );
			
		}

		bool PointerAtBadAngle (Vector3 pointerDir){
			//Check pointer angle
			float dotUp = Vector3.Dot( pointerDir, Vector3.up );
			float dotForward = Vector3.Dot( pointerDir, player.hmdTransform.forward );
			return ( ( dotForward > 0 && dotUp > 0.75f ) || ( dotForward < 0.0f && dotUp > 0.5f ) );
		}


		void SetArcColor(Color color)
		{
			teleportArc.SetColor( color );
			pointerLineRenderer.startColor = color;
			pointerLineRenderer.endColor = color;
		}

		
		void SetReticleActive (bool pointerAtBadAngle, Vector3 at_position, bool hit_marker)
		{
			if (hit_marker)
			{
				destinationReticleTransform.gameObject.SetActive(false);
				invalidReticleTransform.gameObject.SetActive(false);
			}
			else
			{
				bool show_good_reticle = !pointerAtBadAngle; //and area not invalid
				destinationReticleTransform.gameObject.SetActive( show_good_reticle );
				invalidReticleTransform.gameObject.SetActive( !show_good_reticle );
				if (!show_good_reticle){
					//Scale the invalid reticle based on the distance from the player
					float distanceFromPlayer = Vector3.Distance( at_position, player.hmdTransform.position );
					float invalidReticleCurrentScale = Util.RemapNumberClamped( distanceFromPlayer, invalidReticleMinScaleDistance, invalidReticleMaxScaleDistance, invalidReticleMinScale, invalidReticleMaxScale );
					invalidReticleTransform.transform.localScale = Vector3.one * invalidReticleCurrentScale;
				}
				destinationReticleTransform.position = at_position;
				invalidReticleTransform.position = at_position;
				reticleAudioSource.transform.position = at_position;
			}
			


		}

		Vector3 potential_teleport_point;
		bool potential_teleport_valid;
		TeleportMarkerBase current_teleport_marker;




									
			


		void UpdatePointer()
		{
			Vector3 pointerStart = player.rightHand.transform.position;
			Vector3 pointerDir = player.rightHand.transform.forward;

			Vector3 playerFeetOffset = player.transform.position - player.feetPositionGuess;

			bool hitSomething = false;

			Vector3 arcVelocity = pointerDir * arcDistance;

			TeleportMarkerBase hitTeleportMarker = null;

			bool pointerAtBadAngle = PointerAtBadAngle(pointerDir);
			
			//Trace to see if the pointer hit anything
			teleportArc.SetArcData( pointerStart, arcVelocity, true, pointerAtBadAngle );

			RaycastHit hit;
			Vector3 hit_point = Vector3.zero;
			if ( teleportArc.DrawArc( out hit, traceLayerMask ) )
			{
				hitSomething = true;
				hitTeleportMarker = hit.collider.GetComponentInParent<TeleportMarkerBase>();
			}

			HighlightSelected( pointerAtBadAngle ? null : hitTeleportMarker );

			if (pointerAtBadAngle)
			{
				teleportArc.SetColor( pointerInvalidColor );
			}
			else
			{
				if (hitTeleportMarker != null){
					if ( hitTeleportMarker.locked )
						SetArcColor(pointerLockedColor);
					else
						SetArcColor(pointerValidColor);
				}
				else{
					//check validity of position
					SetArcColor(pointerValidColor);
				}
			}

			if ( hitTeleportMarker != null ) //Hit a teleport marker
			{
				potential_teleport_point = hitTeleportMarker.transform.position;
				potential_teleport_valid = !pointerAtBadAngle && !hitTeleportMarker.locked;
			}
			else //Hit neither
			{				
				potential_teleport_point = hitSomething ? hit.point : teleportArc.GetArcPositionAtTime( teleportArc.arcDuration );
				potential_teleport_valid = !pointerAtBadAngle;
			}

			SetReticleActive (pointerAtBadAngle, potential_teleport_point, hitTeleportMarker != null);
	
			pointerLineRenderer.SetPosition( 0, pointerStart );
			pointerLineRenderer.SetPosition( 1, potential_teleport_point );
		}

		void UpdateTeleportColors() {
			float deltaTime = Time.time - pointerShowStartTime;
			if ( deltaTime > meshFadeTime ) {
				meshAlphaPercent = 1.0f;
				meshFading = false;
			}
			else {
				meshAlphaPercent = Mathf.Lerp( 0.0f, 1.0f, deltaTime / meshFadeTime );
			}
			//Tint color for the teleport points
			for (int i = 0; i < teleportMarkers.Length; i++){
				teleportMarkers[i].SetAlpha( fullTintAlpha * meshAlphaPercent, meshAlphaPercent );
			}
		}
		void PlayPointerHaptic( bool validLocation )
		{
			if ( player.rightHand != null ) {
				if ( validLocation )
					player.rightHand.TriggerHapticPulse( 800 );
				else
					player.rightHand.TriggerHapticPulse( 100 );
			}
		}


		IEnumerator TeleportPlayer (Vector3 to_position, bool instant, bool scene_switch, string scene_string)
		{
			float _fade_time = teleportFadeTime;
			if (!instant){
				teleporting = true;

				//if switching scenes
				if ( scene_switch ) _fade_time *= 3.0f;
		
				SteamVR_Fade.Start( Color.clear, 0 );
				SteamVR_Fade.Start( Color.black, _fade_time );

				headAudioSource.PlayClip(teleportSound);

				yield return new WaitForSecondsRealtime(_fade_time);
			}
			teleporting = false;
			SteamVR_Fade.Start( Color.clear, _fade_time );
			if (!scene_switch){
				Vector3 playerFeetOffset = player.transform.position - player.feetPositionGuess;
				player.transform.position = to_position + playerFeetOffset;
			}
			else {
				TeleportToScene(scene_string);
			}
		}

		void TeleportToScene(string scene_string)
		{
			if ( string.IsNullOrEmpty( scene_string ) )
				Debug.LogError( "TeleportPoint: Invalid scene name to switch to: " + scene_string );
			Debug.Log( "TeleportPoint: Hook up your level loading logic to switch to new scene: " + scene_string );
		}

		void HighlightSelected( TeleportMarkerBase new_marker )
		{
			
			if ( current_teleport_marker != new_marker ) //Pointing at a new teleport marker
			{
				if ( current_teleport_marker != null )
					current_teleport_marker.Highlight( false );
				
				if ( new_marker != null ) {
					new_marker.Highlight( true );
					PlayPointerHaptic( !new_marker.locked );
					reticleAudioSource.PlayClip(goodHighlightSound);
				}
				else if ( current_teleport_marker != null ) {
					reticleAudioSource.PlayClip(badHighlightSound);
				}
			}
			else if ( new_marker != null ) //Pointing at the same teleport marker
			{
				//trigger haptic based on time
			}
			current_teleport_marker = new_marker;

		}

		public void ShowTeleportHint()
		{
			CancelTeleportHint();
			hintCoroutine = StartCoroutine( TeleportHintCoroutine() );
		}

		public void CancelTeleportHint(){
			if ( hintCoroutine != null ){
                ControllerButtonHints.HideTextHint(player.leftHand, teleportAction);
                ControllerButtonHints.HideTextHint(player.rightHand, teleportAction);
				StopCoroutine( hintCoroutine );
				hintCoroutine = null;
			}
			CancelInvoke( "ShowTeleportHint" );
		}


		IEnumerator TeleportHintCoroutine()
		{
			float prevBreakTime = Time.time;
			float prevHapticPulseTime = Time.time;

			while ( true )
			{
				bool pulsed = false;

				//Show the hint on each eligible hand
				bool showHint = IsEligibleForTeleport( player.rightHand );
				bool isShowingHint = !string.IsNullOrEmpty( ControllerButtonHints.GetActiveHintText( player.rightHand, teleportAction) );
				if ( showHint )
				{
					if ( !isShowingHint )
					{
						ControllerButtonHints.ShowTextHint( player.rightHand, teleportAction, "Teleport" );
						prevBreakTime = Time.time;
						prevHapticPulseTime = Time.time;
					}

					if ( Time.time > prevHapticPulseTime + 0.05f )
					{
						//Haptic pulse for a few seconds
						pulsed = true;

						player.rightHand.TriggerHapticPulse( 500 );
					}
				}
				else if ( !showHint && isShowingHint )
				{
					ControllerButtonHints.HideTextHint( player.rightHand, teleportAction);
				}

				if ( Time.time > prevBreakTime + 3.0f )
				{
					//Take a break for a few seconds
					yield return new WaitForSeconds( 3.0f );

					prevBreakTime = Time.time;
				}

				if ( pulsed )
					prevHapticPulseTime = Time.time;
				
				yield return null;
			}
		}


		bool IsEligibleForTeleport( Hand hand )
		{
			if ( hand == null ) return false;
			if ( hand.hoveringInteractable != null ) return false;
			if ( !hand.isActive ) return false;
			//Something is attached to the hand
			if ( hand.currentAttachedObject != null ) {
				AllowTeleportWhileAttachedToHand allowTeleportWhileAttachedToHand = hand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
				return ( allowTeleportWhileAttachedToHand != null && allowTeleportWhileAttachedToHand.teleportAllowed == true );
			}
			return true;
		}

		bool ShouldOverrideHoverLock()
		{
			return ( !allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock );
		}


		bool WasTeleportButtonReleased( Hand hand ){
			if ( IsEligibleForTeleport( hand ) ){
				return teleportAction.GetStateUp(hand.handType);
				//return hand.controller.GetPressUp( SteamVR_Controller.ButtonMask.Touchpad );
			}
			return false;
		}
		bool IsTeleportButtonDown( Hand hand ){
			if ( IsEligibleForTeleport( hand ) ){
				return teleportAction.GetState(hand.handType);
				//return hand.controller.GetPress( SteamVR_Controller.ButtonMask.Touchpad );
			}
			return false;
		}
		bool WasTeleportButtonPressed( Hand hand ) {
			if ( IsEligibleForTeleport( hand ) ) {
				return teleportAction.GetStateDown(hand.handType);
				//return hand.controller.GetPressDown( SteamVR_Controller.ButtonMask.Touchpad );
			}
			return false;
		}
	
	}
}
