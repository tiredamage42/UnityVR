using UnityEngine;
using System.Collections;

namespace Valve.VR.InteractionSystem
{
    public class Grenade : Grabbable
    {
        public GameObject explodePartPrefab;
        public int explodeCount = 10;

        public float minMagnitudeToExplode = 1f;

        
        
        private void OnCollisionEnter(Collision collision)
        {
            if (attachedToHand != null) //don't explode in hand
                return;

            if (collision.impulse.magnitude > minMagnitudeToExplode)
            {
                for (int explodeIndex = 0; explodeIndex < explodeCount; explodeIndex++)
                {
                    GameObject explodePart = (GameObject)GameObject.Instantiate(explodePartPrefab, this.transform.position, this.transform.rotation);
                    explodePart.GetComponentInChildren<MeshRenderer>().material.SetColor("_TintColor", Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
                }

                Destroy(this.gameObject);
            }
        }
    }
}