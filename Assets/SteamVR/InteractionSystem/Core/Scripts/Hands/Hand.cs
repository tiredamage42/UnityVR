//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: The hands used by the player in the vr interaction system
//
//=============================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;
using System.Threading;

namespace Valve.VR.InteractionSystem
{
    //-------------------------------------------------------------------------
    // Links with an appropriate SteamVR controller and facilitates
    // interactions with objects in the virtual world.
    //-------------------------------------------------------------------------
    public partial class Hand : MonoBehaviour
    {
        
        public Hand otherHand;
        public SteamVR_Input_Sources handType;

        public SteamVR_Behaviour_Pose trackedObject;

        [SteamVR_DefaultAction("GrabPinch")]
        public SteamVR_Action_Boolean grabPinchAction;

        [SteamVR_DefaultAction("GrabGrip")]
        public SteamVR_Action_Boolean grabGripAction;

        [SteamVR_DefaultAction("Haptic")]
        public SteamVR_Action_Vibration hapticAction;

        [SteamVR_DefaultAction("InteractUI")]
        public SteamVR_Action_Boolean uiInteractAction;

        public bool useHoverSphere = true;
        public Transform hoverSphereTransform;
        public float hoverSphereRadius = 0.05f;
        public LayerMask hoverLayerMask = -1;
        public float hoverUpdateInterval = 0.1f;

        const string controllerHoverComponent = "tip";
        public float controllerHoverRadius = 0.075f;

        public bool useFingerJointHover = true;
        public SteamVR_Skeleton_JointIndexEnum fingerJointHover = SteamVR_Skeleton_JointIndexEnum.indexTip;
        public float fingerJointHoverRadius = 0.025f;

        [Tooltip("A transform on the hand to center attached objects on")]
        public Transform objectAttachmentPoint;

        
        public GameObject renderModelPrefab;
        protected List<RenderModel> renderModels = new List<RenderModel>();
        protected RenderModel mainRenderModel;
        protected RenderModel hoverhighlightRenderModel;

        public bool showDebugText = false;
        public bool spewDebugText = false;
        public bool showDebugInteractables = false;

 

        
        public bool hoverLocked { get; private set; }

        
        private TextMesh debugText;
        private int prevOverlappingColliders = 0;

        private const int ColliderArraySize = 16;
        public Collider[] overlappingColliders;

        private Player playerInstance;

        private GameObject applicationLostFocusObject;

        private SteamVR_Events.Action inputFocusAction;

        public bool isActive
        {
            get
            {
                return trackedObject.isActive;
            }
        }

        public bool isPoseValid
        {
            get
            {
                return trackedObject.isValid;
            }
        }


        void HoverStatusChange(string suffix){
            if (_hoveringInteractable == null)
                return;
            HandDebugLog("Hover" + suffix + " " + _hoveringInteractable.gameObject);
            _hoveringInteractable.SendMessage("OnHandHover" + suffix, this, SendMessageOptions.DontRequireReceiver);
            //Note: The _hoveringInteractable can change after sending the OnHandHoverEnd message so we need to check it again before broadcasting this message
            if (_hoveringInteractable != null)
                this.BroadcastMessage("OnParentHandHover" + suffix, _hoveringInteractable, SendMessageOptions.DontRequireReceiver); // let objects attached to the hand know that a hover has ended
            
        }

        public Interactable _hoveringInteractable;
        // The Interactable object this Hand is currently hovering over
        public Interactable hoveringInteractable
        {
            get { return _hoveringInteractable; }
            set {
                if (_hoveringInteractable != value) {
                    HoverStatusChange("End");
                    _hoveringInteractable = value;
                    HoverStatusChange("Begin");
                }
            }
        }


   
        

        public void SetControllerVisibility(bool visible, bool permanent = false) {
            if (mainRenderModel != null)
                mainRenderModel.SetControllerVisibility(visible, permanent);
            if (hoverhighlightRenderModel != null)
                hoverhighlightRenderModel.SetControllerVisibility(visible, permanent);
        }
        void SetSkeletonVisibility (bool visible, bool permanent = false) {
            if (mainRenderModel != null)
                mainRenderModel.SetHandVisibility(visible, permanent);
            if (hoverhighlightRenderModel != null)
                hoverhighlightRenderModel.SetHandVisibility(visible, permanent);
        }
        void SetVisibility(bool visible){
            if (mainRenderModel != null)
                mainRenderModel.SetVisibility(visible);
        }

