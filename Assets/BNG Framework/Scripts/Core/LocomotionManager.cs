using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class LocomotionManager : MonoBehaviour {

        /// <summary>
        /// Default locomotion to use if nothing stored in playerprefs. 0 = Teleport. 1 = SmoothLocomotion
        /// </summary>
        public LocomotionType DefaultLocomotion = LocomotionType.Teleport;

        /// <summary>
        /// If true, locomotion type will be saved and loaded from player prefs
        /// </summary>
        public bool LoadLocomotionFromPrefs = true;

        /// <summary>
        /// (Oculus Only) - Click Left Thumbstick Down to toggle between Teleport / Smooth Locomotion
        /// </summary>
        public bool LeftThumbstickToggleLocomotionOculus = true;

        InputBridge input;
        BNGPlayerController player;
        PlayerTeleport teleport;

        void Start() {
            input = GetComponent<InputBridge>();
            player = GetComponent<BNGPlayerController>();
            teleport = GetComponent<PlayerTeleport>();

            // Load Locomotion Preference
            if (LoadLocomotionFromPrefs) {
                ChangeLocomotion(PlayerPrefs.GetInt("LocomotionSelection", 0) == 0 ? LocomotionType.Teleport : LocomotionType.SmoothLocomotion, false);
            }
            else {
                ChangeLocomotion(DefaultLocomotion, false);
            }
        }

        void Update() {
            // Oculus Device Only - Toggle Locomotion by pressing left thumbstick down            
            if (LeftThumbstickToggleLocomotionOculus && input.LeftThumbstickDown && input.IsOculusDevice()) {
                ChangeLocomotion(player.SelectedLocomotion == LocomotionType.SmoothLocomotion ? LocomotionType.Teleport : LocomotionType.SmoothLocomotion, LoadLocomotionFromPrefs);
            }
        }

        public void UpdateTeleportStatus() {
            teleport.enabled = player.SelectedLocomotion == LocomotionType.Teleport;
        }

        public void ChangeLocomotion(LocomotionType locomotionType, bool save) {
            player.ChangeLocomotionType(locomotionType);

            if (save) {
                PlayerPrefs.SetInt("LocomotionSelection", locomotionType == LocomotionType.Teleport ? 0 : 1);
            }

            UpdateTeleportStatus();
        }
    }
}