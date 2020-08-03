using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.XR;
#endif

namespace BNG {

    public enum ControllerHand {
        Left,
        Right,
        None
    }

    public enum HandControl {
        LeftGrip,
        RightGrip,
        LeftTrigger,
        RightTrigger,
        None
    }

    public enum GrabButton {
        Grip,
        Trigger
    }

    public enum XRInputSource {
        XRInput,
        OVRInput
    }

    /// <summary>
    /// A proxy for handling input from Oculus. 
    /// </summary>
    public class InputBridge : MonoBehaviour {

        public XRInputSource InputSource = XRInputSource.OVRInput;

        /// <summary>
        /// How far Left Grip is Held down. Values : 0 - 1 (Fully Open / Closed)
        /// </summary>
        public float LeftGrip = 0;

        /// <summary>
        /// Left Grip was pressed down this drame, but not last
        /// </summary>
        public bool LeftGripDown = false;

        /// <summary>
        /// How far Right Grip is Held down. Values : 0 - 1 (Fully Open / Closed)
        /// </summary>
        public float RightGrip = 0;

        /// <summary>
        /// Right Grip was pressed down this drame, but not last
        /// </summary>
        public bool RightGripDown = false;

        public float LeftTrigger = 0;
        public bool LeftTriggerNear = false;
        public bool LeftTriggerDown = false;
        public float RightTrigger = 0;
        public bool RightTriggerDown = false;
        public bool RightTriggerNear = false;

        public bool LeftThumbNear = false;
        public bool RightThumbNear = false;

        /// <summary>
        /// Pressed down this drame, but not last
        /// </summary>
        public bool LeftThumbstickDown = false;
        public bool RightThumbstickDown = false;

        /// <summary>
        /// CurrentlyHeldDown
        /// </summary>
        public bool LeftThumbstick = false;
        public bool RightThumbstick = false;

        // Oculus Touch Controllers
        public bool AButton = false;
        public bool AButtonDown = false;
        public bool BButton = false;
        public bool BButtonDown = false;
        public bool XButton = false;
        public bool XButtonDown = false;
        public bool YButton = false;
        public bool YButtonDown = false;

        public bool StartButton = false;
        public bool StartButtonDown = false;
        public bool BackButton = false;
        public bool BackButtonDown = false;

        public Vector2 LeftThumbstickAxis;
        public Vector2 RightThumbstickAxis;

        /// <summary>
        /// Thumbstick X must be greater than this amount to be considered valid
        /// </summary>
        [Tooltip("Thumbstick X must be greater than this amount to be considered valid")]
        public float ThumbstickDeadzoneX = 0.001f;

        /// <summary>
        /// Thumbstick Y must be greater than this amount to be considered valid
        /// </summary>
        [Tooltip("Thumbstick Y must be greater than this amount to be considered valid")]
        public float ThumbstickDeadzoneY = 0.001f;

#if UNITY_2019_2_OR_NEWER
        static List<InputDevice> devices = new List<InputDevice>();
#endif

        // What threshold constitutes a "down" event.
        // For example, pushing the trigger down 20% (0.2) of the way considered starting a trigger down event
        // This is used in XRInput
        private float _downThreshold = 0.2f;

        bool XRInputSupported = false;

        void Start() {
#if UNITY_2019_3_OR_NEWER
            XRInputSupported = true;
#endif
        }

