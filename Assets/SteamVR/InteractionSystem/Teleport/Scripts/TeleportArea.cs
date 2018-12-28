// Purpose: An area that the player can teleport to
using UnityEngine;
namespace Valve.VR.InteractionSystem{
	public class TeleportArea : TeleportMarkerBase{
		public override bool ShouldActivate( Vector3 playerPosition ) {
			return true;
		}
		public override void Highlight( bool highlight ) {}
		public override void SetAlpha( float tintAlpha, float alphaPercent ) {}
		protected override void UpdateVisuals(){}
	}
}
