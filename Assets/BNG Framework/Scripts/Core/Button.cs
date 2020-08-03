using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    /// <summary>
    /// Physical button helper with 
    /// </summary>
    public class Button : MonoBehaviour {

        public float MinLocalY = 0.25f;
        public float MaxLocalY = 0.55f;
        public float ClickTolerance = 0.01f; // Amount from Min / Max to be considered a click

        List<Grabber> grabbers; // Grabbers in our trigger
        SpringJoint joint;

        bool clickingDown = false;
        public AudioClip ButtonClick;
        public AudioClip ButtonClickUp;

        public UnityEvent onButtonDown;
        public UnityEvent onButtonUp;

        AudioSource audioSource;

        // Start is called before the first frame update
        void Start() {
            grabbers = new List<Grabber>();
            joint = GetComponent<SpringJoint>();

            // Start with button up top / popped up
            transform.localPosition = new Vector3(transform.localPosition.x, MaxLocalY, transform.localPosition.z);

            audioSource = GetComponent<AudioSource>();
        }

        // These have been hard coded for hand speed
        float ButtonSpeed = 15f;
        float SpringForce = 1500f;

        void Update() {

            Vector3 buttonDownPosition = new Vector3(transform.localPosition.x, MinLocalY, transform.localPosition.z);
            Vector3 buttonUpPosition = new Vector3(transform.localPosition.x, MaxLocalY, transform.localPosition.z);
            bool grabberInButton = false;

            // Find a valid grabber to push down
            foreach(var g in grabbers) {
                if(!g.HoldingItem) {
                    grabberInButton = true;
                    break;
                }
            }

            if (grabberInButton) {
                float speed = ButtonSpeed; //;framesInGrabber < 3 ? 5f : ButtonSpeed;
                transform.localPosition = Vector3.Lerp(transform.localPosition, buttonDownPosition, speed * Time.deltaTime);
                joint.spring = 0;

            }
            else {
                joint.spring = SpringForce;
            }

            // Cap values
            if (transform.localPosition.y < MinLocalY) {
                transform.localPosition = buttonDownPosition;
            }
            else if (transform.localPosition.y > MaxLocalY) {
                transform.localPosition = buttonUpPosition;
            }

            // Click Down?
            float buttonDownDistance = transform.localPosition.y - buttonDownPosition.y;
            if (buttonDownDistance <= ClickTolerance && !clickingDown) {
                clickingDown = true;
                OnButtonDown();
            }
            // Click Up?
            float buttonUpDistance = buttonUpPosition.y - transform.localPosition.y;
            if (buttonUpDistance <= ClickTolerance && clickingDown) {
                clickingDown = false;
                OnButtonUp();
            }
        }

        // Callback for ButtonDown
        public virtual void OnButtonDown() {

            // Play sound
            if (audioSource && ButtonClick) {
                audioSource.clip = ButtonClick;
                audioSource.Play();
            }

            // Call event
            if (onButtonDown != null) {
                onButtonDown.Invoke();
            }
        }

        // Callback for ButtonDown
        public virtual void OnButtonUp() {
            // Play sound
            if (audioSource && ButtonClickUp) {
                audioSource.clip = ButtonClickUp;
                audioSource.Play();
            }

            // Call event
            if (onButtonUp != null) {
                onButtonUp.Invoke();
            }
        }

        void OnTriggerEnter(Collider other) {

            Grabber grab = other.GetComponent<Grabber>();
            if (grab != null) {
                if (grabbers == null) {
                    grabbers = new List<Grabber>();
                }

                if (!grabbers.Contains(grab)) {
                    grabbers.Add(grab);
                }
            }
        }

        void OnTriggerExit(Collider other) {
            Grabber grab = other.GetComponent<Grabber>();
            if (grab != null) {
                if (grabbers.Contains(grab)) {
                    grabbers.Remove(grab);
                }
            }
        }
    }
}
