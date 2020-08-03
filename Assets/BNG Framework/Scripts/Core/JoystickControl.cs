using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Helper for joystick type physical inputs
    /// </summary>
    public class JoystickControl : MonoBehaviour {

        /// <summary>
        /// Minimum angle the Level can be rotated
        /// </summary>
        public float MinDegrees = -45f;

        /// <summary>
        /// Maximum angle the Level can be rotated
        /// </summary>
        public float MaxDegrees = 45f;

        /// <summary>
        /// Current Percentage of joystick on X axis (left / right)
        /// </summary>
        public float LeverPercentageX = 0;

        /// <summary>
        /// Current Percentage of joystick on Z axis (forward / back)
        /// </summary>
        public float LeverPercentageZ = 0;

        /// <summary>
        /// Event called when Joystick value is changed
        /// </summary>
        public FloatFloatEvent onJoystickChange;

        // Keep track of Joystick Rotation
        Vector3 currentRotation;
        float angleX;
        float angleZ;

        // Update is called once per frame
        void Update() {
            // Get the modified angle of of the lever. Use this to get percentage based on Min and Max angles.
            currentRotation = transform.localEulerAngles;
            angleX = Mathf.Floor(currentRotation.x);
            angleX = (angleX > 180) ? angleX - 360 : angleX;

            angleZ = Mathf.Floor(currentRotation.z);
            angleZ = (angleZ > 180) ? angleZ - 360 : angleZ;

            // Cap Angles X
            if (angleX > MaxDegrees) {
                transform.localEulerAngles = new Vector3(MaxDegrees, currentRotation.y, currentRotation.z);
            }
            else if (angleX < MinDegrees) {
                transform.localEulerAngles = new Vector3(MinDegrees, currentRotation.y, currentRotation.z);
            }

            // Cap Angles Z
            if (angleZ > MaxDegrees) {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentRotation.y, MaxDegrees);
            }
            else if (angleZ < MinDegrees) {
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentRotation.y, MinDegrees);
            }

            // Set percentage of level position
            LeverPercentageX = (angleX - MinDegrees) / (MaxDegrees - MinDegrees) * 100;
            LeverPercentageZ = (angleZ - MinDegrees) / (MaxDegrees - MinDegrees) * 100;

            // Lever value changed event
            OnJoystickChange(LeverPercentageX, LeverPercentageZ);
        }

        // Callback for lever percentage change
        public virtual void OnJoystickChange(float leverX, float leverY) {
            if (onJoystickChange != null) {
                onJoystickChange.Invoke(leverX, leverY);
            }
        }
    }
}
