using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Set the Rotation of a Transform based on a Bow's draw percentage
    /// </summary>
    public class BowArm : MonoBehaviour {

        public Bow BowItem;

        /// <summary>
        /// When to modify the rotation : 0-1;
        /// </summary>
        public float BowPercentStart = 50f;
        public float RotateDegrees = 10f; // How much past the initial rotation we should rotate

        public float Speed = 50f;

        private Quaternion _startRotation;
        private Quaternion _endRotation;


        // Start is called before the first frame update
        void Start() {
            _startRotation = Quaternion.Euler(transform.localEulerAngles);

            _endRotation = Quaternion.Euler(new Vector3(_startRotation.x + RotateDegrees, transform.localEulerAngles.y, transform.localEulerAngles.z));
        }

        // Update is called once per frame
        void Update() {

            if (BowItem.DrawPercent >= BowPercentStart) {
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, _endRotation, Speed * Time.deltaTime);
            }
            else if(BowItem.DrawPercent < BowPercentStart && BowItem.DrawPercent > 5) {
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, _startRotation, Speed * Time.deltaTime);
            }
            else {
                transform.localRotation = _startRotation;
            }
        }
    }
}

