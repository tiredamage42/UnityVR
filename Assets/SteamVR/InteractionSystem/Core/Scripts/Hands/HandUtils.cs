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

        // Get the world velocity of the VR Hand.
        public Vector3 GetTrackedObjectVelocity(float timeOffset = 0)
        {
            if (isActive)
            {
                if (timeOffset == 0)
                    return Player.instance.transform.TransformVector(trackedObject.GetVelocity());
                else
                {
                    Vector3 velocity;
                    Vector3 angularVelocity;

                    bool success = trackedObject.GetVelocitiesAtTimeOffset(timeOffset, out velocity, out angularVelocity);
                    if (success)
                        return Player.instance.transform.TransformVector(velocity);
                }
            }

            return Vector3.zero;
        }


        // Get the world space angular velocity of the VR Hand.
        public Vector3 GetTrackedObjectAngularVelocity(float timeOffset = 0)
        {
            if (isActive)
            {
                if (timeOffset == 0)
                    return Player.instance.transform.TransformDirection(trackedObject.GetAngularVelocity());
                else
                {
                    Vector3 velocity;
                    Vector3 angularVelocity;

                    bool success = trackedObject.GetVelocitiesAtTimeOffset(timeOffset, out velocity, out angularVelocity);
                    if (success)
                        return Player.instance.transform.TransformDirection(angularVelocity);
                }
            }

            return Vector3.zero;
        }

        



        public void GetEstimatedPeakVelocities(out Vector3 velocity, out Vector3 angularVelocity)
        {
            trackedObject.GetEstimatedPeakVelocities(out velocity, out angularVelocity);
            velocity = Player.instance.transform.TransformVector(velocity);
            angularVelocity = Player.instance.transform.TransformDirection(angularVelocity);
        }


        /*
        public GrabTypes GetGrabStarting(GrabTypes explicitType = GrabTypes.None)
        {
            if (explicitType != GrabTypes.None)
            {
                if (explicitType == GrabTypes.Pinch && grabPinchAction.GetStateDown(handType))
                    return GrabTypes.Pinch;
                if (explicitType == GrabTypes.Grip && grabGripAction.GetStateDown(handType))
                    return GrabTypes.Grip;
            }
            else
            {
                if (grabPinchAction.GetStateDown(handType))
                    return GrabTypes.Pinch;
                if (grabGripAction.GetStateDown(handType))
                    return GrabTypes.Grip;
            }

            return GrabTypes.None;
        }

        public GrabTypes GetGrabEnding(GrabTypes explicitType = GrabTypes.None)
        {
            if (explicitType != GrabTypes.None)
            {
                if (explicitType == GrabTypes.Pinch && grabPinchAction.GetStateUp(handType))
                    return GrabTypes.Pinch;
                if (explicitType == GrabTypes.Grip && grabGripAction.GetStateUp(handType))
                    return GrabTypes.Grip;
            }
            else
            {
                if (grabPinchAction.GetStateUp(handType))
                    return GrabTypes.Pinch;
                if (grabGripAction.GetStateUp(handType))
                    return GrabTypes.Grip;
            }

            return GrabTypes.None;
        }

        public bool IsGrabEnding(GameObject attachedObject)
        {
            for (int attachedObjectIndex = 0; attachedObjectIndex < attachedObjects.Count; attachedObjectIndex++)
            {
                if (attachedObjects[attachedObjectIndex].attachedObject == attachedObject)
                {
                    return IsGrabbingWithType(attachedObjects[attachedObjectIndex].grabbedWithType) == false;
                }
            }

            return false;
        }

        public bool IsGrabbingWithType(GrabTypes type)
        {
            switch (type)
            {
                case GrabTypes.Pinch:
                    return grabPinchAction.GetState(handType);

                case GrabTypes.Grip:
                    return grabGripAction.GetState(handType);

                default:
                    return false;
            }
        }
        public bool IsGrabbingWithOppositeType(GrabTypes type){
            switch (type){
                case GrabTypes.Pinch:
                    return grabGripAction.GetState(handType);
                case GrabTypes.Grip:
                    return grabPinchAction.GetState(handType);
                default:
                    return false;
            }
        }

  

        public GrabTypes GetBestGrabbingType()
        {
            return GetBestGrabbingType(GrabTypes.None);
        }

        public GrabTypes GetBestGrabbingType(GrabTypes preferred, bool forcePreference = false)
        {
            if (preferred == GrabTypes.Pinch)
            {
                if (grabPinchAction.GetState(handType))
                    return GrabTypes.Pinch;
                else if (forcePreference)
                    return GrabTypes.None;
            }
            if (preferred == GrabTypes.Grip)
            {
                if (grabGripAction.GetState(handType))
                    return GrabTypes.Grip;
                else if (forcePreference)
                    return GrabTypes.None;
            }

            if (grabPinchAction.GetState(handType))
                return GrabTypes.Pinch;
            if (grabGripAction.GetState(handType))
                return GrabTypes.Grip;

            return GrabTypes.None;
        }
        */
        

        public int GetDeviceIndex(){
            return trackedObject.GetDeviceIndex();
        }
 
    }
}
