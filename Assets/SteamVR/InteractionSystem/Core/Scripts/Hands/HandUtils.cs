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


        

        public int GetDeviceIndex(){
            return trackedObject.GetDeviceIndex();
        }
 
    }
}
