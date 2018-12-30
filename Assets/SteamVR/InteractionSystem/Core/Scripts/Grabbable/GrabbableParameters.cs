using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Valve.VR.InteractionSystem
{

[CreateAssetMenu()]
public class GrabbableParameters : ScriptableObject
{

        [EnumFlags]
		[Tooltip( "The flags used to attach this object to the hand." )]
		public Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags;
        public Vector3 attach_position_offset, attach_rotation_offset;

        [Tooltip("Hide the whole hand on attachment and show on detach")]
        public bool hideHandOnAttach = true;
        [Tooltip("Hide the skeleton part of the hand on attachment and show on detach")]
        public bool hideSkeletonOnAttach = false;
        [Tooltip("Hide the controller part of the hand on attachment and show on detach")]
        public bool hideControllerOnAttach = false;

        [Tooltip("The integer in the animator to trigger on pickup. 0 for none")]
        //public int handAnimationOnPickup = 0;
        public RenderModel.AnimationState handAnimationOnPickup1 = RenderModel.AnimationState.Rest;

        [Tooltip("The range of motion to set on the skeleton. None for no change.")]
        public SkeletalMotionRangeChange setRangeOfMotionOnPickup = SkeletalMotionRangeChange.None;



	[Tooltip( "How fast must this object be moving to attach due to a trigger hold instead of a trigger press? (-1 to disable)" )]
        public float catchingSpeedThreshold = -1;
        public ReleaseStyle releaseVelocityStyle = ReleaseStyle.GetFromHand;

        [Tooltip("The time offset used when releasing the object with the RawFromHand option")]
        public float releaseVelocityTimeOffset = -0.011f;
        public float scaleReleaseVelocity = 1.1f;

	
    
}
}