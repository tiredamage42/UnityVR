﻿using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
	public class Teleport : MonoBehaviour
    {
        [SteamVR_DefaultAction("Teleport", "default")]
        public SteamVR_Action_Boolean teleportAction;
        public LayerMask traceLayerMask;
		public LayerMask floorFixupTraceLayerMask;
		public float floorFixupMaximumTraceDistance = 1.0f;
		public Material pointVisibleMaterial;
		public Material pointLockedMaterial;
		public Material pointHighlightedMaterial;
		public Transform destinationReticleTransform;
		public Transform invalidReticleTransform;
		public Color pointerValidColor;
		public Color pointerInvalidColor;
		public Color pointerLockedColor;
		public bool showPlayAreaMarker = true;
		public float teleportFadeTime = 0.1f;
		public float meshFadeTime = 0.2f;
		public float arcDistance = 10.0f;

		[Header( "Audio Sources" )]
		public AudioSource pointerAudioSource;
		public AudioSource loopingAudioSource;
		public AudioSource headAudioSource;
		public AudioSource reticleAudioSource;

		[Header( "Sounds" )]
		public AudioClip teleportSound;
		public AudioClip pointerStartSound;
		public AudioClip pointerLoopSound;
		public AudioClip pointerStopSound;
		public AudioClip goodHighlightSound;
		public AudioClip badHighlightSound;

		private LineRenderer pointerLineRenderer;
		private GameObject teleportPointerObject;
		private Transform pointerStartTransform;
		private Hand pointerHand = null;
		Player player = null;
		TeleportArc teleportArc = null;

		bool visible = false;

		TeleportMarkerBase[] teleportMarkers;
		private TeleportMarkerBase pointedAtTeleportMarker;
		private TeleportMarkerBase teleportingToMarker;
		private Vector3 pointedAtPosition;
		private Vector3 prevPointedAtPosition;
		private bool teleporting = false;
		private float _fade_time = 0.0f;

		private float meshAlphaPercent = 1.0f;
		private float pointerShowStartTime = 0.0f;
		private float pointerHideStartTime = 0.0f;
		private bool meshFading = false;
		private float fullTintAlpha;

		private float invalidReticleMinScale = 0.2f;
		private float invalidReticleMaxScale = 1.0f;
		private float invalidReticleMinScaleDistance = 0.4f;
		private float invalidReticleMaxScaleDistance = 2.0f;
		private Vector3 invalidReticleScale = Vector3.one;
		private Quaternion invalidReticleTargetRotation = Quaternion.identity;


		private float loopingAudioMaxVolume = 0.0f;

		private Coroutine hintCoroutine = null;

		private bool originalHoverLockState = false;
		private Interactable originalHoveringInteractable = null;
		private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;

		private Vector3 startingFeetOffset = Vector3.zero;
		private bool movedFeetFarEnough = false;


		// Events

		public static SteamVR_Events.Event< float > ChangeScene = new SteamVR_Events.Event< float >();
		public static SteamVR_Events.Action< float > ChangeSceneAction( UnityAction< float > action ) { return new SteamVR_Events.Action< float >( ChangeScene, action ); }

		public static SteamVR_Events.Event< TeleportMarkerBase > Player = new SteamVR_Events.Event< TeleportMarkerBase >();
		public static SteamVR_Events.Action< TeleportMarkerBase > PlayerAction( UnityAction< TeleportMarkerBase > action ) { return new SteamVR_Events.Action< TeleportMarkerBase >( Player, action ); }

		public static SteamVR_Events.Event< TeleportMarkerBase > PlayerPre = new SteamVR_Events.Event< TeleportMarkerBase >();
		public static SteamVR_Events.Action< TeleportMarkerBase > PlayerPreAction( UnityAction< TeleportMarkerBase > action ) { return new SteamVR_Events.Action< TeleportMarkerBase >( PlayerPre, action ); }

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

			
			loopingAudioMaxVolume = loopingAudioSource.volume;

		
			float invalidReticleStartingScale = invalidReticleTransform.localScale.x;
			invalidReticleMinScale *= invalidReticleStartingScale;
			invalidReticleMaxScale *= invalidReticleStartingScale;

			
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
		void CheckForSpawnPoint()
		{
			for (int i = 0; i < teleportMarkers.Length; i++){
				TeleportMarkerBase teleportMarker = teleportMarkers[i];
				TeleportPoint teleportPoint = teleportMarker as TeleportPoint;
				if ( teleportPoint && teleportPoint.playerSpawnPoint ) {
					teleportingToMarker = teleportMarker;
					TeleportPlayer();
					break;
				}
			}
		}


		//-------------------------------------------------
		public void HideTeleportPointer()
		{
			if ( pointerHand != null )
			{
				HidePointer();
			}
		}


		//-------------------------------------------------
		void Update()
		{
			Hand oldPointerHand = pointerHand;
			Hand newPointerHand = null;

			foreach ( Hand hand in player.hands )
			{
				if ( visible )
				{
					if ( WasTeleportButtonReleased( hand ) )
					{
						//This is the pointer hand
						if ( pointerHand == hand ) 
						{
							TryTeleportPlayer();
						}
					}
				}

				if ( WasTeleportButtonPressed( hand ) )
				{
					newPointerHand = hand;
				}
			}

			//If something is attached to the hand that is preventing teleport
			if ( allowTeleportWhileAttached && !allowTeleportWhileAttached.teleportAllowed )
			{
				HidePointer();
			}
			else
			{
				//button pressed and not visible yet
				if ( !visible && newPointerHand != null )
				{
					//Begin showing the pointer
					ShowPointer( newPointerHand, oldPointerHand );
				}
				else if ( visible )
				{
					if ( newPointerHand == null && !IsTeleportButtonDown( pointerHand ) )
					{
						HidePointer();
					}
					else if ( newPointerHand != null )
					{
						//Move the pointer to a new hand
						ShowPointer( newPointerHand, oldPointerHand );
					}
				}
			}

			if ( visible ) {
				UpdatePointer();
				if ( meshFading ) UpdateTeleportColors();
			}
		}

		void UpdatePointer()
		{
			Vector3 pointerStart = pointerStartTransform.position;
			Vector3 pointerEnd;
			Vector3 pointerDir = pointerStartTransform.forward;
			bool hitSomething = false;
			bool showPlayAreaPreview = false;
			Vector3 playerFeetOffset = player.transform.position - player.feetPositionGuess;

			Vector3 arcVelocity = pointerDir * arcDistance;

			TeleportMarkerBase hitTeleportMarker = null;

			//Check pointer angle
			float dotUp = Vector3.Dot( pointerDir, Vector3.up );
			float dotForward = Vector3.Dot( pointerDir, player.hmdTransform.forward );
			bool pointerAtBadAngle = false;
			if ( ( dotForward > 0 && dotUp > 0.75f ) || ( dotForward < 0.0f && dotUp > 0.5f ) )
			{
				pointerAtBadAngle = true;
			}

			//Trace to see if the pointer hit anything
			RaycastHit hitInfo;
			teleportArc.SetArcData( pointerStart, arcVelocity, true, pointerAtBadAngle );
			if ( teleportArc.DrawArc( out hitInfo, traceLayerMask ) )
			{
				hitSomething = true;
				hitTeleportMarker = hitInfo.collider.GetComponentInParent<TeleportMarkerBase>();
			}

			if ( pointerAtBadAngle )
			{
				hitTeleportMarker = null;
			}

			HighlightSelected( hitTeleportMarker );

			if ( hitTeleportMarker != null ) //Hit a teleport marker
			{
				if ( hitTeleportMarker.locked )
				{
					teleportArc.SetColor( pointerLockedColor );
					pointerLineRenderer.startColor = pointerLockedColor;
					pointerLineRenderer.endColor = pointerLockedColor;
					destinationReticleTransform.gameObject.SetActive( false );
				}
				else
				{
					teleportArc.SetColor( pointerValidColor );
					pointerLineRenderer.startColor = pointerValidColor;
					pointerLineRenderer.endColor = pointerValidColor;
					destinationReticleTransform.gameObject.SetActive( hitTeleportMarker.showReticle );
				}

				invalidReticleTransform.gameObject.SetActive( false );

				pointedAtTeleportMarker = hitTeleportMarker;
				pointedAtPosition = hitInfo.point;

				if ( showPlayAreaMarker )
				{
					//Show the play area marker if this is a teleport area
					TeleportArea teleportArea = pointedAtTeleportMarker as TeleportArea;
					
					if ( teleportArea != null && !teleportArea.locked )
					{
						Vector3 offsetToUse = playerFeetOffset;

						//Adjust the actual offset to prevent the play area marker from moving too much
						if ( !movedFeetFarEnough )
						{
							float distanceFromStartingOffset = Vector3.Distance( playerFeetOffset, startingFeetOffset );
							if ( distanceFromStartingOffset < 0.1f )
							{
								offsetToUse = startingFeetOffset;
							}
							else if ( distanceFromStartingOffset < 0.4f )
							{
								offsetToUse = Vector3.Lerp( startingFeetOffset, playerFeetOffset, ( distanceFromStartingOffset - 0.1f ) / 0.3f );
							}
							else
							{
								movedFeetFarEnough = true;
							}
						}


						showPlayAreaPreview = true;
					}
				}

				pointerEnd = hitInfo.point;
			}
			else //Hit neither
			{
				destinationReticleTransform.gameObject.SetActive( false );

				teleportArc.SetColor( pointerInvalidColor );

				pointerLineRenderer.startColor = pointerInvalidColor;
				pointerLineRenderer.endColor = pointerInvalidColor;
				invalidReticleTransform.gameObject.SetActive( !pointerAtBadAngle );

				//Orient the invalid reticle to the normal of the trace hit point
				Vector3 normalToUse = hitInfo.normal;
				float angle = Vector3.Angle( hitInfo.normal, Vector3.up );
				if ( angle < 15.0f )
				{
					normalToUse = Vector3.up;
				}
				invalidReticleTargetRotation = Quaternion.FromToRotation( Vector3.up, normalToUse );
				invalidReticleTransform.rotation = Quaternion.Slerp( invalidReticleTransform.rotation, invalidReticleTargetRotation, 0.1f );

				//Scale the invalid reticle based on the distance from the player
				float distanceFromPlayer = Vector3.Distance( hitInfo.point, player.hmdTransform.position );
				float invalidReticleCurrentScale = Util.RemapNumberClamped( distanceFromPlayer, invalidReticleMinScaleDistance, invalidReticleMaxScaleDistance, invalidReticleMinScale, invalidReticleMaxScale );
				invalidReticleScale.x = invalidReticleCurrentScale;
				invalidReticleScale.y = invalidReticleCurrentScale;
				invalidReticleScale.z = invalidReticleCurrentScale;
				invalidReticleTransform.transform.localScale = invalidReticleScale;

				pointedAtTeleportMarker = null;

				pointerEnd = hitSomething ? hitInfo.point : teleportArc.GetArcPositionAtTime( teleportArc.arcDuration );
			}

			
			destinationReticleTransform.position = pointedAtPosition;
			invalidReticleTransform.position = pointerEnd;
			
			reticleAudioSource.transform.position = pointedAtPosition;

			pointerLineRenderer.SetPosition( 0, pointerStart );
			pointerLineRenderer.SetPosition( 1, pointerEnd );
		}

		
		void HidePointer()
		{
			if ( visible )
			{
				pointerHideStartTime = Time.time;
			}

			visible = false;
			if ( pointerHand )
			{
				if ( ShouldOverrideHoverLock() )
				{
					//Restore the original hovering interactable on the hand
					pointerHand.HoverLock( originalHoverLockState ? originalHoveringInteractable : null );
					
				}

				//Stop looping sound
				loopingAudioSource.Stop();
				pointerAudioSource.PlayClip(pointerStopSound);
			}
			teleportPointerObject.SetActive( false );

			teleportArc.Hide();

			for (int i = 0; i < teleportMarkers.Length; i++){
				TeleportMarkerBase teleportMarker = teleportMarkers[i];
				if ( teleportMarker != null && teleportMarker.markerActive && teleportMarker.gameObject != null )
					teleportMarker.gameObject.SetActive( false );
			}

			destinationReticleTransform.gameObject.SetActive( false );
			invalidReticleTransform.gameObject.SetActive( false );
			
			pointerHand = null;
		}


		void ShowPointer( Hand newPointerHand, Hand oldPointerHand )
		{
			if ( !visible )
			{
				pointedAtTeleportMarker = null;
				pointerShowStartTime = Time.time;
				visible = true;
				meshFading = true;

				teleportPointerObject.SetActive( false );
				teleportArc.Show();

				for (int i = 0; i < teleportMarkers.Length; i++){
					TeleportMarkerBase teleportMarker = teleportMarkers[i];
					if ( teleportMarker.markerActive && teleportMarker.ShouldActivate( player.feetPositionGuess ) ){
						teleportMarker.gameObject.SetActive( true );
						teleportMarker.Highlight( false );
					}
				}

				startingFeetOffset = player.transform.position - player.feetPositionGuess;
				movedFeetFarEnough = false;

				loopingAudioSource.clip = pointerLoopSound;
				loopingAudioSource.loop = true;
				loopingAudioSource.Play();
				loopingAudioSource.volume = 0.0f;
			}


			if ( oldPointerHand )
			{
				if ( ShouldOverrideHoverLock() )
				{
					//Restore the original hovering interactable on the hand
					oldPointerHand.HoverLock( originalHoverLockState ? originalHoveringInteractable : null );
					
				}
			}

			pointerHand = newPointerHand;

			if ( visible && oldPointerHand != pointerHand )
			{
				pointerAudioSource.PlayClip(pointerStartSound);
			}

			if ( pointerHand )
			{
				pointerStartTransform = GetPointerStartTransform( pointerHand );

				if ( pointerHand.currentAttachedObject != null )
				{
					allowTeleportWhileAttached = pointerHand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
				}

				//Keep track of any existing hovering interactable on the hand
				originalHoverLockState = pointerHand.hoverLocked;
				originalHoveringInteractable = pointerHand.hoveringInteractable;

				if ( ShouldOverrideHoverLock() )
				{
					pointerHand.HoverLock( null );
				}

				pointerAudioSource.transform.SetParent( pointerStartTransform );
				pointerAudioSource.transform.localPosition = Vector3.zero;

				loopingAudioSource.transform.SetParent( pointerStartTransform );
				loopingAudioSource.transform.localPosition = Vector3.zero;
			}
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
			if ( pointerHand != null ) {
				if ( validLocation )
					pointerHand.TriggerHapticPulse( 800 );
				else
					pointerHand.TriggerHapticPulse( 100 );
			}
		}

		void TryTeleportPlayer(){
			if ( visible && !teleporting ){
				if ( pointedAtTeleportMarker != null && pointedAtTeleportMarker.locked == false )
				{
					//Pointing at an unlocked teleport marker
					teleportingToMarker = pointedAtTeleportMarker;
					InitiateTeleportFade();
					CancelTeleportHint();
				}
			}
		}

		void InitiateTeleportFade()
		{
			teleporting = true;
			_fade_time = teleportFadeTime;

			//if switching scenes
			TeleportPoint teleportPoint = teleportingToMarker as TeleportPoint;
			if ( teleportPoint != null && teleportPoint.teleportType == TeleportPoint.TeleportPointType.SwitchToNewScene )
			{
				_fade_time *= 3.0f;
				Teleport.ChangeScene.Send( _fade_time );
			}

			SteamVR_Fade.Start( Color.clear, 0 );
			SteamVR_Fade.Start( Color.black, _fade_time );

			headAudioSource.PlayClip(teleportSound);

			Invoke( "TeleportPlayer", _fade_time );
		}


		//-------------------------------------------------
		void TeleportPlayer()
		{
			teleporting = false;
			Teleport.PlayerPre.Send( pointedAtTeleportMarker );
			SteamVR_Fade.Start( Color.clear, _fade_time );

			TeleportPoint teleportPoint = teleportingToMarker as TeleportPoint;
			Vector3 teleportPosition = pointedAtPosition;

			if ( teleportPoint != null )
			{
				teleportPosition = teleportPoint.transform.position;

				//Teleport to a new scene
				if ( teleportPoint.teleportType == TeleportPoint.TeleportPointType.SwitchToNewScene )
				{
					teleportPoint.TeleportToScene();
					return;
				}
			}

			// Find the actual floor position below the navigation mesh
			TeleportArea teleportArea = teleportingToMarker as TeleportArea;
			if ( teleportArea != null )
			{
				if ( floorFixupMaximumTraceDistance > 0.0f )
				{
					RaycastHit raycastHit;
					if ( Physics.Raycast( teleportPosition + 0.05f * Vector3.down, Vector3.down, out raycastHit, floorFixupMaximumTraceDistance, floorFixupTraceLayerMask ) )
					{
						teleportPosition = raycastHit.point;
					}
				}
			}

			if ( teleportingToMarker.ShouldMovePlayer() )
			{
				Vector3 playerFeetOffset = player.transform.position - player.feetPositionGuess;
				player.transform.position = teleportPosition + playerFeetOffset;
			}
			else
			{
				teleportingToMarker.TeleportPlayer( pointedAtPosition );
			}

			Teleport.Player.Send( pointedAtTeleportMarker );
		}


		private void HighlightSelected( TeleportMarkerBase hitTeleportMarker )
		{
			if ( pointedAtTeleportMarker != hitTeleportMarker ) //Pointing at a new teleport marker
			{
				if ( pointedAtTeleportMarker != null )
				{
					pointedAtTeleportMarker.Highlight( false );
				}

				if ( hitTeleportMarker != null )
				{
					hitTeleportMarker.Highlight( true );

					prevPointedAtPosition = pointedAtPosition;
					PlayPointerHaptic( !hitTeleportMarker.locked );
					
					reticleAudioSource.PlayClip(goodHighlightSound);

	
					loopingAudioSource.volume = loopingAudioMaxVolume;
				}
				else if ( pointedAtTeleportMarker != null )
				{
					reticleAudioSource.PlayClip(badHighlightSound);

					
					loopingAudioSource.volume = 0.0f;
				}
			}
			else if ( hitTeleportMarker != null ) //Pointing at the same teleport marker
			{
				if ( Vector3.Distance( prevPointedAtPosition, pointedAtPosition ) > 1.0f )
				{
					prevPointedAtPosition = pointedAtPosition;
					PlayPointerHaptic( !hitTeleportMarker.locked );
				}
			}
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
				foreach ( Hand hand in player.hands )
				{
					bool showHint = IsEligibleForTeleport( hand );
					bool isShowingHint = !string.IsNullOrEmpty( ControllerButtonHints.GetActiveHintText( hand, teleportAction) );
					if ( showHint )
					{
						if ( !isShowingHint )
						{
							ControllerButtonHints.ShowTextHint( hand, teleportAction, "Teleport" );
							prevBreakTime = Time.time;
							prevHapticPulseTime = Time.time;
						}

						if ( Time.time > prevHapticPulseTime + 0.05f )
						{
							//Haptic pulse for a few seconds
							pulsed = true;

							hand.TriggerHapticPulse( 500 );
						}
					}
					else if ( !showHint && isShowingHint )
					{
						ControllerButtonHints.HideTextHint( hand, teleportAction);
					}
				}

				if ( Time.time > prevBreakTime + 3.0f )
				{
					//Take a break for a few seconds
					yield return new WaitForSeconds( 3.0f );

					prevBreakTime = Time.time;
				}

				if ( pulsed )
				{
					prevHapticPulseTime = Time.time;
				}

				yield return null;
			}
		}


		public bool IsEligibleForTeleport( Hand hand )
		{
			if ( hand == null )
				return false;
			
			if ( !hand.gameObject.activeInHierarchy )
				return false;
			
			if ( hand.hoveringInteractable != null )
				return false;
			
			if ( hand.noSteamVRFallbackCamera == null )
			{
				if ( hand.isActive == false)
					return false;
				
				//Something is attached to the hand
				if ( hand.currentAttachedObject != null ) {
					AllowTeleportWhileAttachedToHand allowTeleportWhileAttachedToHand = hand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();

					return ( allowTeleportWhileAttachedToHand != null && allowTeleportWhileAttachedToHand.teleportAllowed == true );
				}
			}

			return true;
		}

		bool ShouldOverrideHoverLock()
		{
			return ( !allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock );
		}


		bool WasTeleportButtonReleased( Hand hand ){
			if ( IsEligibleForTeleport( hand ) ){
				if ( hand.noSteamVRFallbackCamera != null ){
					return Input.GetKeyUp( KeyCode.T );
				}
				else{
                    return teleportAction.GetStateUp(hand.handType);
                    //return hand.controller.GetPressUp( SteamVR_Controller.ButtonMask.Touchpad );
                }
			}
			return false;
		}
		bool IsTeleportButtonDown( Hand hand ){
			if ( IsEligibleForTeleport( hand ) ){
				if ( hand.noSteamVRFallbackCamera != null ){
					return Input.GetKey( KeyCode.T );
				}
				else{
                    return teleportAction.GetState(hand.handType);
                    //return hand.controller.GetPress( SteamVR_Controller.ButtonMask.Touchpad );
				}
			}
			return false;
		}


		bool WasTeleportButtonPressed( Hand hand ) {
			if ( IsEligibleForTeleport( hand ) ) {
				if ( hand.noSteamVRFallbackCamera != null ) {
					return Input.GetKeyDown( KeyCode.T );
				}
				else {
                    return teleportAction.GetStateDown(hand.handType);
                    //return hand.controller.GetPressDown( SteamVR_Controller.ButtonMask.Touchpad );
				}
			}
			return false;
		}

		Transform GetPointerStartTransform( Hand hand ) {
			return hand.noSteamVRFallbackCamera != null ? hand.noSteamVRFallbackCamera.transform : hand.transform;
		}
	}
}
