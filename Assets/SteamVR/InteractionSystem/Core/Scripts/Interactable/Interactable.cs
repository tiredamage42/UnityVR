// Purpose: This object will get hover events and can be attached to the hands

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Valve.VR.InteractionSystem
{
	//-------------------------------------------------------------------------
	public class Interactable : MonoBehaviour
    {
        [Header("Interactable Options:")]
        [Tooltip("Set whether or not you want this interactible to highlight when hovering over it")]
        public bool highlightOnHover = true;
        MeshRenderer[] highlightRenderers, existingRenderers;
        GameObject highlightHolder;
        SkinnedMeshRenderer[] highlightSkinnedRenderers, existingSkinnedRenderers;
        static Material highlightMat;

        [Tooltip("An array of child gameObjects to not render a highlight for. Things like transparent parts, vfx, etc.")]
        public GameObject[] hideHighlight;

        public bool isDestroying { get; protected set; }
        public bool isHovering { get; protected set; }
        public bool wasHovering { get; protected set; }

        protected virtual void Start()
        {
            highlightMat = (Material)Resources.Load("SteamVR_HoverHighlight", typeof(Material));
            if (highlightMat == null)
                Debug.LogError("Hover Highlight Material is missing. Please create a material named 'SteamVR_HoverHighlight' and place it in a Resources folder");
        }

        bool ShouldIgnoreHighlight(Component component)
        {
            return ShouldIgnore(component.gameObject);
        }
        bool ShouldIgnore(GameObject check)
        {
            for (int i = 0; i < hideHighlight.Length; i++) {
                if (check == hideHighlight[i])
                    return true;
            }
            return false;
        }

        void CreateHighlightRenderers()
        {
            existingSkinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            highlightHolder = new GameObject("Highlighter");
            highlightSkinnedRenderers = new SkinnedMeshRenderer[existingSkinnedRenderers.Length];

            for (int i = 0; i < existingSkinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer existingSkinned = existingSkinnedRenderers[i];

                if (ShouldIgnoreHighlight(existingSkinned))
                    continue;

                GameObject newSkinnedHolder = new GameObject("SkinnedHolder");
                newSkinnedHolder.transform.parent = highlightHolder.transform;
                SkinnedMeshRenderer newSkinned = newSkinnedHolder.AddComponent<SkinnedMeshRenderer>();
                
                Material[] materials = new Material[existingSkinned.sharedMaterials.Length];
                
                for (int j = 0; j < materials.Length; j++)
                    materials[j] = highlightMat;
                
                newSkinned.sharedMaterials = materials;
                newSkinned.sharedMesh = existingSkinned.sharedMesh;
                newSkinned.rootBone = existingSkinned.rootBone;
                newSkinned.updateWhenOffscreen = existingSkinned.updateWhenOffscreen;
                newSkinned.bones = existingSkinned.bones;

                highlightSkinnedRenderers[i] = newSkinned;
            }

            MeshFilter[] existingFilters = this.GetComponentsInChildren<MeshFilter>(true);
            existingRenderers = new MeshRenderer[existingFilters.Length];
            highlightRenderers = new MeshRenderer[existingFilters.Length];

            for (int i = 0; i < existingFilters.Length; i++)
            {
                MeshFilter existingFilter = existingFilters[i];
                MeshRenderer existingRenderer = existingFilter.GetComponent<MeshRenderer>();

                if (existingFilter == null || existingRenderer == null || ShouldIgnoreHighlight(existingFilter))
                    continue;

                GameObject newFilterHolder = new GameObject("FilterHolder");
                newFilterHolder.transform.parent = highlightHolder.transform;
                MeshFilter newFilter = newFilterHolder.AddComponent<MeshFilter>();
                newFilter.sharedMesh = existingFilter.sharedMesh;
                MeshRenderer newRenderer = newFilterHolder.AddComponent<MeshRenderer>();

                Material[] materials = new Material[existingRenderer.sharedMaterials.Length];
                for (int j = 0; j < materials.Length; j++)
                    materials[j] = highlightMat;
                
                newRenderer.sharedMaterials = materials;

                highlightRenderers[i] = newRenderer;
                existingRenderers[i] = existingRenderer;
            }
        }



        void DisableHighlightRenderers () {
            for (int i = 0; i < existingSkinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer highlightSkinned = highlightSkinnedRenderers[i];
                if (highlightSkinned != null)
                    highlightSkinned.enabled = false;

            }

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                MeshRenderer highlightRenderer = highlightRenderers[i];
                if (highlightRenderer != null)
                    highlightRenderer.enabled = false;
            }
        }

        protected virtual bool DisableHighlight () {
            return false;
        }
        
        void UpdateHighlightRenderers()
        {
            if (highlightHolder == null)
                return;
            if (DisableHighlight()) {
                DisableHighlightRenderers();
                return;
            }

            for (int i = 0; i < existingSkinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer existingSkinned = existingSkinnedRenderers[i];
                SkinnedMeshRenderer highlightSkinned = highlightSkinnedRenderers[i];

                if (existingSkinned != null && highlightSkinned != null)
                {
                    highlightSkinned.transform.position = existingSkinned.transform.position;
                    highlightSkinned.transform.rotation = existingSkinned.transform.rotation;
                    highlightSkinned.transform.localScale = existingSkinned.transform.lossyScale;
                    highlightSkinned.localBounds = existingSkinned.localBounds;
                    highlightSkinned.enabled = isHovering && existingSkinned.enabled && existingSkinned.gameObject.activeInHierarchy;
                    int blendShapeCount = existingSkinned.sharedMesh.blendShapeCount;
                    for (int j = 0; j < blendShapeCount; j++)
                        highlightSkinned.SetBlendShapeWeight(j, existingSkinned.GetBlendShapeWeight(j));
                    
                }
                else if (highlightSkinned != null)
                    highlightSkinned.enabled = false;

            }

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                MeshRenderer existingRenderer = existingRenderers[i];
                MeshRenderer highlightRenderer = highlightRenderers[i];

                if (existingRenderer != null && highlightRenderer != null)
                {
                    highlightRenderer.transform.position = existingRenderer.transform.position;
                    highlightRenderer.transform.rotation = existingRenderer.transform.rotation;
                    highlightRenderer.transform.localScale = existingRenderer.transform.lossyScale;
                    highlightRenderer.enabled = isHovering && existingRenderer.enabled && existingRenderer.gameObject.activeInHierarchy;
                }
                else if (highlightRenderer != null)
                    highlightRenderer.enabled = false;
            }
        }

        protected virtual void OnHandHoverBegin (Hand hand) {

        }
        protected virtual void OnHandHoverEnd (Hand hand) {

        }

        protected virtual void HandHoverUpdate (Hand hand)
        {
            if (highlightOnHover)
            {
                if (!wasHovering)
                {
                    isHovering = true;
                    CreateHighlightRenderers();
                    UpdateHighlightRenderers();
                }

            }
            isHovering = true;
        }


        protected virtual void Update()
        {
            wasHovering = isHovering;

            if (highlightOnHover)
            {
                UpdateHighlightRenderers();

                if (wasHovering == false && isHovering == false && highlightHolder != null)
                    Destroy(highlightHolder);

                isHovering = false;
            }
        }
        
        protected virtual void OnDestroy()
        {
            isDestroying = true;
        }
    }
}
