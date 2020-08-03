using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

	/// <summary>
	/// This CharacterConstraint will keep the size the Character Capsule along with the camera if not colliding with anything
	/// </summary>
	public class CharacterConstraint : MonoBehaviour {

		/// <summary>
		/// Distance from the HMD's position. When the player is moving in legal space without collisions, this will be zero
		/// </summary>
		public float CurrentDistance;

		public float CapsuleClimbingHeight = 0.75f;
		public float CapsuleClimbingCenter = -0.125f;

		public float MinimumCapsuleHeight = 0.4f;
		public float MaximumCapsuleHeight = 3f;

		Action cameraUpdateAction;
		Action preCharacterMovementAction;
		OVRCameraRig cameraRig;
		OVRPlayerController playerController;
		BNGPlayerController bngController;
		CharacterController character;

		CharacterConstraint() {
			cameraUpdateAction = CameraUpdate;
			preCharacterMovementAction = PreCharacterMovement;
		}

		void Awake() {
			character = GetComponent<CharacterController>();
			playerController = GetComponent<OVRPlayerController>();
			cameraRig = GetComponentInChildren<OVRCameraRig>();
			bngController = transform.parent.GetComponent<BNGPlayerController>();
		}

		void OnEnable() {
			playerController.PreCharacterMove += preCharacterMovementAction;
		}

		void OnDisable() {
			playerController.CameraUpdated -= cameraUpdateAction;
		}

		private void Update() {
			CameraUpdate();
			checkClimbing();
		}

		void checkClimbing() {
			bool grounded = character.isGrounded;
			if (bngController != null && bngController.GrippingClimbable && !grounded) {
				character.height = CapsuleClimbingHeight;
				character.center = new Vector3(0, CapsuleClimbingCenter, 0);
			}
			else {
				character.center = new Vector3(0, -0.25f, 0);
			}			
		}

		private void CameraUpdate() {
			// Try to adjust the controller height to the height of the camera.
			var cameraHeight = playerController.CameraHeight;

			// If the new height is less than before, just accept the reduced height.
			if (cameraHeight <= character.height) {
				character.height = Mathf.Clamp(cameraHeight - character.skinWidth, MinimumCapsuleHeight, MaximumCapsuleHeight);
			}
			else {
				var bottom = character.transform.position;
				bottom += character.center;
				bottom.y -= character.height / 2.0f + character.radius;
				character.height = Mathf.Clamp(cameraHeight - character.skinWidth, MinimumCapsuleHeight, MaximumCapsuleHeight);
			}
		}
		
		void PreCharacterMovement() {
			if (playerController.Teleported) {
				return;
			}

			// First, determine if the lateral movement will collide with the scene geometry.
			var oldCameraPos = cameraRig.transform.position;
			var wpos = cameraRig.centerEyeAnchor.position;
			var delta = wpos - transform.position;
			delta.y = 0;
			var len = delta.magnitude;
			if (len > 0.0f) {
				character.Move(delta);
				var currentDelta = transform.position - wpos;
				currentDelta.y = 0;
				CurrentDistance = currentDelta.magnitude;
				cameraRig.transform.position = oldCameraPos;				
			}
			else {
				CurrentDistance = 0;
			}

			// Next, determine if the player camera is colliding with something above the player by doing a sphere test from the feet to the head.
			var bottom = transform.position;
			bottom += character.center;
			bottom.y -= character.height / 2.0f;

			var max = playerController.CameraHeight;
			if (Physics.SphereCast(bottom, character.radius, Vector3.up, out RaycastHit info, max, gameObject.layer, QueryTriggerInteraction.Ignore)) {
				// It hit something. Use the fade distance min/max to determine how much to fade.
				var dist = info.distance;
				dist = max - dist;
				if (dist > CurrentDistance) {
					CurrentDistance = dist;
				}
			}
		}
	}
}