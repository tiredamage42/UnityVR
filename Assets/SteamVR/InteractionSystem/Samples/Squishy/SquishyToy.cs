using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace Valve.VR.InteractionSystem
{
    public class SquishyToy : Grabbable
    {
        public new SkinnedMeshRenderer renderer;

        public bool affectMaterial = true;

        [SteamVR_DefaultAction("Squeeze")]
        public SteamVR_Action_Single gripSqueeze;

        [SteamVR_DefaultAction("Squeeze")]
        public SteamVR_Action_Single pinchSqueeze;


       
        protected override void Start()
        {
            base.Start();
       
            if (renderer == null)
                renderer = GetComponent<SkinnedMeshRenderer>();
        }

        protected override void Update()
        {
            base.Update();
            float grip = 0;
            float pinch = 0;

            if (attachedToHand)
            {
                grip = gripSqueeze.GetAxis(attachedToHand.handType);
                pinch = pinchSqueeze.GetAxis(attachedToHand.handType);
            }

            renderer.SetBlendShapeWeight(0, Mathf.Lerp(renderer.GetBlendShapeWeight(0), grip * 150, Time.deltaTime * 10));

            if (renderer.sharedMesh.blendShapeCount > 1) // make sure there's a pinch blend shape
                renderer.SetBlendShapeWeight(1, Mathf.Lerp(renderer.GetBlendShapeWeight(1), pinch * 200, Time.deltaTime * 10));

            if (affectMaterial)
            {
                renderer.material.SetFloat("_Deform", Mathf.Pow(grip * 1.5f, 0.5f));
                if (renderer.material.HasProperty("_PinchDeform"))
                {
                    renderer.material.SetFloat("_PinchDeform", Mathf.Pow(pinch * 2.0f, 0.5f));
                }
            }
        }
    }
}