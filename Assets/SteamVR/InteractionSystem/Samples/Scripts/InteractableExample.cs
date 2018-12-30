//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Demonstrates how to create a simple interactable object
//
//=============================================================================

using UnityEngine;
using System.Collections;

namespace Valve.VR.InteractionSystem.Sample
{
	//------------------------------------------
	public class InteractableExample : Grabbable
	{
		private TextMesh textMesh;
		private Vector3 oldPosition;
		private Quaternion oldRotation;

		private float attachTime;



		//-------------------------------------------------
		protected override void Awake()
		{
			base.Awake();

			textMesh = GetComponentInChildren<TextMesh>();
			textMesh.text = "No Hand Hovering";

            //attachmentFlags = Hand.defaultAttachmentFlags & (~Hand.AttachmentFlags.DetachOthers) & (~Hand.AttachmentFlags.VelocityMovement);
		}


		//-------------------------------------------------
		// Called when a Hand starts hovering over this object
		//-------------------------------------------------
		protected override void OnHandHoverBegin( Hand hand )
		{
			base.OnHandHoverBegin(hand);
			textMesh.text = "Hovering hand: " + hand.name;
		}


		//-------------------------------------------------
		// Called when a Hand stops hovering over this object
		//-------------------------------------------------
		protected override void OnHandHoverEnd( Hand hand )
		{
			base.OnHandHoverEnd(hand);
			textMesh.text = "No Hand Hovering";
		}


		//-------------------------------------------------
		// Called every Update() while a Hand is hovering over this object
		//-------------------------------------------------
		protected override void HandHoverUpdate( Hand hand )
		{
			base.HandHoverUpdate(hand);

            
			bool isGrabEnding = Player.instance.input_manager.GetGripUp(hand);
			bool starting_grab = Player.instance.input_manager.GetGripDown(hand);

            if (attachedToHand == null && starting_grab)
			{
                // Save our position/rotation so that we can restore it when we detach
                oldPosition = transform.position;
                oldRotation = transform.rotation;

                // Call this to continue receiving HandHoverUpdate messages,
                // and prevent the hand from hovering over anything else
                hand.HoverLock(this);

                // Attach this object to the hand
                hand.AttachGrabbable(this);
			}
            else if (isGrabEnding)
            {
                // Detach this object from the hand
                hand.DetachObject(gameObject);
                // Call this to undo HoverLock
                hand.HoverUnlock(this);
                // Restore position/rotation
                transform.position = oldPosition;
                transform.rotation = oldRotation;
            }
		}


		//-------------------------------------------------
		// Called when this GameObject becomes attached to the hand
		//-------------------------------------------------
		protected override void OnAttachedToHand( Hand hand )
		{
			base.OnAttachedToHand(hand);
			textMesh.text = "Attached to hand: " + hand.name;
			attachTime = Time.time;
		}


		//-------------------------------------------------
		// Called when this GameObject is detached from the hand
		//-------------------------------------------------
		protected override void OnDetachedFromHand( Hand hand )
		{
			base.OnDetachedFromHand(hand);
			textMesh.text = "Detached from hand: " + hand.name;
		}


		//-------------------------------------------------
		// Called every Update() while this GameObject is attached to the hand
		//-------------------------------------------------
		protected override void HandAttachedUpdate( Hand hand )
		{
			base.HandAttachedUpdate(hand);
			textMesh.text = "Attached to hand: " + hand.name + "\nAttached time: " + ( Time.time - attachTime ).ToString( "F2" );
		}


		//-------------------------------------------------
		// Called when this attached GameObject becomes the primary attached object
		//-------------------------------------------------
		protected override void OnHandFocusAcquired( Hand hand )
		{
			base.OnHandFocusAcquired(hand);
		}


		//-------------------------------------------------
		// Called when another attached GameObject becomes the primary attached object
		//-------------------------------------------------
		protected override void OnHandFocusLost( Hand hand )
		{
			base.OnHandFocusLost(hand);
		}
	}
}
