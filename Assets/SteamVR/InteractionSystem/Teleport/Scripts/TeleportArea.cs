// Purpose: An area that the player can teleport to
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Valve.VR.InteractionSystem{
	public class TeleportArea : TeleportMarkerBase{
		//public Bounds meshBounds { get; private set; }

		//MeshRenderer areaMesh;
		//int tintColorId = 0;
		//Color visibleTintColor = Color.clear;
		//Color highlightedTintColor = Color.clear;
		//Color lockedTintColor = Color.clear;
		//bool highlighted = false;

		public void Awake()
		{
			//areaMesh = GetComponent<MeshRenderer>();
			//tintColorId = Shader.PropertyToID( "_TintColor" );
			CalculateBounds();
		}


		public void Start()
		{
			//visibleTintColor = Teleport.instance.areaVisibleMaterial.GetColor( tintColorId );
			//highlightedTintColor = Teleport.instance.areaHighlightedMaterial.GetColor( tintColorId );
			//lockedTintColor = Teleport.instance.areaLockedMaterial.GetColor( tintColorId );
		}

		public override bool ShouldActivate( Vector3 playerPosition ) {
			return true;
		}
		public override bool ShouldMovePlayer() {
			return true;
		}

		public override void Highlight( bool highlight ) {
			if ( !locked ) {
				//highlighted = highlight;
				//areaMesh.material = highlight ? Teleport.instance.areaHighlightedMaterial : Teleport.instance.areaVisibleMaterial;
			}
		}

		public override void SetAlpha( float tintAlpha, float alphaPercent ) {
			//Color tintedColor = GetTintColor();
			//tintedColor.a *= alphaPercent;
			//areaMesh.material.SetColor( tintColorId, tintedColor );
		}
		public override void UpdateVisuals(){
			//areaMesh.material = locked ? Teleport.instance.areaLockedMaterial : Teleport.instance.areaVisibleMaterial;
		}

		public void UpdateVisualsInEditor() {
			//areaMesh = GetComponent<MeshRenderer>();
			//areaMesh.sharedMaterial = locked ? Teleport.instance.areaLockedMaterial : Teleport.instance.areaVisibleMaterial;
		}


		bool CalculateBounds(){
			MeshFilter meshFilter = GetComponent<MeshFilter>();
			if ( meshFilter == null )
				return false;
			Mesh mesh = meshFilter.sharedMesh;
			if ( mesh == null )
				return false;
			//meshBounds = mesh.bounds;
			return true;
		}

		//Color GetTintColor(){
		//	return locked ? lockedTintColor : (highlighted ? highlightedTintColor : visibleTintColor);
		//}
	}


#if UNITY_EDITOR
	[CustomEditor( typeof( TeleportArea ) )]
	public class TeleportAreaEditor : Editor{
		void OnEnable() {
			if ( Selection.activeTransform != null ){
				TeleportArea teleportArea = Selection.activeTransform.GetComponent<TeleportArea>();
				if ( teleportArea != null ){
					teleportArea.UpdateVisualsInEditor();
				}
			}
		}
		public override void OnInspectorGUI(){
			DrawDefaultInspector();
			if ( Selection.activeTransform != null ){
				TeleportArea teleportArea = Selection.activeTransform.GetComponent<TeleportArea>();
				if ( GUI.changed && teleportArea != null ){
					teleportArea.UpdateVisualsInEditor();
				}
			}
		}
	}
#endif
}