        public void SetSkeletonRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f) {
            for (int i = 0; i < renderModels.Count; i++) renderModels[i].SetSkeletonRangeOfMotion(newRangeOfMotion, blendOverSeconds);
        }
        void SetTemporarySkeletonRangeOfMotion(SkeletalMotionRangeChange temporaryRangeOfMotionChange, float blendOverSeconds = 0.1f) {
            for (int i = 0; i < renderModels.Count; i++) renderModels[i].SetTemporarySkeletonRangeOfMotion(temporaryRangeOfMotionChange, blendOverSeconds);
        }
        void ResetTemporarySkeletonRangeOfMotion(float blendOverSeconds = 0.1f ){
            for (int i = 0; i < renderModels.Count; i++) renderModels[i].ResetTemporarySkeletonRangeOfMotion(blendOverSeconds);
        }
        void SetAnimationState(RenderModel.AnimationState stateValue) {
            for (int i = 0; i < renderModels.Count; i++) renderModels[i].SetAnimationState(stateValue);
        }
        void StopAnimation() {
            for (int i = 0; i < renderModels.Count; i++) renderModels[i].StopAnimation();
        }

        public void ForceHoverUnlock()
        {
            hoverLocked = false;
        }

        protected virtual void Awake()
        {
            inputFocusAction = SteamVR_Events.InputFocusAction(OnInputFocus);

            if (hoverSphereTransform == null)
                hoverSphereTransform = this.transform;

            if (objectAttachmentPoint == null)
                objectAttachmentPoint = this.transform;

            applicationLostFocusObject = new GameObject("_application_lost_focus");
            applicationLostFocusObject.transform.parent = transform;
            applicationLostFocusObject.SetActive(false);

            if (trackedObject == null)
                trackedObject = this.gameObject.GetComponent<SteamVR_Behaviour_Pose>();

            trackedObject.onTransformUpdated.AddListener(OnTransformUpdated);
      
        }


        protected virtual void OnTransformUpdated(SteamVR_Action_Pose pose)
        {
            AttachmentsOnTransformUpdate();
        }

        protected virtual IEnumerator Start()
        {
            // save off player instance
            playerInstance = Player.instance;
            if (!playerInstance)
            {
                Debug.LogError("No player instance found in Hand Start()");
            }

            // allocate array for colliders
            overlappingColliders = new Collider[ColliderArraySize];

            Debug.Log( "Hand - initializing connection routine" );
            while (true)
            {
                if (isPoseValid)
                {
                    InitController();
                    break;
                }

                yield return null;
            }
        }


        protected virtual void UpdateHovering()
        {
            if (isActive == false)
            {
                Debug.Log("not active" + name);
                return;
            }

            if (hoverLocked)
            {
                Debug.Log("hover locked" + name);
                return;
            }

            if (applicationLostFocusObject.activeSelf)
            {
                Debug.Log("lost focus" + name);
                return;
            }

            float closestDistance = float.MaxValue;
            Interactable closestInteractable = null;

            if (useHoverSphere)
            {
                float scaledHoverRadius = hoverSphereRadius * Mathf.Abs(SteamVR_Utils.GetLossyScale(hoverSphereTransform));
                CheckHoveringForTransform(hoverSphereTransform.position, scaledHoverRadius, ref closestDistance, ref closestInteractable, Color.green);
            }

            if (mainRenderModel != null)
            {
                if ( mainRenderModel.IsControllerVisibile())
                {
                    float scaledHoverRadius = controllerHoverRadius * Mathf.Abs(SteamVR_Utils.GetLossyScale(this.transform));
                    CheckHoveringForTransform(mainRenderModel.GetControllerPosition(controllerHoverComponent), scaledHoverRadius / 2f, ref closestDistance, ref closestInteractable, Color.blue);
                }

                if (useFingerJointHover && mainRenderModel.IsHandVisibile())
                {
                    float scaledHoverRadius = fingerJointHoverRadius * Mathf.Abs(SteamVR_Utils.GetLossyScale(this.transform));
                    CheckHoveringForTransform(mainRenderModel.GetBonePosition((int)fingerJointHover), scaledHoverRadius / 2f, ref closestDistance, ref closestInteractable, Color.yellow);
                }

            }


            // Hover on this one
            hoveringInteractable = closestInteractable;
        }

