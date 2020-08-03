using OVRTouchSample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// An example hand controller that sets animation values depending on Grabber state
    /// </summary>
    public class HandController : MonoBehaviour {

        // Control will follow this around
        public Transform HandAnchor;
        public Animator HandAnimator;

        Grabber grabber; // Child Grabber

        /// <summary>
        /// 0 = Open Hand, 1 = Full Grip
        /// </summary>
        public float GripAmount;
        private float _prevGrip;

        /// <summary>
        /// 0 = Index Curled in,  1 = Pointing Finger
        /// </summary>
        public float PointAmount;
        private float _prevPoint;

        /// <summary>
        /// 0 = Thumb Down, 1 = Thumbs Up
        /// </summary>
        public float ThumbAmount;
        private float _prevThumb;

        /// <summary>
        /// How fast to Lerp the Layer Animations
        /// </summary>
        public float HandAnimationSpeed = 20f;

        InputBridge input;

        // Start is called before the first frame update
        void Start() {
            transform.parent = HandAnchor;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            grabber = GetComponentInChildren<Grabber>();
            input = GameObject.FindGameObjectWithTag("Player").GetComponent<InputBridge>();
        }

        // Update is called once per frame
        void Update() {

            // Grabber may have been deactivated
            if (grabber == null || !grabber.isActiveAndEnabled) {
                GripAmount = 0;
                PointAmount = 0;
                ThumbAmount = 0;
                return;
            }

            if (grabber.HandSide == ControllerHand.Left) {
                GripAmount = input.LeftGrip;
                PointAmount = 1 - input.LeftTrigger; // Range between 0 and 1. 1 == Finger all the way out
                PointAmount *= 0.5f; // Reduce the amount our finger points out

                // If not near the trigger, point finger all the way out
                if (!input.LeftTriggerNear && PointAmount != 0) {
                    PointAmount = 1f;
                }

                ThumbAmount = input.LeftThumbNear ? 0 : 1;
            }
            else if (grabber.HandSide == ControllerHand.Right) {
                GripAmount = input.RightGrip;
                PointAmount = 1 - input.RightTrigger; // Range between 0 and 1. 1 == Finger all the way out
                PointAmount *= 0.5f; // Reduce the amount our finger points out

                // If not near the trigger, point finger all the way out
                if (input.RightTriggerNear == false && PointAmount != 0) {
                    PointAmount = 1f;
                }

                ThumbAmount = input.RightThumbNear ? 0 : 1;
            }

            // Force everything to grab if we're holding something
            if (grabber.HoldingItem) {
                GripAmount = 1;
                PointAmount = 0;
                ThumbAmount = 0;                
            }

            // Try getting child animator
            if(HandAnimator == null|| !HandAnimator.isActiveAndEnabled) {
                HandAnimator = GetComponentInChildren<Animator>();
            }

            if (HandAnimator != null) {
                updateAnimimationStates();
            }
        }

        void updateAnimimationStates()
        {            
            if(HandAnimator != null && HandAnimator.isActiveAndEnabled && HandAnimator.runtimeAnimatorController != null) {

                _prevGrip = Mathf.Lerp(_prevGrip, GripAmount, Time.deltaTime * HandAnimationSpeed);
                _prevThumb = Mathf.Lerp(_prevThumb, ThumbAmount, Time.deltaTime * HandAnimationSpeed);
                _prevPoint = Mathf.Lerp(_prevPoint, PointAmount, Time.deltaTime * HandAnimationSpeed);

                // 0 = Hands Open, 1 = Grip closes                        
                HandAnimator.SetFloat("Flex", _prevGrip);

                HandAnimator.SetLayerWeight(1, _prevThumb);

                //// 0 = pointer finger inwards, 1 = pointing out    
                //// Point is played as a blend
                //// Near trigger? Push fin ger down a bit
                HandAnimator.SetLayerWeight(2, _prevPoint);

                // Should we use a custom hand pose?
                if (grabber.HeldGrabbable != null) {
                    HandAnimator.SetLayerWeight(0, 0);
                    HandAnimator.SetLayerWeight(1, 0);
                    HandAnimator.SetLayerWeight(2, 0);

                    HandAnimator.SetInteger("Pose", (int)grabber.HeldGrabbable.CustomHandPose);
                }
                else {
                    HandAnimator.SetInteger("Pose", 0);
                }
            }
        }
    }
}