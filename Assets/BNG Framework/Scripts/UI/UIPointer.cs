using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Point a line  at our GazePointer
    /// </summary>
    public class UIPointer : MonoBehaviour {
        
        /// <summary>
        /// The transform to point from. May also contain a LineRenderer Component
        /// </summary>
        public Transform PointerObject;
        LineRenderer uiLiner;

        OVRGazePointer uiCursor;

        // How far away from the UI canvas can we be before we stop showing the pointer
        public float MaxDistance = 5f;

        /// <summary>
        /// 0.5 = Line Goes Half Way. 1 = Line reaches end.
        /// </summary>
        public float LineDistanceModifier = 0.8f;
        
        /// <summary>
        /// Lock the UI object scale to this amount. Prevents the object from scaling up or down based on distance
        /// </summary>
        public float PointerLocalScale = 0.01f;

        // Start is called before the first frame update
        void Start() {
            uiCursor = GetComponent<OVRGazePointer>();

            if(PointerObject) {
                uiLiner = PointerObject.GetComponent<LineRenderer>();
                uiLiner.useWorldSpace = false; // Keep this in local space
            }
        }

        void LateUpdate() {
            transform.localScale = new Vector3(PointerLocalScale, PointerLocalScale, PointerLocalScale);

            // Draw Line
            if(uiLiner) {
                if(uiCursor.visibilityStrength >  0.1f) {
                    // Set LineRenderer position
                    uiLiner.enabled = true;
                    
                    float dist = Vector3.Distance(transform.position, PointerObject.position);     
                    
                    // Don't show if far away
                    if(dist > MaxDistance) {
                        uiLiner.enabled = false;
                    }
                    else {
                        uiLiner.SetPosition(1, new Vector3(0, 0, dist * LineDistanceModifier));
                    }
                    
                }
                else {
                    uiLiner.enabled = false;
                }
            }
        }
    }
}

