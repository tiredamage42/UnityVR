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
            AllowSidegrade = 1 << 7, // The object is able to switch from a pinch grab to a grip grab. Decreases likelyhood of a good throw but also decreases likelyhood of accidental drop
        };

        public const AttachmentFlags defaultAttachmentFlags = AttachmentFlags.ParentToHand |
                                                              AttachmentFlags.DetachOthers |
                                                              AttachmentFlags.DetachFromOtherHand |
                                                                AttachmentFlags.TurnOnKinematic;

        [System.Serializable] public struct AttachedObject
        {
            public GameObject attachedObject;
            public Interactable interactable;
            public Rigidbody attachedRigidbody;
            public CollisionDetectionMode collisionDetectionMode;
            public bool attachedRigidbodyWasKinematic;
            public bool attachedRigidbodyUsedGravity;
            public GameObject originalParent;
            public bool isParentedToHand;
            //public GrabTypes grabbedWithType;
            public AttachmentFlags attachmentFlags;
            //public Vector3 initialPositionalOffset;
            //public Quaternion initialRotationalOffset;
            //public Transform attachedOffsetTransform;
            //public Transform handAttachmentPointTransform;

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



        //void AttachObj (GameObject obj_attached, Interactable interactable_ref, Vector3 pos_offset, Vector3 rot_offset, GrabTypes grabbedWithType, AttachmentFlags flags, Transform attachmentOffset = null)
        void AttachObj (GameObject obj_attached, Interactable interactable_ref, Vector3 pos_offset, Vector3 rot_offset, AttachmentFlags flags)
        
        {
        
            if (flags == 0)
                flags = defaultAttachmentFlags;
            AttachedObject attachedObject = new AttachedObject();
            attachedObject.attachmentFlags = flags;
            //attachedObject.attachedOffsetTransform = attachmentOffset;
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
            //attachedObject.handAttachmentPointTransform = this.transform;
            
            SetInteractaleAttachedObject (ref attachedObject, interactable_ref);
            attachedObject.originalParent = obj_attached.transform.parent != null ? obj_attached.transform.parent.gameObject : null;
            //attachedObject.grabbedWithType = grabbedWithType;
            MaybeParentToHand (ref attachedObject, obj_attached);
            //MaybeSnap (ref attachedObject, obj_attached.transform, attachmentOffset, pos_offset, rot_offset);
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
        public void AttachInteractable(Interactable interactable_to_attach)//, GrabTypes grabbedWithType, Transform attachmentOffset = null)
        {

            AttachmentFlags flags = interactable_to_attach.attachmentFlags;
            //AttachObj (interactable_to_attach.gameObject, interactable_to_attach, interactable_to_attach.attach_position_offset, interactable_to_attach.attach_rotation_offset, grabbedWithType, flags, attachmentOffset);
            AttachObj (interactable_to_attach.gameObject, interactable_to_attach, interactable_to_attach.attach_position_offset, interactable_to_attach.attach_rotation_offset, flags);
        
        }
        // Attach a GameObject to this GameObject
        // objectToAttach - The GameObject to attach
        // flags - The flags to use for attaching the object
        // attachmentPoint - Name of the GameObject in the hierarchy of this Hand which should act as the attachment point for this GameObject
        public void AttachGameObject(GameObject objectToAttach, AttachmentFlags flags = defaultAttachmentFlags)
        //public void AttachGameObject(GameObject objectToAttach, GrabTypes grabbedWithType, AttachmentFlags flags = defaultAttachmentFlags, Transform attachmentOffset = null)
        
        {
            AttachObj (objectToAttach, null, Vector3.zero, Vector3.zero, flags);
        
        //    AttachObj (objectToAttach, null, Vector3.zero, Vector3.zero, grabbedWithType, flags, attachmentOffset);
        }
        void SetInteractaleAttachedObject (ref AttachedObject ao, Interactable interactable) {
            ao.interactable = interactable;
            
            if (interactable == null)
                return;

            //if (interactable.useHandObjectAttachmentPoint)
            //    ao.handAttachmentPointTransform = objectAttachmentPoint;
            if (interactable.hideHandOnAttach)
                SetVisibility(false);
            if (interactable.hideSkeletonOnAttach && mainRenderModel != null && mainRenderModel.displayHandByDefault)
                SetSkeletonVisibility (false);
            if (interactable.hideControllerOnAttach && mainRenderModel != null && mainRenderModel.displayControllerByDefault)
                SetControllerVisibility(false); 
            if (interactable.handAnimationOnPickup1 != RenderModel.AnimationState.Rest)
                SetAnimationState(interactable.handAnimationOnPickup1);
            if (interactable.setRangeOfMotionOnPickup != SkeletalMotionRangeChange.None)
                SetTemporarySkeletonRangeOfMotion(interactable.setRangeOfMotionOnPickup);
            
        }

        void SetPhysicsAttachedObject (ref AttachedObject ao, GameObject obj_attached) {
            ao.attachedRigidbody = obj_attached.GetComponent<Rigidbody>();
            if (ao.attachedRigidbody == null)
                return;
            
            
            Interactable interactable = ao.interactable;
            if (interactable != null && interactable.attachedToHand != null) //already attached to another hand
            {
                //if it was attached to another hand, get the flags from that hand
                
                for (int i = 0; i < interactable.attachedToHand.attachedObjects.Count; i++)
                {
                    AttachedObject attachedObjectInList = interactable.attachedToHand.attachedObjects[i];
                    if (attachedObjectInList.interactable == interactable)
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

        //void MaybeSnap (ref AttachedObject ao, Transform t_attached, Transform attachmentOffset, Vector3 pos_offset, Vector3 rot_offset) {
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


                Interactable interactable = a_obj.interactable;
                if (interactable != null)
                {
                    if (interactable.hideHandOnAttach)
                        SetVisibility(true);
                    if (interactable.hideSkeletonOnAttach && mainRenderModel != null && mainRenderModel.displayHandByDefault)
                        SetSkeletonVisibility (true);
                    if (interactable.hideControllerOnAttach && mainRenderModel != null && mainRenderModel.displayControllerByDefault)
                        SetControllerVisibility(true); 
                    if (interactable.handAnimationOnPickup1 != RenderModel.AnimationState.Rest)
                        StopAnimation();
                    if (interactable.setRangeOfMotionOnPickup != SkeletalMotionRangeChange.None)
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

                if (interactable == null || (interactable != null && interactable.isDestroying == false))
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

/*
protected virtual void AttachmentsOnTransformUpdate()
        {

            GameObject attachedObject = currentAttachedObject;
            if (attachedObject != null)
            {
                Transform attach_trans = transform;// attachedObject.Value.handAttachmentPointTransform;
                if (currentAttachedObjectInfo.Value.interactable != null && currentAttachedObjectInfo.Value.interactable.handFollowTransform != null)
                {
                    if (currentAttachedObjectInfo.Value.interactable.handFollowTransformRotation)
                    {

                        Debug.LogError("Setting Rotation OnTransform Update");

                        Quaternion offset = Quaternion.Inverse(this.transform.rotation) * attach_trans.rotation;
                        Quaternion targetHandRotation = currentAttachedObjectInfo.Value.interactable.handFollowTransform.rotation * Quaternion.Inverse(offset);


                        if (mainRenderModel != null)
                            mainRenderModel.SetHandRotation(targetHandRotation);
                        if (hoverhighlightRenderModel != null)
                            hoverhighlightRenderModel.SetHandRotation(targetHandRotation);
                    }

                    if (currentAttachedObjectInfo.Value.interactable.handFollowTransformPosition)
                    {
                        Debug.LogError("Setting Position OnTransform Update");

                        Vector3 worldOffset = (this.transform.position - attach_trans.position);

                        Quaternion rotationDiff = mainRenderModel.GetHandRotation() * Quaternion.Inverse(this.transform.rotation);

                        Vector3 localOffset = rotationDiff * worldOffset;
                        Vector3 targetHandPosition = currentAttachedObjectInfo.Value.interactable.handFollowTransform.position + localOffset;

                        if (mainRenderModel != null)
                            mainRenderModel.SetHandPosition(targetHandPosition);
                        if (hoverhighlightRenderModel != null)
                            hoverhighlightRenderModel.SetHandPosition(targetHandPosition);
                    }
                }
            }
        }
        */


        protected const float MaxVelocityChange = 10f;
        protected const float VelocityMagic = 6000f;
        protected const float AngularVelocityMagic = 50f;
        protected const float MaxAngularVelocityChange = 20f;

        protected void UpdateAttachedVelocity(AttachedObject attachedObjectInfo)
        {
            Transform attach_trans = transform;// currentAttachedObjectInfo.Value.handAttachmentPointTransform;

            float scale = SteamVR_Utils.GetLossyScale(attach_trans);

            float maxVelocityChange = MaxVelocityChange * scale;
            float velocityMagic = VelocityMagic;
            float angularVelocityMagic = AngularVelocityMagic;
            float maxAngularVelocityChange = MaxAngularVelocityChange * scale;

            //Vector3 targetItemPosition = attach_trans.TransformPoint(attachedObjectInfo.initialPositionalOffset);
            Vector3 targetItemPosition = attach_trans.TransformPoint(hand_attachment_offset_helper.localPosition);
            
            Vector3 positionDelta = (targetItemPosition - attachedObjectInfo.attachedRigidbody.position);
            Vector3 velocityTarget = (positionDelta * velocityMagic * Time.deltaTime);

            if (float.IsNaN(velocityTarget.x) == false && float.IsInfinity(velocityTarget.x) == false)
            {
                attachedObjectInfo.attachedRigidbody.velocity = Vector3.MoveTowards(attachedObjectInfo.attachedRigidbody.velocity, velocityTarget, maxVelocityChange);
            }


            //Quaternion targetItemRotation = attach_trans.rotation * attachedObjectInfo.initialRotationalOffset;
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
