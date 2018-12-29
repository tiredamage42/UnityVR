//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Basic throwable object
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    public class ModalThrowable : Throwable
    {
        [Tooltip("The local point which acts as a positional and rotational offset to use while held with a grip type grab")]
        public Transform gripOffset;

        [Tooltip("The local point which acts as a positional and rotational offset to use while held with a pinch type grab")]
        public Transform pinchOffset;
        
        protected override void HandHoverUpdate(Hand hand)
        {
            bool grabbing = Player.instance.input_manager.GetGripDown(hand);

            //GrabTypes startingGrabType = hand.GetGrabStarting();


            //if (startingGrabType != GrabTypes.None)
            if (grabbing)
            
            {
                hand.AttachInteractable(interactable, GrabTypes.None, attachmentOffset);
                /*
                if (startingGrabType == GrabTypes.Pinch)
                {
                    hand.AttachInteractable(interactable, startingGrabType, pinchOffset);
                }
                else if (startingGrabType == GrabTypes.Grip)
                {
                    hand.AttachInteractable(interactable, startingGrabType, gripOffset);
                }
                else
                {
                    hand.AttachInteractable(interactable, startingGrabType, attachmentOffset);
                }
                */
                

                hand.HideGrabHint();
            }
        }
    }
}