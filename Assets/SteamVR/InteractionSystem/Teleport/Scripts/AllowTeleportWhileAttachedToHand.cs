using UnityEngine;
namespace Valve.VR.InteractionSystem{
	// Adding this component to an object will allow the player to 
	// initiate teleporting while that object is attached to their hand
	public class AllowTeleportWhileAttachedToHand : MonoBehaviour{
		public bool teleportAllowed = true;
		public bool overrideHoverLock = true;
	}
}
