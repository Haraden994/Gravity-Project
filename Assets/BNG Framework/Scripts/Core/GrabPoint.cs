using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class GrabPoint : MonoBehaviour {

        /// <summary>
        /// Set to Default to inherit Grabbable's HandPose. Otherwise this HandPose will be used
        /// </summary>
        public HandPoseId HandPose;

        /// <summary>
        /// If specified, Hand Model will be placed here when snapped
        /// </summary>
        public Transform HandPosition;

        /// <summary>
        /// GrabPoint is not considered valid if the angle between the GrabPoint and Grabber is greater than this
        /// </summary>
        [Range(0.0f, 360.0f)]
        public float MaxDegreeDifferenceAllowed = 360;
    }
}