        protected virtual bool CheckHoveringForTransform(Vector3 hoverPosition, float hoverRadius, ref float closestDistance, ref Interactable closestInteractable, Color debugColor)
        {
            bool foundCloser = false;

            // null out old vals
            for (int i = 0; i < overlappingColliders.Length; ++i)
            {
                overlappingColliders[i] = null;
            }

            int numColliding = Physics.OverlapSphereNonAlloc(hoverPosition, hoverRadius, overlappingColliders, hoverLayerMask.value);

            if (numColliding == ColliderArraySize)
                Debug.LogWarning("This hand is overlapping the max number of colliders: " + ColliderArraySize + ". Some collisions may be missed. Increase ColliderArraySize on Hand.cs");

            // DebugVar
            int iActualColliderCount = 0;

            // Pick the closest hovering
            for (int colliderIndex = 0; colliderIndex < overlappingColliders.Length; colliderIndex++)
            {
                Collider collider = overlappingColliders[colliderIndex];

                if (collider == null)
                    continue;

                Interactable contacting = collider.GetComponentInParent<Interactable>();

                // Yeah, it's null, skip
                if (contacting == null)
                    continue;

                // Ignore this collider for hovering
                IgnoreHovering ignore = collider.GetComponent<IgnoreHovering>();
                if (ignore != null)
                {
                    if (ignore.onlyIgnoreHand == null || ignore.onlyIgnoreHand == this)
                    {
                        continue;
                    }
                }

                // Can't hover over the object if it's attached
                bool hoveringOverAttached = false;
                for (int attachedIndex = 0; attachedIndex < attachedObjects.Count; attachedIndex++)
                {
                    if (attachedObjects[attachedIndex].attachedObject == contacting.gameObject)
                    {
                        hoveringOverAttached = true;
                        break;
                    }
                }
                if (hoveringOverAttached)
                    continue;

                // Occupied by another hand, so we can't touch it
                if (otherHand && otherHand.hoveringInteractable == contacting)
                    continue;

                // Best candidate so far...
                float distance = Vector3.Distance(contacting.transform.position, hoverPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = contacting;
                    foundCloser = true;
                }
                iActualColliderCount++;
            }

            if (showDebugInteractables && foundCloser)
            {
                Debug.DrawLine(hoverPosition, closestInteractable.transform.position, debugColor, .05f, false);
            }

            if (iActualColliderCount > 0 && iActualColliderCount != prevOverlappingColliders)
            {
                prevOverlappingColliders = iActualColliderCount;

                HandDebugLog("Found " + iActualColliderCount + " overlapping colliders.");
            }

            return foundCloser;
        }

        

        void UpdateDebugText()
        {
            if (showDebugText)
            {
                if (debugText == null)
                {
                    debugText = new GameObject("_debug_text").AddComponent<TextMesh>();
                    debugText.fontSize = 120;
                    debugText.characterSize = 0.001f;
                    debugText.transform.parent = transform;

                    debugText.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                }

                if (handType == SteamVR_Input_Sources.RightHand)
                {
                    debugText.transform.localPosition = new Vector3(-0.05f, 0.0f, 0.0f);
                    debugText.alignment = TextAlignment.Right;
                    debugText.anchor = TextAnchor.UpperRight;
                }
                else
                {
                    debugText.transform.localPosition = new Vector3(0.05f, 0.0f, 0.0f);
                    debugText.alignment = TextAlignment.Left;
                    debugText.anchor = TextAnchor.UpperLeft;
                }

                debugText.text = string.Format(
                    "Hovering: {0}\n" +
                    "Hover Lock: {1}\n" +
                    "Attached: {2}\n" +
                    "Total Attached: {3}\n" +
                    "Type: {4}\n",
                    (hoveringInteractable ? hoveringInteractable.gameObject.name : "null"),
                    hoverLocked,
                    (currentAttachedObject ? currentAttachedObject.name : "null"),
                    attachedObjects.Count,
                    handType.ToString()
                );
            }
        }


