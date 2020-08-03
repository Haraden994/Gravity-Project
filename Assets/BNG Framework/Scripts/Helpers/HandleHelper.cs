using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// This component is used to pull grab items toward it, and then reset it's position when not being grabbed
    /// </summary>
    public class HandleHelper : MonoBehaviour {

        public Rigidbody ParentRigid;

        /// <summary>
        /// The Transform that is following us
        /// </summary>
        public Transform HandleTransform;

        Grabbable thisGrab;
        Rigidbody rb;
        bool didRelease = false;
        Collider col;

        void Start() {
            thisGrab = GetComponent<Grabbable>();
            thisGrab.CanBeSnappedToSnapZone = false;
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            // Handle and parent shouldn't collide with each other
            if(col != null && ParentRigid != null && ParentRigid.GetComponent<Collider>() != null) {
                Physics.IgnoreCollision(ParentRigid.GetComponent<Collider>(), col, true);
            }
        }

        void Update() {
            if(!thisGrab.BeingHeld) {
                if(!didRelease) {
                    StartCoroutine(doRelease());
                    didRelease = true;
                }
            }
            else {
                // Object is being held, need to fire release
                didRelease = false;

                // Check Break Distance since we are always holding the helper
                if (thisGrab.BreakDistance > 0 && Vector3.Distance(transform.position, HandleTransform.position) > thisGrab.BreakDistance) {
                    thisGrab.DropItem(false, false);
                }
            }
        }

        IEnumerator doRelease() {
            col.enabled = false;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (ParentRigid) {
                ParentRigid.velocity = Vector3.zero;
                ParentRigid.angularVelocity = Vector3.zero;
            }

            yield return new WaitForSeconds(0.1f);

            col.enabled = true;
        }
    }
}

