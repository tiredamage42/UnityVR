// Purpose: The hands used by the player in the vr interaction system

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;
using System.Threading;

namespace Valve.VR.InteractionSystem
{
    public partial class Hand : MonoBehaviour
    {

        Transform hand_attachment_offset_helper;

        void InitializeHandHelper () {
            hand_attachment_offset_helper = new GameObject("HandHelper").transform;
            hand_attachment_offset_helper.SetParent(transform);
        }
        void SetOffset (Vector3 pos_offset, Vector3 rot_offset) {
            hand_attachment_offset_helper.localPosition = pos_offset;
            hand_attachment_offset_helper.localRotation = Quaternion.Euler(rot_offset);
        }



        // The flags used to determine how an object is attached to the hand.
        [Flags]
        public enum AttachmentFlags
        {
            DetachOthers = 1 << 1, // Other objects attached to this hand will be detached.
            DetachFromOtherHand = 1 << 2, // This object will be detached from the other hand.
            ParentToHand = 1 << 3, // The object will be parented to the hand.
            VelocityMovement = 1 << 4, // The object will attempt to move to match the position and rotation of the hand.
            TurnOnKinematic = 1 << 5, // The object will not respond to external physics.
            TurnOffGravity = 1 << 6, // The object will not respond to external physics.
        };

        public const AttachmentFlags defaultAttachmentFlags = AttachmentFlags.ParentToHand |
                                                              AttachmentFlags.DetachOthers |
                                                              AttachmentFlags.DetachFromOtherHand |
                                                                AttachmentFlags.TurnOnKinematic;

        [System.Serializable] public struct AttachedObject
        {
            public GameObject attachedObject;
            public Grabbable grabbable;
            public Rigidbody attachedRigidbody;
            public CollisionDetectionMode collisionDetectionMode;
            public bool attachedRigidbodyWasKinematic;
            public bool attachedRigidbodyUsedGravity;
            public GameObject originalParent;
            public bool isParentedToHand;
            public AttachmentFlags attachmentFlags;

            public bool HasAttachFlag(AttachmentFlags flag)
            {
                return (attachmentFlags & flag) == flag;
            }
        }
        public List<AttachedObject> attachedObjects = new List<AttachedObject>();


       // Active GameObject attached to this Hand
        public GameObject currentAttachedObject
        {
            get
            {
                CleanUpAttachedObjectStack();
                if (attachedObjects.Count > 0)
                    return attachedObjects[attachedObjects.Count - 1].attachedObject;
                return null;
            }
        }

        public AttachedObject? currentAttachedObjectInfo
        {
            get
            {
                CleanUpAttachedObjectStack();
                if (attachedObjects.Count > 0)
                    return attachedObjects[attachedObjects.Count - 1];
                return null;
            }
        }

        public bool ObjectIsAttached(GameObject go)
        {
            for (int attachedIndex = 0; attachedIndex < attachedObjects.Count; attachedIndex++){
                if (attachedObjects[attachedIndex].attachedObject == go)
                    return true;
            }
            return false;
        }



        void AttachObj (GameObject obj_attached, Grabbable grabbable, Vector3 pos_offset, Vector3 rot_offset, AttachmentFlags flags)
        
