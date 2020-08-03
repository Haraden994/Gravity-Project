using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    public enum LocomotionType {
        Teleport,
        SmoothLocomotion,
        None
    }

    /// <summary>
    /// The BNGPlayerController handles basic player movement and climbing.
    /// </summary>
    public class BNGPlayerController : MonoBehaviour {

        [SerializeField]
        LocomotionType selectedLocomotion = LocomotionType.Teleport;
        public LocomotionType SelectedLocomotion {
            get { return selectedLocomotion; }
        }

        [HideInInspector]
        public float LastTeleportTime;

        [HideInInspector]
        public float LastPlayerMoveTime;

        /// <summary>
        /// Whether or not we are currently holding on to something climbable with 1 or more grabbers
        /// </summary>
        public bool GrippingClimbable = false;

        /// <summary>
        /// Is Gravity Currently Enabled for this Character
        /// </summary>
        public bool GravityEnabled = true;

        public LayerMask GroundedLayers;

        private Vector3 moveDirection = Vector3.zero;

        /// <summary>
        /// 0 means we are grounded
        /// </summary>
        public float DistanceFromGround = 0;

        /// <summary>
        /// If player goes below this elevation they will be reset to their initial starting position.
        /// If the player goes too far away from the center they may start to jitter due to floating point precisions.
        /// Can also use this to detect if player somehow fell through a floor. Or if the "floor is lava".
        /// </summary>
        public float MinElevation = -5000f;

        /// <summary>
        /// If player goes above this elevation they will be reset to their initial starting position.
        /// If the player goes too far away from the center they may start to jitter due to floating point precisions.
        /// </summary>
        public float MaxElevation = 5000f;

        // Any climber grabbers in use
        List<Grabber> climbers;

        // The controller to manipulate
        CharacterController characterController;
        OVRPlayerController pControl;
        PlayerTeleport teleport;

        private float _initialGravityModifier;
        private Vector3 _initialPosition;

        public bool RecentlyMoved { get; private set; }

        Vector3 lastPlayerPosition;

        void Start() {
            characterController = GetComponentInChildren<CharacterController>();

            pControl = GetComponentInChildren<OVRPlayerController>();
            _initialGravityModifier = pControl.GravityModifier;

            _initialPosition = characterController.transform.position;
            float initialY = _initialPosition.y;
            if (initialY < MinElevation) {
                Debug.LogWarning("Initial Starting Position is lower than Minimum Elevation. Increasing Min Elevation to " + MinElevation);
                MinElevation = initialY;
            }
            if (initialY > MaxElevation) {
                Debug.LogWarning("Initial Starting Position is greater than Maximum Elevation. Reducing Max Elevation to " + MaxElevation);
                MaxElevation = initialY;
            }

            teleport = GetComponent<PlayerTeleport>();

            climbers = new List<Grabber>();

            ChangeLocomotionType(selectedLocomotion);
        }

        void Update() {

            if (characterController) {

                // Update revently moved status
                updateMoveTime();

                // Recently Moved if position changed to teleport of some kind
                RecentlyMoved = Vector3.Distance(lastPlayerPosition, characterController.transform.position) > 0.001f;

                // Considered recently moved if just teleported
                if (!RecentlyMoved && Time.time - LastTeleportTime < 0.1f) {
                    RecentlyMoved = true;
                }

                // Considered recently moved if just moved using PlayerController (for example, snap turning)
                if (!RecentlyMoved && Time.time - LastPlayerMoveTime < 1f) {
                    RecentlyMoved = true;
                }

                // Store player position so we can compare against it next frame
                lastPlayerPosition = characterController.transform.position;
            }

            // Smoth locomotion enabled / disabled
            if (pControl) {
                pControl.EnableLinearMovement = selectedLocomotion == LocomotionType.SmoothLocomotion;
            }

            checkClimbing();

            // Update Gravity based on public variable. CLimbing overrides gravity setting
            UpdateGravity(GravityEnabled && !GrippingClimbable);
        }

        void FixedUpdate() {

            // Player should never go above or below 6000 units as physics can start to jitter due to floating point precision
            if(characterController.transform.position.y < MinElevation || characterController.transform.position.y > MaxElevation) {
                characterController.transform.position = _initialPosition;
            }

            DistanceFromGround = 9999;

            RaycastHit hit;
            if (Physics.Raycast(characterController.transform.position, -characterController.transform.up, out hit, 20, GroundedLayers, QueryTriggerInteraction.Ignore)) {
                DistanceFromGround = Vector3.Distance(characterController.transform.position, hit.point);

                DistanceFromGround -= characterController.height / 2;
            }
        }

        bool grippingAtLeastOneClimbable() {

            if(climbers != null && climbers.Count > 0) {

                for(int x = 0; x < climbers.Count; x++) {
                    // Climbable is still being held
                    if(climbers[x] != null && climbers[x].HoldingItem) {
                        return true;
                    }
                }

                // If we made it through every climber and none were valid, reset the climbers
                climbers = new List<Grabber>();
            }

            return false;
        }

        void checkClimbing() {
            GrippingClimbable = grippingAtLeastOneClimbable(); ;

            if (GrippingClimbable) {

                pControl.enabled = false;

                moveDirection = Vector3.zero;

                int count = 0;
                for (int i = 0; i < climbers.Count; i++) {
                    Grabber climber = climbers[i];
                    if (climber != null && climber.HoldingItem) {
                        Vector3 climberMoveAmount = climber.PreviousPosition - climber.transform.position;

                        if (count == 0) {
                            moveDirection += climberMoveAmount;
                        }
                        else {
                            moveDirection += climberMoveAmount - moveDirection;
                        }

                        count++;
                    }
                }

                characterController.Move(moveDirection);
            }
            else {
                pControl.enabled = true;
            }


            // Update any climber previous position
            for (int x = 0; x < climbers.Count; x++) {
                Grabber climber = climbers[x];
                if (climber != null && climber.HoldingItem) {
                    if (climber.DummyTransform != null) {
                        // Use climber position if possible
                        climber.PreviousPosition = climber.DummyTransform.position;
                    }
                    else {
                        climber.PreviousPosition = climber.transform.position;
                    }
                }
            }
        }

        void updateMoveTime() {
            // Considered recently moved if just moved using CharacterController
            // We don't have access to a snap turn variable in OVRPlayerController, so we have to check the input ourself
            if (pControl && pControl.EnableRotation && pControl.SnapRotation) {
                if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickLeft) || (pControl.RotationEitherThumbstick && OVRInput.Get(OVRInput.Button.PrimaryThumbstickLeft))) {
                    LastPlayerMoveTime = Time.time;
                }
                else if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickRight) || (pControl.RotationEitherThumbstick && OVRInput.Get(OVRInput.Button.PrimaryThumbstickRight))) {
                    LastPlayerMoveTime = Time.time;
                }
            }
        }

        public void ChangeLocomotionType(LocomotionType loc) {
            selectedLocomotion = loc;

            if(teleport == null) {
                teleport = GetComponent<PlayerTeleport>();
            }

            if(selectedLocomotion == LocomotionType.Teleport) {
                teleport.EnableTeleportation();
            }
            else if (selectedLocomotion == LocomotionType.SmoothLocomotion) {
                teleport.DisableTeleportation();
            }
            // Default to Disable All
            else {
                // Disable all
                teleport.DisableTeleportation();
            }
        }

        public void ToggleLocomotionType() {
            // Toggle based on last value
            if(selectedLocomotion == LocomotionType.SmoothLocomotion) {
                ChangeLocomotionType(LocomotionType.Teleport);
            }
            else {
                ChangeLocomotionType(LocomotionType.SmoothLocomotion);
            }
        }

        public void UpdateGravity(bool gravityOn) {
            if (gravityOn) {
                pControl.GravityModifier = _initialGravityModifier;
            }
            else {
                pControl.GravityModifier = 0;
            }
        }

        public void AddClimber(Climbable climbable, Grabber grab) {
            if (!climbers.Contains(grab)) {

                if(grab.DummyTransform == null) {
                    GameObject go = new GameObject();
                    go.transform.name = "DummyTransform";
                    go.transform.parent = grab.transform;
                    go.transform.position = grab.transform.position;
                    go.transform.localEulerAngles = Vector3.zero;

                    grab.DummyTransform = go.transform;
                }

                // Set parent to whatever we grabbed. This way we can follow the object around if it moves
                grab.DummyTransform.parent = climbable.transform;
                grab.PreviousPosition = grab.DummyTransform.position;

                climbers.Add(grab);
            }
        }

        public void RemoveClimber(Grabber grab) {
            if (climbers.Contains(grab)) {
                // Reset grabbable parent
                grab.DummyTransform.parent = grab.transform;
                grab.DummyTransform.localPosition = Vector3.zero;

                climbers.Remove(grab);
            }
        }
    }
}
