using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class HandModelSelector : MonoBehaviour {

        /// <summary>
        /// Child index of the hand model to use if nothing stored in playerprefs and LoadHandSelectionFromPrefs true
        /// </summary>
        public int DefaultHandsModel = 1;

        /// <summary>
        /// If true, hand model will be saved and loaded from player prefs
        /// </summary>
        public bool LoadHandSelectionFromPrefs = true;

        /// <summary>
        /// Click Right Thumbstick Down to toggle between hand models. Mostly useful for testing.
        /// </summary>
        public bool RightThumbstickToggleHands = false;

        /// <summary>
        /// This transform holds all of the hand models. Can be used to enabled / disabled various hand options
        /// </summary>
        public Transform LeftHandGFXHolder;

        /// <summary>
        /// This transform holds all of the hand models. Can be used to enabled / disabled various hand options
        /// </summary>
        public Transform RightHandGFXHolder;
        private int _selectedHandGFX = 0;

        /// <summary>
        /// Used for demo scene IK Hands / Body
        /// </summary>
        public CharacterIK IKBody;

        /// <summary>
        /// This is the start point of a line for UI purposes. We may want to move this around if we change models or controllers.        
        /// </summary>
        UIPointer uiPoint;

        InputBridge input;

        // Start is called before the first frame update
        void Start() {
            input = GetComponent<InputBridge>();
            uiPoint = GetComponentInChildren<UIPointer>();

            // Load new Hands. Default to White hands (3rd child child)
            if (LoadHandSelectionFromPrefs) {
                ChangeHandsModel(PlayerPrefs.GetInt("HandSelection", DefaultHandsModel), false);
            }
        }

        // Update is called once per frame
        void Update() {
            // Cycle through hand models with Right Thumbstick
            if ((RightThumbstickToggleHands && input.RightThumbstickDown) || Input.GetKeyDown(KeyCode.N)) {
                ChangeHandsModel(_selectedHandGFX + 1, LoadHandSelectionFromPrefs);
            }
        }

        public void ChangeHandsModel(int childIndex, bool save = false) {

            // Deactivate Old
            LeftHandGFXHolder.GetChild(_selectedHandGFX).gameObject.SetActive(false);
            RightHandGFXHolder.GetChild(_selectedHandGFX).gameObject.SetActive(false);

            // Loop back to beginning if we went over
            _selectedHandGFX = childIndex;
            if (_selectedHandGFX > LeftHandGFXHolder.childCount - 1) {
                _selectedHandGFX = 0;
            }

            // Activate New
            GameObject leftHand = LeftHandGFXHolder.GetChild(_selectedHandGFX).gameObject;
            GameObject rightHand = RightHandGFXHolder.GetChild(_selectedHandGFX).gameObject;

            leftHand.SetActive(true);
            rightHand.SetActive(true);

            // Update any animators
            HandController leftControl = LeftHandGFXHolder.parent.GetComponent<HandController>();
            HandController rightControl = RightHandGFXHolder.parent.GetComponent<HandController>();
            if (leftControl && rightControl) {
                leftControl.HandAnimator = leftHand.GetComponentInChildren<Animator>();
                rightControl.HandAnimator = rightHand.GetComponentInChildren<Animator>();
            }

            // Enable / Disable IK Character. For demo purposes only
            if (IKBody != null) {
                IKBody.gameObject.SetActive(leftHand.transform.name.Contains("IK"));
            }

            // Change UI Pointer position depending on if we're using Oculus Hands or Oculus Controller Model
            // This is for the demo. Typically this would be fixed to a bone or transform
            // Oculus Touch Controller is positioned near the front
            if (_selectedHandGFX == 0 && uiPoint != null) {
                uiPoint.PointerObject.localPosition = new Vector3(0, 0, 0.0462f);
                uiPoint.PointerObject.localEulerAngles = new Vector3(0, -4.5f, 0);
            }
            // Hand Model
            else if (_selectedHandGFX != 0 && uiPoint != null) {
                uiPoint.PointerObject.localPosition = new Vector3(0.045f, 0.07f, 0.12f);
                uiPoint.PointerObject.localEulerAngles = new Vector3(-9.125f, 4.65f, 0);
            }

            if (save) {
                PlayerPrefs.SetInt("HandSelection", _selectedHandGFX);
            }
        }
    }
}