// Purpose: Base class for all the objects that the player can teleport to
using UnityEngine;
namespace Valve.VR.InteractionSystem{
	public abstract class TeleportMarkerBase : MonoBehaviour {
		public bool locked = false;
		public bool markerActive = true;
		public bool scene_teleport;
		public string switchToScene;
		
		public virtual bool showReticle { get { return true; } }
		public void SetLocked( bool locked ) {
			this.locked = locked;
			UpdateVisuals();
		}
		protected abstract void UpdateVisuals();
		public abstract void Highlight( bool highlight );
		public abstract void SetAlpha( float tintAlpha, float alphaPercent );
		public abstract bool ShouldActivate( Vector3 playerPosition );
	}
}
