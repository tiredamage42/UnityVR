// Purpose: Basic throwable object

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
	//[RequireComponent( typeof( Rigidbody ) )]
    [RequireComponent( typeof(VelocityEstimator))]
	public class Grabbable : Interactable
	{
        [Header("Grabbable Options:")]
        public GrabbableParameters parameters;

        
    
        [System.NonSerialized]
        public Hand attachedToHand;



	
		[Tooltip( "When detaching the object, should it return to its original parent?" )]
		public bool restoreOriginalParent = false;

		protected VelocityEstimator velocityEstimator;
        protected bool attached = false;
        
		
        
        protected RigidbodyInterpolation hadInterpolation = RigidbodyInterpolation.None;

        protected Rigidbody rb;

      
        protected virtual void Awake()
		{
			velocityEstimator = GetComponent<VelocityEstimator>();
            
            rb = GetComponent<Rigidbody>();
            if (rb)
            {

            rb.maxAngularVelocity = 50.0f;
            }

		}


        //-------------------------------------------------
        protected override void OnHandHoverBegin( Hand hand )
		{
            base.OnHandHoverBegin(hand);
			bool showHint = true;

            // "Catch" the throwable by holding down the interaction button instead of pressing it.
            // Only do this if the throwable is moving faster than the prescribed threshold speed,
            // and if it isn't attached to another hand
            if ( !attached && parameters.catchingSpeedThreshold != -1)
            {
                float catchingThreshold = parameters.catchingSpeedThreshold * SteamVR_Utils.GetLossyScale(Player.instance.transform);

                bool grabbing = Player.instance.input_manager.GetGrip(hand);
		
                if ( grabbing )
				{
					if (rb.velocity.magnitude >= catchingThreshold)
					{
						hand.AttachGrabbable( this );
                        showHint = false;
					}
				}
			}

			if ( showHint )
			{
                Player.instance.input_manager.ShowGrabHint(hand);
			}
		}


        protected override void OnHandHoverEnd( Hand hand )
		{
            base.OnHandHoverEnd(hand);
            Player.instance.input_manager.HideGrabHint(hand);

        }


        //-------------------------------------------------
        protected override void HandHoverUpdate( Hand hand )
        {
            base.HandHoverUpdate (hand);

            bool grabbing = Player.instance.input_manager.GetGripDown(hand);
            if (grabbing)
            {
				hand.AttachGrabbable( this );
                Player.instance.input_manager.HideGrabHint(hand);
            
            }
		}

        //-------------------------------------------------
        protected virtual void OnAttachedToHand( Hand hand )
		{
            //if ( onAttachedToHand != null )
			//	onAttachedToHand.Invoke( hand );
			
            attachedToHand = hand;
            hadInterpolation = rb.interpolation;

            attached = true;

			//onPickUp.Invoke();

			hand.HoverLock( null );
            
            rb.interpolation = RigidbodyInterpolation.None;

		    velocityEstimator.BeginEstimatingVelocity();
			
        }

        public virtual void GetReleaseVelocities(Hand hand, out Vector3 velocity, out Vector3 angularVelocity)
        {
            switch (parameters.releaseVelocityStyle)
            {
                case ReleaseStyle.ShortEstimation:
                    velocityEstimator.FinishEstimatingVelocity();
                    velocity = velocityEstimator.GetVelocityEstimate();
                    angularVelocity = velocityEstimator.GetAngularVelocityEstimate();
                    break;
                case ReleaseStyle.AdvancedEstimation:
                    hand.GetEstimatedPeakVelocities(out velocity, out angularVelocity);
                    break;
                case ReleaseStyle.GetFromHand:
                    velocity = hand.GetTrackedObjectVelocity(parameters.releaseVelocityTimeOffset);
                    angularVelocity = hand.GetTrackedObjectAngularVelocity(parameters.releaseVelocityTimeOffset);
                    break;
                default:
                case ReleaseStyle.NoChange:
                    velocity = rb.velocity;
                    angularVelocity = rb.angularVelocity;
                    break;
            }

            if (parameters.releaseVelocityStyle != ReleaseStyle.NoChange)
                velocity *= parameters.scaleReleaseVelocity;
        }

        protected virtual void HandAttachedUpdate(Hand hand)
        {
            bool grabbing_end = Player.instance.input_manager.GetGripUp(hand);
            if (grabbing_end)   
            {
                hand.DetachObject(gameObject, restoreOriginalParent);

                // Uncomment to detach ourselves late in the frame.
                // This is so that any vehicles the player is attached to
                // have a chance to finish updating themselves.
                // If we detach now, our position could be behind what it
                // will be at the end of the frame, and the object may appear
                // to teleport behind the hand when the player releases it.
                //StartCoroutine( LateDetach( hand ) );
            }
        }


        protected virtual IEnumerator LateDetach( Hand hand )
		{
			yield return new WaitForEndOfFrame();
			hand.DetachObject( gameObject, restoreOriginalParent );
		}

        protected virtual void OnHandFocusAcquired( Hand hand )
		{
			gameObject.SetActive( true );
			velocityEstimator.BeginEstimatingVelocity();
		}
        protected virtual void OnHandFocusLost( Hand hand )
		{
			gameObject.SetActive( false );
			velocityEstimator.FinishEstimatingVelocity();
		}

        protected virtual void OnDetachedFromHand(Hand hand)
        {
            //if ( onDetachedFromHand != null )
			//	onDetachedFromHand.Invoke( hand );	
            
            attachedToHand = null;


            attached = false;
            //onDetachFromHand.Invoke();
            hand.HoverUnlock(null);
    
            rb.interpolation = hadInterpolation;

            Vector3 velocity;
            Vector3 angularVelocity;
            GetReleaseVelocities(hand, out velocity, out angularVelocity);

            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }




protected override bool DisableHighlight () {
            return attachedToHand == false;
        }
        


        

        protected override void OnDestroy() {
            base.OnDestroy();
            if (attachedToHand != null) {
                attachedToHand.ForceHoverUnlock();
                attachedToHand.DetachObject(this.gameObject, false);
            }

        }
	}

    public enum ReleaseStyle { NoChange, GetFromHand, ShortEstimation, AdvancedEstimation }
}
