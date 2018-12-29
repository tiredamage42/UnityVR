using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Valve.VR.InteractionSystem
{

    [System.Serializable] public class InputManager 
    {
        [SteamVR_DefaultAction("GrabPinch")]
        public SteamVR_Action_Boolean grabPinchAction;

        [SteamVR_DefaultAction("GrabGrip")]
        public SteamVR_Action_Boolean grabGripAction;

        public bool GetPinchDown (Hand hand) { return grabPinchAction.GetStateDown(hand.handType); }
        public bool GetPinchUp (Hand hand) { return grabPinchAction.GetStateUp(hand.handType); }
        public bool GetPinch (Hand hand) { return grabPinchAction.GetState(hand.handType); }
        public bool GetGripDown (Hand hand) { return grabGripAction.GetStateDown(hand.handType); }
        public bool GetGripUp (Hand hand) { return grabGripAction.GetStateUp(hand.handType); }
        public bool GetGrip (Hand hand) { return grabGripAction.GetState(hand.handType); }

    }
}
