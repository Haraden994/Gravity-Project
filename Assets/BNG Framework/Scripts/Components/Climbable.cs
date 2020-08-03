using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Allows the Player to climb objects by Grabbing them
    /// </summary>
    public class Climbable : Grabbable {

        BNGPlayerController bngController;

        void Start() {
            // Make sure Climbable is set to dual grab
            SecondaryGrabBehavior = OtherGrabBehavior.DualGrab;

            // Make sure we don't try tp keep this in our hand
            GrabPhysics = GrabPhysics.None;

            CanBeSnappedToSnapZone = false;
        }

        public override void GrabItem(Grabber grabbedBy) {

            // Add the climber so we can track it's position for Character movement
            if (bngController == null) {
                bngController = GameObject.FindGameObjectWithTag("Player").GetComponent<BNGPlayerController>();
            }
            bngController.AddClimber(this, grabbedBy);
            
            base.GrabItem(grabbedBy);        
        }

        public override void DropItem(Grabber droppedBy) {
            if(droppedBy != null) {
                Debug.Log("Dropping " + droppedBy.HandSide);
                bngController.RemoveClimber(droppedBy);
            }
            
            base.DropItem(droppedBy);
        }
    }
}