        {
        
            if (flags == 0)
                flags = defaultAttachmentFlags;
            AttachedObject attachedObject = new AttachedObject();
            attachedObject.attachmentFlags = flags;
            //Make sure top object on stack is non-null
            CleanUpAttachedObjectStack();

            //Detach the object if it is already attached so that it can get re-attached at the top of the stack
            if(ObjectIsAttached(obj_attached))
                DetachObject(obj_attached);

            //Detach from the other hand if requested
            if (attachedObject.HasAttachFlag(AttachmentFlags.DetachFromOtherHand))
                otherHand.DetachObject(obj_attached);
            
            if (attachedObject.HasAttachFlag(AttachmentFlags.DetachOthers))
            {
                //Detach all the objects from the stack
                while (attachedObjects.Count > 0) {
                    DetachObject(attachedObjects[0].attachedObject);
                }
            }

            if (currentAttachedObject)
                currentAttachedObject.SendMessage("OnHandFocusLost", this, SendMessageOptions.DontRequireReceiver);
            
            attachedObject.attachedObject = obj_attached;
            
            SetInteractaleAttachedObject (ref attachedObject, grabbable);
            attachedObject.originalParent = obj_attached.transform.parent != null ? obj_attached.transform.parent.gameObject : null;
            MaybeParentToHand (ref attachedObject, obj_attached);
            MaybeSnap (ref attachedObject, obj_attached.transform, pos_offset, rot_offset);
            
            SetPhysicsAttachedObject (ref attachedObject, obj_attached);
            attachedObjects.Add(attachedObject);
            UpdateHovering();
            HandDebugLog("AttachObject " + obj_attached.name);
            obj_attached.SendMessage("OnAttachedToHand", this, SendMessageOptions.DontRequireReceiver);
        
        }

        // Attach a GameObject to this GameObject
        // objectToAttach - The GameObject to attach
        // attachmentPoint - Name of the GameObject in the hierarchy of this Hand which should act as the attachment point for this GameObject
        public void AttachGrabbable(Grabbable interactable_to_attach)//, GrabTypes grabbedWithType, Transform attachmentOffset = null)
        {

            AttachmentFlags flags = interactable_to_attach.parameters.attachmentFlags;
            AttachObj (interactable_to_attach.gameObject, interactable_to_attach, interactable_to_attach.parameters.attach_position_offset, interactable_to_attach.parameters.attach_rotation_offset, flags);
        
        }
        // Attach a GameObject to this GameObject
        // objectToAttach - The GameObject to attach
        // flags - The flags to use for attaching the object
        // attachmentPoint - Name of the GameObject in the hierarchy of this Hand which should act as the attachment point for this GameObject
        public void AttachGameObject(GameObject objectToAttach, AttachmentFlags flags = defaultAttachmentFlags)
        {
            AttachObj (objectToAttach, null, Vector3.zero, Vector3.zero, flags);
        }
        void SetInteractaleAttachedObject (ref AttachedObject ao, Grabbable grabbable) {
            ao.grabbable = grabbable;
            
            if (grabbable == null)
                return;

            if (grabbable.parameters.hideHandOnAttach)
                SetVisibility(false);
            if (grabbable.parameters.hideSkeletonOnAttach && mainRenderModel != null && mainRenderModel.displayHandByDefault)
                SetSkeletonVisibility (false);
            if (grabbable.parameters.hideControllerOnAttach && mainRenderModel != null && mainRenderModel.displayControllerByDefault)
                SetControllerVisibility(false); 
            if (grabbable.parameters.handAnimationOnPickup1 != RenderModel.AnimationState.Rest)
                SetAnimationState(grabbable.parameters.handAnimationOnPickup1);
            if (grabbable.parameters.setRangeOfMotionOnPickup != SkeletalMotionRangeChange.None)
                SetTemporarySkeletonRangeOfMotion(grabbable.parameters.setRangeOfMotionOnPickup);
            
        }