        void Update() {

            // Use OVRInput to get more Oculus Specific inputs, such as "Near Touch"
            if (InputSource == XRInputSource.OVRInput || !XRInputSupported) {

                LeftThumbstickAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick), ThumbstickDeadzoneX, ThumbstickDeadzoneY);
                RightThumbstickAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick), ThumbstickDeadzoneX, ThumbstickDeadzoneY);

                LeftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
                LeftGripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);

                RightGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
                RightGripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

                LeftTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
                LeftTriggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

                RightTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
                RightTriggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

                LeftTriggerNear = OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
                LeftThumbNear = OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, OVRInput.Controller.LTouch);

                RightTriggerNear = OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
                RightThumbNear = OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, OVRInput.Controller.RTouch);
            }
            else {
#if UNITY_2019_3_OR_NEWER
                // Refresh XR devices
                InputDevices.GetDevices(devices);

                // Left XR Controller
                var leftHandedControllers = new List<InputDevice>();
                var dc = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
                InputDevices.GetDevicesWithCharacteristics(dc, leftHandedControllers);
                var primaryLeftController = leftHandedControllers.FirstOrDefault();

                // Right XR Controller
                var rightHandedControllers = new List<InputDevice>();
                dc = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
                InputDevices.GetDevicesWithCharacteristics(dc, rightHandedControllers);
                var primaryRightController = rightHandedControllers.FirstOrDefault();

                LeftThumbstickAxis = ApplyDeadZones(getFeatureUsage(primaryLeftController, CommonUsages.primary2DAxis), ThumbstickDeadzoneX, ThumbstickDeadzoneY);
                RightThumbstickAxis = ApplyDeadZones(getFeatureUsage(primaryRightController, CommonUsages.primary2DAxis), ThumbstickDeadzoneX, ThumbstickDeadzoneY);

                // Store copy of previous value so we can determin if we need to call OnDownEvent
                var prevVal = LeftGrip;
                LeftGrip = getFeatureUsage(primaryLeftController, CommonUsages.grip);
                LeftGripDown = prevVal < _downThreshold && LeftGrip >= _downThreshold;

                prevVal = RightGrip;
                RightGrip = getFeatureUsage(primaryRightController, CommonUsages.grip);
                RightGripDown = prevVal < _downThreshold && RightGrip >= _downThreshold;

                prevVal = LeftTrigger;
                LeftTrigger = getFeatureUsage(primaryLeftController, CommonUsages.trigger);
                LeftTriggerDown = prevVal < _downThreshold && LeftTrigger >= _downThreshold;

                prevVal = RightTrigger;
                RightTrigger = getFeatureUsage(primaryRightController, CommonUsages.trigger);
                RightTriggerDown = prevVal < _downThreshold && RightTrigger >= _downThreshold;

                LeftTriggerNear = getFeatureUsage(primaryLeftController, CommonUsages.indexTouch) > 0;
                LeftThumbNear = getFeatureUsage(primaryLeftController, CommonUsages.thumbTouch) > 0;

                RightTriggerNear = getFeatureUsage(primaryRightController, CommonUsages.indexTouch) > 0;
                RightThumbNear = getFeatureUsage(primaryRightController, CommonUsages.thumbTouch) > 0;
#endif
            }

            // OVRInput can handle these inputs :
            AButton = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
            AButtonDown = OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch);

            BButton = OVRInput.Get(OVRInput.Button.Two);
            BButtonDown = OVRInput.GetDown(OVRInput.Button.Two);
            XButton = OVRInput.Get(OVRInput.Button.Three);
            XButtonDown = OVRInput.GetDown(OVRInput.Button.Three);
            YButton = OVRInput.Get(OVRInput.Button.Four);
            YButtonDown = OVRInput.GetDown(OVRInput.Button.Four);

            StartButton = OVRInput.Get(OVRInput.Button.Start);
            StartButtonDown = OVRInput.GetDown(OVRInput.Button.Start);

            BackButton = OVRInput.Get(OVRInput.Button.Back);
            BackButtonDown = OVRInput.GetDown(OVRInput.Button.Back);

            LeftThumbstickDown = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            RightThumbstickDown = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);

            LeftThumbstick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            RightThumbstick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
        }

        Vector2 ApplyDeadZones(Vector2 pos, float deadZoneX, float deadZoneY) {

            // X Positive
            if (pos.x > 0 && pos.x < deadZoneX) {
                pos = new Vector2(deadZoneX, pos.y);
            }
            // X Negative
            else if (pos.x < 0 && pos.x > -ThumbstickDeadzoneX) {
                pos = new Vector2(-deadZoneX, pos.y);
            }

            // Y Positive
            if (pos.y > 0 && pos.y < deadZoneY) {
                pos = new Vector2(pos.y, deadZoneY);
            }
            // Y Negative
            else if (pos.y < 0 && pos.y > -ThumbstickDeadzoneY) {
                pos = new Vector2(pos.y, -deadZoneY);
            }

            return pos;
        }

        public virtual bool IsOculusDevice() {
#if UNITY_2019_2_OR_NEWER
            return XRSettings.loadedDeviceName != null && XRSettings.loadedDeviceName.ToLower().Contains("oculus");
#else
                return true;
#endif
        }

        public virtual bool IsOculusQuest() {
#if UNITY_2019_2_OR_NEWER
            return IsOculusDevice() && XRDevice.refreshRate == 72f;
#else
                return Application.platform == RuntimePlatform.Android;
#endif
        }

        public virtual bool IsHTCDevice() {
#if UNITY_2019_2_OR_NEWER
            return XRSettings.loadedDeviceName.StartsWith("HTC");
#else
                return false;
#endif
        }

#if UNITY_2019_2_OR_NEWER
        float getFeatureUsage(InputDevice device, InputFeatureUsage<float> usage, bool clamp = true) {
            float val;
            device.TryGetFeatureValue(usage, out val);

            return Mathf.Clamp01(val);
        }

        bool getFeatureUsage(InputDevice device, InputFeatureUsage<bool> usage) {
            bool val;
            if (device.TryGetFeatureValue(usage, out val)) {
                return val;
            }

            return val;
        }

        Vector2 getFeatureUsage(InputDevice device, InputFeatureUsage<Vector2> usage) {
            Vector2 val;
            if (device.TryGetFeatureValue(usage, out val)) {
                return val;
            }

            return val;
        }
#endif

#if UNITY_2019_3_OR_NEWER
        public void SetTrackingOrigin(TrackingOriginModeFlags trackingOrigin) {
            // Set to Floor Mode
            List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(subsystems);
            for (int i = 0; i < subsystems.Count; i++) {
                subsystems[i].TrySetTrackingOriginMode(trackingOrigin);
            }
        }
#endif


        // Start Vibration on controller
        public void VibrateController(float frequency, float amplitude, float duration, ControllerHand hand) {
            StartCoroutine(Vibrate(frequency, amplitude, duration, hand));
        }

        IEnumerator Vibrate(float frequency, float amplitude, float duration, ControllerHand hand) {
            // Start vibration
            if (hand == ControllerHand.Right) {
                OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
            }
            else if (hand == ControllerHand.Left) {
                OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);
            }

            yield return new WaitForSeconds(duration);

            // Stop vibration
            if (hand == ControllerHand.Right) {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
            }
            else if (hand == ControllerHand.Left) {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
            }
        }
    }
}