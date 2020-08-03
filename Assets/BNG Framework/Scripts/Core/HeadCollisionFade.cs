using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public class HeadCollisionFade : MonoBehaviour {

        OVRScreenFade fader;

        public float FadeDistance = 0.1f;
        public float FadeOutDistance = 0.045f;
        /// <summary>
        /// Where to start the fade from
        /// </summary>
        public float MinFade = 0.5f;
        public float MaxFade = 0.95f;
        public float FadeSpeed = 1f;

        public bool IgnoreHeldGrabbables = true;

        public Transform DistanceTransform;
        public int cols = 0;
        private float currentFade = 0;

        public List<Collider> collisions;

        void Start() {
            fader = Camera.main.transform.GetComponent<OVRScreenFade>();
        }

        // Update is called once per frame
        void LateUpdate() {

            bool headColliding = false;

            // Checl for Head Collisions if hmd equipped
            if (OVRManager.hasVrFocus) {
                for (int x = 0; x < collisions.Count; x++) {
                    if (collisions[x] != null && collisions[x].enabled) {
                        headColliding = true;
                        break;
                    }
                }
            }

            if(headColliding) {
                FadeDistance = Vector3.Distance(transform.position, DistanceTransform.position);
            }
            else {
                FadeDistance = 0;
            }
            

            if(fader) {
                // Too far away, fade to black
                if(FadeDistance > FadeOutDistance) {
                    currentFade += Time.deltaTime * FadeSpeed;

                    if (headColliding && currentFade < MinFade) {
                        currentFade = MinFade;
                    }

                    if (currentFade > MaxFade) {
                        currentFade = MaxFade;
                    }

                    fader.SetFadeLevel(currentFade);
                }
                // Fade back
                else {
                    currentFade -= Time.deltaTime * FadeSpeed;

                    if (currentFade < 0) {
                        currentFade = 0;
                    }

                    fader.SetFadeLevel(currentFade);
                }
            }
        }

        void OnCollisionEnter(Collision col) {
            if(collisions == null) {
                collisions = new List<Collider>();
            }

            // Ignore Grabbable Physics objects that are being held
            bool ignorePhysics = IgnoreHeldGrabbables && col.gameObject.GetComponent<Grabbable>() != null && col.gameObject.GetComponent<Grabbable>().BeingHeld;

            // Also ignore physics if this object has a joint attached to it
            if(!ignorePhysics && col.collider.GetComponent<Joint>()) {
                ignorePhysics = true;
            }
            
            if (ignorePhysics) {
                Physics.IgnoreCollision(col.collider, GetComponent<Collider>(), true);
                return;
            }

            if(!collisions.Contains(col.collider)) {
                collisions.Add(col.collider);
                cols++;
            }
        }

        void OnCollisionExit(Collision col) {
            if (collisions == null) {
                collisions = new List<Collider>();
            }

            if (collisions.Contains(col.collider)) {
                collisions.Remove(col.collider);
                cols--;
            }
        }
    }
}