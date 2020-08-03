using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    // This will rotate a transform along with a users headset. Useful for keeping an object aligned with the camera, independent of the player capsule collider.
    public class RotateWithHMD : MonoBehaviour {

        /// <summary>
        /// The Character Capsule to  rotate along with
        /// </summary>
        public CharacterController Character;

        /// <summary>
        /// Offset to apply in local space to the hmdTransform
        /// </summary>
        public Vector3 Offset = new Vector3(0, -0.25f, 0);

        public float RotateSpeed = 5f;
        public float MoveSpeed = 5f;

        Transform camTransform;

        void LateUpdate() {
            updateBodyPosition();
        }

        void updateBodyPosition() {

            if (camTransform == null) {
                camTransform = Camera.main.transform;
            }

            if (camTransform != null) {

                transform.position = camTransform.position;

                // Move position relative to Character Controller
                if (Character != null) {
                    //transform.rotation = Quaternion.Lerp(transform.rotation, Character.transform.rotation, Time.deltaTime * RotateSpeed);
                    transform.localPosition -= Character.transform.TransformVector(Offset);
                }

                if (camTransform != null) {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0.0f, Camera.main.transform.rotation.eulerAngles.y, 0.0f), Time.deltaTime * RotateSpeed);
                }
            }
        }
    }
}