        protected virtual void OnEnable()
        {
            inputFocusAction.enabled = true;

            // Stagger updates between hands
            float hoverUpdateBegin = ((otherHand != null) && (otherHand.GetInstanceID() < GetInstanceID())) ? (0.5f * hoverUpdateInterval) : (0.0f);
            InvokeRepeating("UpdateHovering", hoverUpdateBegin, hoverUpdateInterval);
        }


        protected virtual void OnDisable()
        {
            inputFocusAction.enabled = false;

            CancelInvoke();
        }



        protected virtual void Update()
        {
            AttachmentsUpdate();


            if (hoveringInteractable)
                hoveringInteractable.SendMessage("HandHoverUpdate", this, SendMessageOptions.DontRequireReceiver);
            
            UpdateDebugText();
        }

        protected virtual void FixedUpdate()
        {
            AttachmentsFixedUpdate();
        }

       


        protected virtual void OnInputFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                DetachObject(applicationLostFocusObject, true);
                applicationLostFocusObject.SetActive(false);
                UpdateHovering();
                BroadcastMessage("OnParentHandInputFocusAcquired", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                applicationLostFocusObject.SetActive(true);
                AttachGameObject(applicationLostFocusObject, GrabTypes.Scripted, AttachmentFlags.ParentToHand);
                BroadcastMessage("OnParentHandInputFocusLost", SendMessageOptions.DontRequireReceiver);
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (useHoverSphere)
            {
                Gizmos.color = Color.green;
                float scaledHoverRadius = hoverSphereRadius * Mathf.Abs(SteamVR_Utils.GetLossyScale(hoverSphereTransform));
                Gizmos.DrawWireSphere(hoverSphereTransform.position, scaledHoverRadius/2);
            }

            if ( mainRenderModel != null && mainRenderModel.IsControllerVisibile())
            {
                Gizmos.color = Color.blue;
                float scaledHoverRadius = controllerHoverRadius * Mathf.Abs(SteamVR_Utils.GetLossyScale(this.transform));
                Gizmos.DrawWireSphere(mainRenderModel.GetControllerPosition(controllerHoverComponent), scaledHoverRadius/2 + .01f);
            }

            if (useFingerJointHover && mainRenderModel != null && mainRenderModel.IsHandVisibile())
            {
                Gizmos.color = Color.yellow;
                float scaledHoverRadius = fingerJointHoverRadius * Mathf.Abs(SteamVR_Utils.GetLossyScale(this.transform));
                Gizmos.DrawWireSphere(mainRenderModel.GetBonePosition((int)fingerJointHover), scaledHoverRadius/2 + .02f);
            }
        }

        void HandDebugLog(string msg) {
            if (spewDebugText) Debug.Log("Hand (" + this.name + "): " + msg);
        }

        // Continue to hover over this object indefinitely, whether or not the Hand moves out of its interaction trigger volume.
        // interactable - The Interactable to hover over indefinitely.
        public void HoverLock(Interactable interactable)
        {
            HandDebugLog("HoverLock " + interactable);
            hoverLocked = true;
            hoveringInteractable = interactable;
        }


        // Stop hovering over this object indefinitely.
        // interactable - The hover-locked Interactable to stop hovering over indefinitely.
        public void HoverUnlock(Interactable interactable)
        {
            HandDebugLog("HoverUnlock " + interactable);

            if (hoveringInteractable == interactable)
            {
                hoverLocked = false;
            }
        }



        /*
        Trigger the haptics at a certain time for a certain length

        secondsFromNow: How long from the current time to execute the action (in seconds - can be 0)
        durationSeconds: How long the haptic action should last (in seconds)
        frequency: How often the haptic motor should bounce (0 - 320 in hz. The lower end being more useful)
        amplitude: How intense the haptic action should be (0 - 1)
        inputSource: The device you would like to execute the haptic action. Any if the action is not device specific.

        void SteamVR_Action_Vibration.Execute(
            float secondsFromNow, 
            float durationSeconds, 
            float frequency, 
            float amplitude, 
            SteamVR_Input_Sources inputSource
        )
        */
        public void TriggerHapticPulse(ushort microSecondsDuration)
        {
            float seconds = (float)microSecondsDuration / 1000000f;
            hapticAction.Execute(0, seconds, 1f / seconds, 1, handType);
        }
        public void TriggerHapticPulse(float duration, float frequency, float amplitude)
        {
            hapticAction.Execute(0, duration, frequency, amplitude, handType);
        }