        void SetPhysicsAttachedObject (ref AttachedObject ao, GameObject obj_attached) {
            ao.attachedRigidbody = obj_attached.GetComponent<Rigidbody>();
            if (ao.attachedRigidbody == null)
                return;
            
            
            Grabbable grabbable = ao.grabbable;
            if (grabbable != null && grabbable.attachedToHand != null) //already attached to another hand
            {
                //if it was attached to another hand, get the flags from that hand
                
                for (int i = 0; i < grabbable.attachedToHand.attachedObjects.Count; i++)
                {
                    AttachedObject attachedObjectInList = grabbable.attachedToHand.attachedObjects[i];
                    if (attachedObjectInList.grabbable == grabbable)
                    {
                        ao.attachedRigidbodyWasKinematic = attachedObjectInList.attachedRigidbodyWasKinematic;
                        ao.attachedRigidbodyUsedGravity = attachedObjectInList.attachedRigidbodyUsedGravity;
                        ao.originalParent = attachedObjectInList.originalParent;
                    }
                }
            }
            else
            {
                ao.attachedRigidbodyWasKinematic = ao.attachedRigidbody.isKinematic;
                ao.attachedRigidbodyUsedGravity = ao.attachedRigidbody.useGravity;
            }
          
            if (ao.HasAttachFlag(AttachmentFlags.TurnOnKinematic))
            {
                ao.collisionDetectionMode = ao.attachedRigidbody.collisionDetectionMode;
                if (ao.collisionDetectionMode == CollisionDetectionMode.Continuous)
                    ao.attachedRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                ao.attachedRigidbody.isKinematic = true;
            }
            if (ao.HasAttachFlag(AttachmentFlags.TurnOffGravity))
                ao.attachedRigidbody.useGravity = false;
        }
        void MaybeParentToHand (ref AttachedObject ao, GameObject obj_attached){
            ao.isParentedToHand = ao.HasAttachFlag(AttachmentFlags.ParentToHand);
            //Parent the object to the hand
            if (ao.isParentedToHand)
                obj_attached.transform.parent = this.transform;
        }

        void MaybeSnap (ref AttachedObject ao, Transform t_attached, Vector3 pos_offset, Vector3 rot_offset) {
            SetOffset(pos_offset, rot_offset);
            t_attached.rotation = transform.rotation;
            t_attached.position = transform.position;
            
        }

      



        // Detach this GameObject from the attached object stack of this Hand
        // objectToDetach - The GameObject to detach from this Hand
        public void DetachObject(GameObject objectToDetach, bool restoreOriginalParent = true)
        {
            int index = attachedObjects.FindIndex(l => l.attachedObject == objectToDetach);
            if (index != -1)
            {
                HandDebugLog("DetachObject " + objectToDetach);

                GameObject prevTopObject = currentAttachedObject;
                AttachedObject a_obj = attachedObjects[index];


                Grabbable grabbable = a_obj.grabbable;
                if (grabbable != null)
                {
                    if (grabbable.parameters.hideHandOnAttach)
                        SetVisibility(true);
                    if (grabbable.parameters.hideSkeletonOnAttach && mainRenderModel != null && mainRenderModel.displayHandByDefault)
                        SetSkeletonVisibility (true);
                    if (grabbable.parameters.hideControllerOnAttach && mainRenderModel != null && mainRenderModel.displayControllerByDefault)
                        SetControllerVisibility(true); 
                    if (grabbable.parameters.handAnimationOnPickup1 != RenderModel.AnimationState.Rest)
                        StopAnimation();
                    if (grabbable.parameters.setRangeOfMotionOnPickup != SkeletalMotionRangeChange.None)
                        ResetTemporarySkeletonRangeOfMotion();
                }

                Transform parentTransform = null;
                if (a_obj.isParentedToHand)
                {
                    if (restoreOriginalParent && (a_obj.originalParent != null))
                    {
                        parentTransform = a_obj.originalParent.transform;
                    }
                    a_obj.attachedObject.transform.parent = parentTransform;
                }

                if (a_obj.HasAttachFlag(AttachmentFlags.TurnOnKinematic))
                {
                    if (a_obj.attachedRigidbody != null)
                    {
                        a_obj.attachedRigidbody.isKinematic = a_obj.attachedRigidbodyWasKinematic;
                        a_obj.attachedRigidbody.collisionDetectionMode = a_obj.collisionDetectionMode;
                    }
                }

                if (a_obj.HasAttachFlag(AttachmentFlags.TurnOffGravity))
                {
                    if (a_obj.attachedRigidbody != null)
                        a_obj.attachedRigidbody.useGravity = a_obj.attachedRigidbodyUsedGravity;
                }

                if (grabbable == null || (grabbable != null && grabbable.isDestroying == false))
                {
                    a_obj.attachedObject.SetActive(true);
                    a_obj.attachedObject.SendMessage("OnDetachedFromHand", this, SendMessageOptions.DontRequireReceiver);
                    attachedObjects.RemoveAt(index);
                }
                else
                    attachedObjects.RemoveAt(index);

                CleanUpAttachedObjectStack();

                GameObject newTopObject = currentAttachedObject;

                hoverLocked = false;


                //Give focus to the top most object on the stack if it changed
                if (newTopObject != null && newTopObject != prevTopObject)
                {
                    newTopObject.SetActive(true);
                    newTopObject.SendMessage("OnHandFocusAcquired", this, SendMessageOptions.DontRequireReceiver);
                }
            }

            CleanUpAttachedObjectStack();

            if (mainRenderModel != null)
                mainRenderModel.MatchHandToTransform(mainRenderModel.transform);
            if (hoverhighlightRenderModel != null)
                hoverhighlightRenderModel.MatchHandToTransform(hoverhighlightRenderModel.transform);
        }




