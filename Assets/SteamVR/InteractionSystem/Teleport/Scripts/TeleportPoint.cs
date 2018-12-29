// Purpose: Single location that the player can teleport to

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Valve.VR.InteractionSystem{
	public class TeleportPoint : TeleportMarkerBase{
		

		public string title;
		public Color titleVisibleColor;
		public Color titleHighlightedColor;
		public Color titleLockedColor;
		public bool playerSpawnPoint = false;

		MeshRenderer markerMesh, switchSceneIcon, moveLocationIcon, lockedIcon, pointIcon;
		Transform lookAtJointTransform;
		new Animation animation;
		Text titleText;
		private Vector3 lookAtPosition = Vector3.zero;
		private int tintColorID = 0;
		Color tintColor = Color.clear;
		Color titleColor = Color.clear;
		float fullTitleAlpha = 0.0f;

		const string switchSceneAnimation = "switch_scenes_idle";
		const string moveLocationAnimation = "move_location_idle";
		const string lockedAnimation = "locked_idle";

		public override bool showReticle { get { return false; } }



		Player player;
		Teleport teleportation;
		
		void Awake()
		{
			GetRelevantComponents();
			animation = GetComponent<Animation>();
			tintColorID = Shader.PropertyToID( "_TintColor" );
			moveLocationIcon.gameObject.SetActive( false );
			switchSceneIcon.gameObject.SetActive( false );
			lockedIcon.gameObject.SetActive( false );
			teleportation = Teleport.instance;
			UpdateVisuals();
		}

		void Start()
		{
			player = Player.instance;
		}
		void Update()
		{
			if ( Application.isPlaying )
			{
				lookAtPosition.x = player.hmdTransform.position.x;
				lookAtPosition.y = lookAtJointTransform.position.y;
				lookAtPosition.z = player.hmdTransform.position.z;

				lookAtJointTransform.LookAt( lookAtPosition );
			}
		}
		public override bool ShouldActivate( Vector3 playerPosition )
		{
			return ( Vector3.Distance( transform.position, playerPosition ) > 1.0f );
		}

		public override void Highlight( bool highlight )
		{
			if ( !locked ) SetMeshMaterials( highlight ? teleportation.pointHighlightedMaterial : teleportation.pointVisibleMaterial, highlight ? titleHighlightedColor : titleVisibleColor );
			
			pointIcon.gameObject.SetActive( highlight );
			if ( highlight )
				animation.Play();
			else 
				animation.Stop();
		}
		protected override void UpdateVisuals()
		{
			SetMeshMaterials( locked ? teleportation.pointLockedMaterial : teleportation.pointVisibleMaterial, locked ? titleLockedColor : titleVisibleColor );
			pointIcon = locked ? lockedIcon : (scene_teleport ? switchSceneIcon : moveLocationIcon);
			animation.clip = animation.GetClip( locked ? lockedAnimation : (scene_teleport ? switchSceneAnimation : moveLocationAnimation) );
			titleText.text = title;
		}
		public override void SetAlpha( float tintAlpha, float alphaPercent )
		{
			tintColor = markerMesh.material.GetColor( tintColorID );
			tintColor.a = tintAlpha;
			markerMesh.material.SetColor( tintColorID, tintColor );
			switchSceneIcon.material.SetColor( tintColorID, tintColor );
			moveLocationIcon.material.SetColor( tintColorID, tintColor );
			lockedIcon.material.SetColor( tintColorID, tintColor );
			titleColor.a = fullTitleAlpha * alphaPercent;
			titleText.color = titleColor;
		}
		void SetMeshMaterials( Material material, Color textColor )
		{
			markerMesh.material = material;
			switchSceneIcon.material = material;
			moveLocationIcon.material = material;
			lockedIcon.material = material;
			titleColor = textColor;
			fullTitleAlpha = textColor.a;
			titleText.color = titleColor;
		}
		void GetRelevantComponents()
		{
			if (markerMesh != null)
				return;
			markerMesh = transform.Find( "teleport_marker_mesh" ).GetComponent<MeshRenderer>();
			switchSceneIcon = transform.Find( "teleport_marker_lookat_joint/teleport_marker_icons/switch_scenes_icon" ).GetComponent<MeshRenderer>();
			moveLocationIcon = transform.Find( "teleport_marker_lookat_joint/teleport_marker_icons/move_location_icon" ).GetComponent<MeshRenderer>();
			lockedIcon = transform.Find( "teleport_marker_lookat_joint/teleport_marker_icons/locked_icon" ).GetComponent<MeshRenderer>();
			lookAtJointTransform = transform.Find( "teleport_marker_lookat_joint" );
			titleText = transform.Find( "teleport_marker_lookat_joint/teleport_marker_canvas/teleport_marker_canvas_text" ).GetComponent<Text>();
		}




		public void UpdateVisualsInEditor() {
			if ( Application.isPlaying )
				return;
			GetRelevantComponents();
			lockedIcon.gameObject.SetActive( locked );
			moveLocationIcon.gameObject.SetActive( !scene_teleport && !locked );
			switchSceneIcon.gameObject.SetActive( scene_teleport && !locked );
			markerMesh.sharedMaterial = locked ? Teleport.instance.pointLockedMaterial : Teleport.instance.pointVisibleMaterial;
			lockedIcon.sharedMaterial = Teleport.instance.pointLockedMaterial;
			switchSceneIcon.sharedMaterial = Teleport.instance.pointVisibleMaterial;
			moveLocationIcon.sharedMaterial = Teleport.instance.pointVisibleMaterial;
			titleText.color = locked ? titleLockedColor : titleVisibleColor;
			titleText.text = title;
		}
	}


#if UNITY_EDITOR
	[CustomEditor( typeof( TeleportPoint ) )]
	public class TeleportPointEditor : Editor{
		void OnEnable(){
			if ( Selection.activeTransform ){
				TeleportPoint teleportPoint = Selection.activeTransform.GetComponent<TeleportPoint>();
				teleportPoint.UpdateVisualsInEditor();
			}
		}
		public override void OnInspectorGUI(){
			DrawDefaultInspector();
			if ( Selection.activeTransform ){
				TeleportPoint teleportPoint = Selection.activeTransform.GetComponent<TeleportPoint>();
				if ( GUI.changed ){
					teleportPoint.UpdateVisualsInEditor();
				}
			}
		}
	}
#endif
}
