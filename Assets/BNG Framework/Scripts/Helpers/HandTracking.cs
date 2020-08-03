using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static OVRHand;

namespace BNG {
    public class HandTracking : MonoBehaviour {

        public OVRHand LeftHand;
        public OVRHand RightHand;

        public Grabber LeftGrabber;
        public Grabber RightGrabber;

        public Transform LeftModelHolder;
        public Transform RightModelHolder;

        /// <summary>
        /// Are we currently doing hand tracking?
        /// </summary>       
        public bool IsHandTracking = false;
        bool wasHandTracking = false; // used to detect hand tracking toggle

        public TrackingConfidence LeftHandConfidence;
        public TrackingConfidence RightHandConfidence;

        public bool IsLeftIndexPinching = false;
        public float LeftIndexPinchStrength;

        public bool IsRightIndexPinching = false;
        public float RightIndexPinchStrength;
        public Vector3 RightIndexPosition;
        public Vector3 LeftIndexPosition;

        /// <summary>
        /// Pinching will activate Grabber attached to Index Finger
        /// </summary>
        public bool DoPinchToGrab = true;

        OVRSkeleton leftSkele;
        OVRSkeleton rightSkele;
        OVRBone leftIndexBone;
        OVRBone rightIndexBone;

        /// <summary>
        /// Disable Hand Tracking when using Oculus Link. Enabling may cause Unity crash
        /// </summary>
        public bool DisableHandTrackingInEditor = true;

        void Awake() {
            leftSkele = LeftHand.GetComponent<OVRSkeleton>();
            rightSkele = RightHand.GetComponent<OVRSkeleton>();

            // This fixes a Unity crash to desktop in Oculus 1.32 when using hand tracking on start in editor mode
            if (DisableHandTrackingInEditor && Application.isEditor && leftSkele != null) {
                leftSkele.enabled = false;
                rightSkele.enabled = false;

                // See https://forum.unity.com/threads/released-vr-interaction-framework-for-oculus-quest.817614/reply?quote=5468730
                Debug.Log("Disabling Hand Tracking in Editor due to CTD bug in Oculus SDK");
            }
        }

        void Update() {

            updateHandTracking();

            if(IsHandTracking) {
                LeftHandConfidence = LeftHand.GetFingerConfidence(HandFinger.Index);
                RightHandConfidence = RightHand.GetFingerConfidence(HandFinger.Index);

                if(leftSkele != null && leftSkele.Bones != null) {
                    leftIndexBone = leftSkele.Bones.FirstOrDefault(x => x.Id == OVRSkeleton.BoneId.Hand_IndexTip);
                    if (leftIndexBone != null) {
                        LeftIndexPosition = leftIndexBone.Transform.position;
                    }
                }
                
                IsLeftIndexPinching = LeftHand.GetFingerIsPinching(HandFinger.Index) && LeftHandConfidence == TrackingConfidence.High;
                LeftIndexPinchStrength = LeftHand.GetFingerPinchStrength(HandFinger.Index);

                if(rightSkele && rightSkele.Bones != null) {
                    rightIndexBone = rightSkele.Bones.FirstOrDefault(x => x.Id == OVRSkeleton.BoneId.Hand_IndexTip);
                    if (rightIndexBone != null) {
                        RightIndexPosition = rightIndexBone.Transform.position;
                    }
                }

                IsRightIndexPinching = RightHand.GetFingerIsPinching(HandFinger.Index) && RightHandConfidence == TrackingConfidence.High;
                RightIndexPinchStrength = RightHand.GetFingerPinchStrength(HandFinger.Index);
            }

            updateGrabbers();
        }

        void updateHandTracking() {
            
            IsHandTracking = OVRInput.GetActiveController() == OVRInput.Controller.Hands;

            if(IsHandTracking != wasHandTracking) {
                onHandTrackingChange(IsHandTracking);
            }

            wasHandTracking = IsHandTracking;
        }

        void onHandTrackingChange(bool handTrackingEnabled) {
            // We'll consider a controller active for anything but Hands
            LeftModelHolder.gameObject.SetActive(!handTrackingEnabled);
            RightModelHolder.gameObject.SetActive(!handTrackingEnabled);
        }

        void updateGrabbers() {

            if(LeftGrabber) {
                LeftGrabber.gameObject.SetActive(IsHandTracking);

                if (IsHandTracking) {

                    LeftGrabber.transform.position = LeftIndexPosition;
                    LeftGrabber.ForceGrab = DoPinchToGrab && IsLeftIndexPinching;
                    LeftGrabber.ForceRelease = DoPinchToGrab && IsLeftIndexPinching == false;
                }
            }

            if (RightGrabber) {
                RightGrabber.gameObject.SetActive(IsHandTracking);

                if (IsHandTracking) {
                    RightGrabber.transform.position = RightIndexPosition;
                    RightGrabber.ForceGrab = DoPinchToGrab && IsRightIndexPinching;
                    RightGrabber.ForceRelease = DoPinchToGrab && IsRightIndexPinching == false;
                }
            }
        }
    }
}