        //-------------------------------------------------
        private void CleanUpAttachedObjectStack()
        {
            attachedObjects.RemoveAll(l => l.attachedObject == null);
        }


        void AttachmentsFixedUpdate ()
        {
            if (currentAttachedObject != null)
            {
                AttachedObject attachedInfo = currentAttachedObjectInfo.Value;
                if (attachedInfo.attachedObject != null)
                {
                    if (attachedInfo.HasAttachFlag(AttachmentFlags.VelocityMovement))
                    {
                        UpdateAttachedVelocity(attachedInfo);
                    }
                }
            }
        }
        
void AttachmentsUpdate()
{
    GameObject attachedObject = currentAttachedObject;
    if (attachedObject != null)
        attachedObject.SendMessage("HandAttachedUpdate", this, SendMessageOptions.DontRequireReceiver);

}


        protected const float MaxVelocityChange = 10f;
        protected const float VelocityMagic = 6000f;
        protected const float AngularVelocityMagic = 50f;
        protected const float MaxAngularVelocityChange = 20f;

        protected void UpdateAttachedVelocity(AttachedObject attachedObjectInfo)
        {
            Transform attach_trans = transform;
            
            float scale = SteamVR_Utils.GetLossyScale(attach_trans);

            float maxVelocityChange = MaxVelocityChange * scale;
            float velocityMagic = VelocityMagic;
            float angularVelocityMagic = AngularVelocityMagic;
            float maxAngularVelocityChange = MaxAngularVelocityChange * scale;

            Vector3 targetItemPosition = attach_trans.TransformPoint(hand_attachment_offset_helper.localPosition);
            
            Vector3 positionDelta = (targetItemPosition - attachedObjectInfo.attachedRigidbody.position);
            Vector3 velocityTarget = (positionDelta * velocityMagic * Time.deltaTime);

            if (float.IsNaN(velocityTarget.x) == false && float.IsInfinity(velocityTarget.x) == false)
            {
                attachedObjectInfo.attachedRigidbody.velocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.velocity, velocityTarget, maxVelocityChange);
            }


            Quaternion targetItemRotation = attach_trans.rotation * hand_attachment_offset_helper.localRotation;
            
            Quaternion rotationDelta = targetItemRotation * Quaternion.Inverse(attachedObjectInfo.attachedObject.transform.rotation);


            float angle;
            Vector3 axis;
            rotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0 && float.IsNaN(axis.x) == false && float.IsInfinity(axis.x) == false)
            {
                Vector3 angularTarget = angle * axis * angularVelocityMagic * Time.deltaTime;

                attachedObjectInfo.attachedRigidbody.angularVelocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.angularVelocity, angularTarget, maxAngularVelocityChange);
            }
        }

    }
}
