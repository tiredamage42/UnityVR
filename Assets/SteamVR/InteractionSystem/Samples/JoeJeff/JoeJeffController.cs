using UnityEngine;
using System.Collections;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace Valve.VR.InteractionSystem.Sample
{
    public class JoeJeffController : Grabbable
    {
        public Transform Joystick;
        public float joyMove = 0.1f;

        
        [SteamVR_DefaultActionSet("platformer")]
        public SteamVR_ActionSet actionSet;

        [SteamVR_DefaultAction("Move", "platformer")]
        public SteamVR_Action_Vector2 a_move;

        [SteamVR_DefaultAction("Jump", "platformer")]
        public SteamVR_Action_Boolean a_jump;


        public JoeJeff character;

        public Renderer jumpHighlight;


        private Vector3 movement;
        private bool jump;
        private float glow;
        private SteamVR_Input_Sources hand;
        bool was_attached;
        
        protected override void Update()
        {
            base.Update();

              if (attachedToHand)
            {

                if (!was_attached) {
                    


                    actionSet.ActivatePrimary();


                }


            }
            else {
                if (was_attached) {
                    


                    
                    actionSet.Deactivate();

                }



            }
            was_attached = attachedToHand != null;


            if (attachedToHand)
            {
                hand = attachedToHand.handType;
                Vector2 m = a_move.GetAxis(hand);
                movement = new Vector3(m.x, 0, m.y);

                jump = a_jump.GetStateDown(hand);
                glow = Mathf.Lerp(glow, a_jump.GetState(hand) ? 1.5f : 1.0f, Time.deltaTime * 20);
            }
            else
            {
                movement = Vector2.zero;
                jump = false;
                glow = 0;
            }

            Joystick.localPosition = movement * joyMove;

            float rot = transform.eulerAngles.y;

            movement = Quaternion.AngleAxis(rot, Vector3.up) * movement;

            jumpHighlight.sharedMaterial.SetColor("_EmissionColor", Color.white * glow);

            character.Move(movement * 2, jump);
        }
    }
}