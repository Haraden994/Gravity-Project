﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class DoorHelper : MonoBehaviour {

        public AudioClip DoorOpenSound;
        public AudioClip DoorCloseSound;

        /// <summary>
        /// Does the handle need to be turned in order to open the door from the closed position?
        /// </summary>
        [Tooltip("Does the handle need to be turned in order to open the door from the closed position?")]
        public bool RequireHandleTurnToOpen = false;

        /// <summary>
        /// This transform is used to determine how many degrees have been turned. Required if RequireHandleTurnToOpen is true
        /// </summary>
        public Transform HandleFollower;

        public float DegreesTurned;

        /// <summary>
        /// How many degrees the handle must turn in order for the latch to be open
        /// </summary>
        public float DegreesTurnToOpen = 10f;

        /// <summary>
        /// Rotate this transform with Handle Rotation
        /// </summary>
        public Transform DoorLockTransform;
        float initialLockPosition;

        HingeJoint hinge;
        Rigidbody rigid;
        bool playedOpenSound = false;

        // Need to open door up a certain amount before considering playing a close sound afterwards
        bool readyToPlayCloseSound = false;

        public float AngularVelocitySnapDoor = 0.2f;

        void Start() {
            hinge = GetComponent<HingeJoint>();
            rigid = GetComponent<Rigidbody>();

            if(DoorLockTransform) {
                initialLockPosition = DoorLockTransform.transform.localPosition.x;
            }
        }

        public float angle;
        public float AngularVelocity = 0.2f;
        bool doorLocked = false;

        public float lockPos;

        void Update() {

            // Read Angular Velocity used for snapping door shut
            AngularVelocity = rigid.angularVelocity.magnitude;

            // Get the modified angle of of the lever. Use this to get percentage based on Min and Max angles.
            Vector3 currentRotation = transform.localEulerAngles;
            angle = Mathf.Floor(currentRotation.y);

            if(angle >= 180) {
                angle -= 180; 
            }
            else {
                angle = 180 - angle;
            }

            // Play Open Sound
            if (angle > 10) {

                if(!playedOpenSound) {
                    VRUtils.Instance.PlaySpatialClipAt(DoorOpenSound, transform.position, 1f, 1f);
                    playedOpenSound = true;
                }
            }

            if(angle > 30) {
                readyToPlayCloseSound = true;
            }

            // Reset Open Sound
            if(angle < 2 && playedOpenSound) {
                playedOpenSound = false;
            }

            // Should we snap the door closed?
            if (angle < 1 && AngularVelocity <= AngularVelocitySnapDoor) {
                rigid.angularVelocity = Vector3.zero;
            }

            // Play Close Sound
            if (readyToPlayCloseSound && angle < 2) {
                VRUtils.Instance.PlaySpatialClipAt(DoorCloseSound, transform.position, 1f, 1f);
                readyToPlayCloseSound = false;
            }

            // Calculate Handle if available
            if (HandleFollower) {
                DegreesTurned = Mathf.Abs(HandleFollower.localEulerAngles.y - 270);
            }

            if(DoorLockTransform) {
                // 45 Degrees = Fully Open
                float moveLockAmount = 0.025f;
                float rotateAngles = 55;
                float ratio = rotateAngles / (rotateAngles - Mathf.Clamp(DegreesTurned, 0, rotateAngles));
                lockPos =  initialLockPosition - (ratio * moveLockAmount) + moveLockAmount;
                lockPos = Mathf.Clamp(lockPos, initialLockPosition - moveLockAmount, initialLockPosition);

                DoorLockTransform.transform.localPosition = new Vector3(lockPos, DoorLockTransform.transform.localPosition.y, DoorLockTransform.transform.localPosition.z);
            }

            // Set Lock Status
            if(RequireHandleTurnToOpen) {
                doorLocked = DegreesTurned < DegreesTurnToOpen;
            }

            // Lock Door in place if closed and requires handle to be turned
            rigid.isKinematic = angle < 0.02f && doorLocked;
        }
    }
}