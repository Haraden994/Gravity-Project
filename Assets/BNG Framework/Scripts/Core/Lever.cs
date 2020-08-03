using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    
    /// <summary>
    /// Helper class to interact with physical levers
    /// </summary>
    public class Lever : MonoBehaviour {

        public AudioClip SwitchOnSound;
        public AudioClip SwitchOffSound;

        /// <summary>
        /// Minimum angle the Level can be rotated
        /// </summary>
        public float MinDegrees = -45f;

        /// <summary>
        /// Maximum angle the Level can be rotated
        /// </summary>
        public float MaxDegrees = 45f;

        /// <summary>
        /// Tolerance before considering a switch flipped On or Off
        /// Ex : 1.25 Tolerance means switch can be 98.25% up and considered switched on
        /// </summary>
        public float SwitchTolerance = 1.25f;

        // Current position of the lever as expressed as a percentage 1-100
        public float LeverPercentage;

        /// <summary>
        /// Called when lever was up, but is now in the down position
        /// </summary>
        public UnityEvent onLeverDown;

        /// <summary>
        /// Called when lever was down, but is now in the up position
        /// </summary>
        public UnityEvent onLeverUp;

        /// <summary>
        /// Called if the lever changes position at all
        /// </summary>
        public FloatEvent onLeverChange;

        Grabbable grab;
        HingeJoint hinge;
        AudioSource audioSource;
        bool switchedOn = false;

        void Start() {
            grab = GetComponent<Grabbable>();
            hinge = GetComponent<HingeJoint>();

            // Set Hinge Limits
            if (hinge != null) {
                JointLimits jl = hinge.limits;
                jl.min = MinDegrees;
                jl.max = MaxDegrees;

                hinge.limits = jl;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (SwitchOnSound != null || SwitchOffSound != null)) {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        void Update() {

            // Get the modified angle of of the lever. Use this to get percentage based on Min and Max angles.
            Vector3 currentRotation = transform.localEulerAngles;
            float angle = Mathf.Floor(currentRotation.x);
            angle = (angle > 180) ? angle - 360 : angle;

            // Cap Angles
            if (angle > MaxDegrees) {
                transform.localEulerAngles = new Vector3(MaxDegrees, currentRotation.y, currentRotation.z);
            }
            else if (angle < MinDegrees) {
                transform.localEulerAngles = new Vector3(MinDegrees, currentRotation.y, currentRotation.z);
            }

            // Set percentage of level position
            LeverPercentage = (angle - MinDegrees) / (MaxDegrees - MinDegrees) * 100;

            // Lever value changed event
            OnLeverChange(LeverPercentage);

            // Up / Down Events
            if (LeverPercentage > 99 && !switchedOn) {
                OnLeverUp();
            }
            else if (LeverPercentage < 1 && switchedOn) {
                OnLeverDown();
            }
        }

        public Vector3 lookDirection;
        public Vector3 LookOffset = new Vector3(90f, 0, 0);
        public float DebugVal;

        void doLeverLook() {
            // Do Lever Look
            if (grab != null && grab.BeingHeld) {
                transform.LookAt(grab.GetPrimaryGrabber().transform, lookDirection);
                transform.localEulerAngles += LookOffset;

                DebugVal = transform.localEulerAngles.x;

                //float leverX = Mathf.Clamp(transform.localEulerAngles.x, MinDegrees, MaxDegrees);

                //transform.localEulerAngles = new Vector3(leverX, 0, 0);
            }
        }


        // Callback for lever percentage change
        public virtual void OnLeverChange(float percentage) {
            if(onLeverChange != null) {
                onLeverChange.Invoke(percentage);
            }
        }

        /// <summary>
        /// Lever Moved to down position
        /// </summary>
        public virtual void OnLeverDown() {

            if (SwitchOffSound != null) {
                audioSource.clip = SwitchOffSound;
                audioSource.Play();
            }

            if(onLeverDown != null) {
                onLeverDown.Invoke();
            }

            switchedOn = false;
        }

        /// <summary>
        /// Lever moved to up position
        /// </summary>
        public virtual void OnLeverUp() {

            if (SwitchOnSound != null) {
                audioSource.clip = SwitchOnSound;
                audioSource.Play();
            }

            // Fire event
            if(onLeverUp != null) {
                onLeverUp.Invoke();
            }

            switchedOn = true;

        }
    }

    /// <summary>
    /// A UnityEvent with a float as a parameter
    /// </summary>
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    /// <summary>
    /// A UnityEvent with a 2 floats as parameters
    /// </summary>
    [System.Serializable]
    public class FloatFloatEvent : UnityEvent<float, float> { }

    /// <summary>
    /// A UnityEvent with a Grabber as the parameter
    /// </summary>
    [System.Serializable]
    public class GrabberEvent : UnityEvent<Grabber> { }

    /// <summary>
    /// A UnityEvent with a Grabbable as the parameter
    /// </summary>
    [System.Serializable]
    public class GrabbableEvent : UnityEvent<Grabbable> { }
}