        public void ShowGrabHint()
        {
            ControllerButtonHints.ShowButtonHint(this, grabGripAction); //todo: assess
        }
        public void ShowGrabHint(string text)
        {
            ControllerButtonHints.ShowTextHint(this, grabGripAction, text);
        }
        public void HideGrabHint()
        {
            ControllerButtonHints.HideButtonHint(this, grabGripAction); //todo: assess
        }

        void InitController()
        {
            HandDebugLog("Connected with type " + handType.ToString());

            bool hadOldRendermodel = mainRenderModel != null;
            EVRSkeletalMotionRange oldRM_rom = EVRSkeletalMotionRange.WithController;
            if(hadOldRendermodel)
                oldRM_rom = mainRenderModel.GetSkeletonRangeOfMotion;


            foreach (RenderModel r in renderModels)
            {
                if (r != null)
                    Destroy(r.gameObject);
            }

            renderModels.Clear();

            GameObject renderModelInstance = GameObject.Instantiate(renderModelPrefab);
            renderModelInstance.layer = gameObject.layer;
            renderModelInstance.tag = gameObject.tag;
            renderModelInstance.transform.parent = this.transform;
            renderModelInstance.transform.localPosition = Vector3.zero;
            renderModelInstance.transform.localRotation = Quaternion.identity;
            renderModelInstance.transform.localScale = renderModelPrefab.transform.localScale;

            TriggerHapticPulse(800);  //pulse on controller init

            int deviceIndex = trackedObject.GetDeviceIndex();

            mainRenderModel = renderModelInstance.GetComponent<RenderModel>();
            renderModels.Add(mainRenderModel);

            if (hadOldRendermodel)
                mainRenderModel.SetSkeletonRangeOfMotion(oldRM_rom);

            this.BroadcastMessage("SetInputSource", handType, SendMessageOptions.DontRequireReceiver); // let child objects know we've initialized
            this.BroadcastMessage("OnHandInitialized", deviceIndex, SendMessageOptions.DontRequireReceiver); // let child objects know we've initialized
        }
        public void SetRenderModel(GameObject prefab){
            renderModelPrefab = prefab;
            if (mainRenderModel != null && isPoseValid)
                InitController();
        }
        public void SetHoverRenderModel(RenderModel hoverRenderModel){
            hoverhighlightRenderModel = hoverRenderModel;
            renderModels.Add(hoverRenderModel);
        }
 
    }


    [System.Serializable]
    public class HandEvent : UnityEvent<Hand> { }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(Hand))]
    public class HandEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            Hand hand = (Hand)target;
            if (hand.otherHand){
                if (hand.otherHand.otherHand != hand)
                    UnityEditor.EditorGUILayout.HelpBox("The otherHand of this Hand's otherHand is not this Hand.", UnityEditor.MessageType.Warning);
                if (hand.handType == SteamVR_Input_Sources.LeftHand && hand.otherHand.handType != SteamVR_Input_Sources.RightHand)
                    UnityEditor.EditorGUILayout.HelpBox("This is a left Hand but otherHand is not a right Hand.", UnityEditor.MessageType.Warning);
                if (hand.handType == SteamVR_Input_Sources.RightHand && hand.otherHand.handType != SteamVR_Input_Sources.LeftHand)
                    UnityEditor.EditorGUILayout.HelpBox("This is a right Hand but otherHand is not a left Hand.", UnityEditor.MessageType.Warning);
                if (hand.handType == SteamVR_Input_Sources.Any && hand.otherHand.handType != SteamVR_Input_Sources.Any)
                    UnityEditor.EditorGUILayout.HelpBox("This is an any-handed Hand but otherHand is not an any-handed Hand.", UnityEditor.MessageType.Warning);
            }
        }
    }
#endif
}
