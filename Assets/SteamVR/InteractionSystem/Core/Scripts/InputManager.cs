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

        [SteamVR_DefaultAction("Haptic")]
        public SteamVR_Action_Vibration hapticAction;

        [SteamVR_DefaultAction("InteractUI")]
        public SteamVR_Action_Boolean uiInteractAction;


        public bool GetPinchDown (Hand hand) { return grabPinchAction.GetStateDown(hand.handType); }
        public bool GetPinchUp (Hand hand) { return grabPinchAction.GetStateUp(hand.handType); }
        public bool GetPinch (Hand hand) { return grabPinchAction.GetState(hand.handType); }
        
        public bool GetUIInteractionDown (Hand hand) { return uiInteractAction.GetStateDown(hand.handType); }
        
        public bool GetGripDown (Hand hand) { return grabGripAction.GetStateDown(hand.handType); }
        public bool GetGripUp (Hand hand) { return grabGripAction.GetStateUp(hand.handType); }
        public bool GetGrip (Hand hand) { return grabGripAction.GetState(hand.handType); }

        public void ShowInteractUIHint(Hand hand)
        {
            ControllerButtonHints.ShowButtonHint(hand, uiInteractAction); //todo: assess
        }
        public void HideInteractUIHint (Hand hand) {
            ControllerButtonHints.HideButtonHint(hand, uiInteractAction); //todo: assess
        }
        public void ShowGrabHint(Hand hand)
        {
            ControllerButtonHints.ShowButtonHint(hand, grabGripAction); //todo: assess
        }
        public void ShowGrabHint(Hand hand, string text)
        {
            ControllerButtonHints.ShowTextHint(hand, grabGripAction, text);
        }
        public void HideGrabHint(Hand hand)
        {
            ControllerButtonHints.HideButtonHint(hand, grabGripAction); //todo: assess
        }


  /*
        Trigger the haptics at a certain time for a certain length

        secondsFromNow: How long from the current time to execute the action (in seconds - can be 0)
        durationSeconds: How long the haptic action should last (in seconds)
        frequency: How often the haptic motor should bounce (0 - 320 in hz. The lower end being more useful)
        amplitude: How intense the haptic action should be (0 - 1)
        inputSource: The device you would like to execute the haptic action. Any if the action is not device specific.

        void SteamVR_Action_Vibration.Execute(
            float secondsFromNow, 
            float durationSeconds, 
            float frequency, 
            float amplitude, 
            SteamVR_Input_Sources inputSource
        )
        */
      
        public void TriggerHapticPulse(Hand hand, ushort microSecondsDuration)
        {
            float seconds = (float)microSecondsDuration / 1000000f;
            hapticAction.Execute(0, seconds, 1f / seconds, 1, hand.handType);
        }
        public void TriggerHapticPulse(Hand hand, float duration, float frequency, float amplitude)
        {
            hapticAction.Execute(0, duration, frequency, amplitude, hand.handType);
        }

    }
}
