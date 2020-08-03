using OVRTouchSample;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {

    public enum GrabType {
        Snap,
        Precise
    }

    public enum HoldType {
        HoldDown,
        Toggle
    }

    public enum GrabPhysics {
        PhysicsJoint,
        Kinematic,
        None
    }

    public enum OtherGrabBehavior {
        None,
        SwapHands,
        DualGrab
    }

    /// <summary>
    /// An object that can be picked up by a Grabber
    /// </summary>
    public class Grabbable : MonoBehaviour {

        [HideInInspector]
        public bool BeingHeld = false;
        public bool IsGrabbable
        {
            get
            {
                return isGrabbable();
            }
        }
        List<Grabber> validGrabbers;        

        /// <summary>
        /// The grabber that is currently holding us. Null if not being held
        /// </summary>        
        protected List<Grabber> heldByGrabbers;

        /// <summary>
        /// Save whether or not the RigidBody was kinematic on Start.
        /// </summary>
        bool wasKinematic;
        bool usedGravity;
        CollisionDetectionMode initialCollisionMode;
        RigidbodyInterpolation initialInterpolationMode;

        /// <summary>
        /// Is the object being pulled towards the Grabber
        /// </summary>
        bool remoteGrabbing;

        /// <summary>
        /// Configure which button is used to initiate the grab
        /// </summary>
        [Tooltip("Configure which button is used to initiate the grab")]
        public GrabButton GrabButton = GrabButton.Grip;

        /// <summary>
        /// Should the Grab Button be held down, or can it be toggle on and off
        /// </summary>
        [Tooltip("Should the Grab Button be held down, or can it be toggle on and off")]
        public HoldType Grabtype = HoldType.HoldDown;

        /// <summary>
        /// Kinematic Physics locks the object in place on the hand / grabber. PhysicsJoint allows collisions with the environment.
        /// </summary>
        [Tooltip("Kinematic Physics locks the object in place on the hand / grabber. Physics Joint allows collisions with the environment.")]
        public GrabPhysics GrabPhysics = GrabPhysics.PhysicsJoint;

        /// <summary>
        /// Snap to a location or grab anywhere on the object
        /// </summary>
        [Tooltip("Snap to a location or grab anywhere on the object")]
        public GrabType GrabMechanic = GrabType.Snap;

        /// <summary>
        /// How fast to Lerp the object to the hand
        /// </summary>
        [Tooltip("How fast to Lerp the object to the hand")]
        public float GrabSpeed = 7.5f;

        /// <summary>
        /// Can the object be picked up from far away. Must be within RemoteGrabber Trigger
        /// </summary>
        [Tooltip("Can the object be picked up from far away. Must be within RemoteGrabber Trigger")]
        public bool RemoteGrabbable = false;

        /// <summary>
        /// Max Distance Object can be Remote Grabbed. Not applicable if RemoteGrabbable is false
        /// </summary>
        [Tooltip("Max Distance Object can be Remote Grabbed. Not applicable if RemoteGrabbable is false")]
        public float RemoteGrabDistance = 2f;

        /// <summary>
        /// Multiply controller's velocity times this when throwing
        /// </summary>
        [Tooltip("Multiply controller's velocity times this when throwing")]
        public float ThrowForceMultiplier = 2f;

        /// <summary>
        /// Multiply controller's angular velocity times this when throwing
        /// </summary>
        [Tooltip("Multiply controller's angular velocity times this when throwing")]
        public float ThrowForceMultiplierAngular = 1.5f; // Multiply Angular Velocity times this

        /// <summary>
        /// Drop the item if object's center travels this far from the Grabber's Center (in meters). Set to 0 to disable distance break.
        /// </summary>
        [Tooltip("Drop the item if object's center travels this far from the Grabber's Center (in meters). Set to 0 to disable distance break.")]
        public float BreakDistance = 1f;

        /// <summary>
        /// Enabling this will hide the Transform specified in the Grabber's HandGraphics property
        /// </summary>
        [Tooltip("Enabling this will hide the Transform specified in the Grabber's HandGraphics property")]
        public bool HideHandGraphics = false;

        /// <summary>
        ///  Parent this object to the hands for better stability.
        ///  Not recommended for child grabbers
        /// </summary>
        [Tooltip("Parent this object to the hands for better stability during movement (Recommended unless a child Grabbable)")]
        public bool ParentToHands = true;

        /// <summary>
        /// If true, the hand model will be attached to the grabbed object
        /// </summary>
        [Tooltip("If true, the hand model will be attached to the grabbed object. This separates it from a 1:1 match with the controller, but may look more realistic.")]
        public bool ParentHandModel = false;

        /// <summary>
        /// Animator ID of the Hand Pose to use
        /// </summary>
        [Tooltip("This HandPose Id will be passed to the Hand Animator when equipped. You can add new hand poses in the HandPoseDefinitions.cs file.")]
        public HandPoseId CustomHandPose = HandPoseId.Default;

        /// <summary>
        /// What to do if another grabber grabs this while equipped. DualGrab is currently unsupported.
        /// </summary>
        [Tooltip("What to do if another grabber grabs this while equipped. DualGrab is currently unsupported.")]
        public OtherGrabBehavior SecondaryGrabBehavior = OtherGrabBehavior.None;

        /// <summary>
        /// The Grabbable can only be grabbed if this grabbable is being held.
        /// Example : If you only want a weapon part to be grabbable if the weapon itself is being held.
        /// </summary>
        [Tooltip("The Grabbable can only be grabbed if this grabbable is being held. Example : If you only want a weapon part to be grabbable if the weapon itself is being held.")]
        public Grabbable OtherGrabbableMustBeGrabbed = null;
        /// <summary>
        /// How much Spring Force to apply to the joint when something comes in contact with the grabbable
        /// A higher Spring Force will make the Grabbable more rigid
        /// </summary>
        [Tooltip("A higher Spring Force will make the Grabbable more rigid")]
        public float CollisionSpring = 3000;

        /// <summary>
        /// How much Slerp Force to apply to the joint when something is in contact with the grabbable
        /// </summary>
        public float CollisionSlerp = 500;

        /// <summary>
        /// Set to false to disable dropping. If false, will be permanently attached to whatever grabs this.
        /// </summary>
        [Tooltip("Set to false to disable dropping. If false, will be permanently attached to whatever grabs this.")]
        public bool CanBeDropped = true;

        /// <summary>
        /// Can this object be snapped to snap zones? Set to false if you never want this to be snappable. Further filtering can be done on the SnapZones
        /// </summary>
        [Tooltip("Can this object be snapped to snap zones? Set to false if you never want this to be snappable. Further filtering can be done on the SnapZones")]
        public bool CanBeSnappedToSnapZone = true;

        /// <summary>
        /// Time in seconds (Time.time) when we last grabbed this item
        /// </summary>
        [HideInInspector]
        public float LastGrabTime;

        /// <summary>
        /// Time in seconds (Time.time) when we last dropped this item
        /// </summary>
        [HideInInspector]
        public float LastDropTime;

        /// <summary>
        /// Set to True to throw the Grabbable by applying the controller velocity to the grabbable on drop. 
        /// Set False if you don't want the object to be throwable, or want to apply your own force manually
        /// </summary>
        [HideInInspector]
        public bool AddControllerVelocityOnDrop = true;

        // Total distance between the Grabber and Grabbable.
        float journeyLength;

        public float OriginalScale { get; private set; }

        // Keep track of objects that are colliding with us
        List<Collider> collisions;

        // Last time in seconds (Time.time) since we had a valid collision
        float lastCollisionSeconds;

        // If Time.time < requestSpringTime, force joint to be springy
        float requestSpringTime;

        /// <summary>
        /// If Grab Mechanic is set to Snap, set position and rotation to this Transform on the primary Grabber
        /// </summary>
        Transform primaryGrabOffset;

        [Tooltip("Look at this object if it is being held. For example, a rifle may look at a Grabable Grip on the object.")]
        public Grabbable SecondaryGrabbable;

        [Tooltip("How quickly to Lerp towards the SecondaryGrabbable")]
        public float SecondHandLookSpeed = 40f;

        [HideInInspector]
        public Vector3 SecondaryLookOffset;

        [HideInInspector]
        public Transform SecondaryLookAtTransform;

        [HideInInspector]
        public Transform LocalOffsetTransform;

        Vector3 grabPosition {
            get {
                if(primaryGrabOffset != null) {
                    return primaryGrabOffset.position;
                }
                else {
                    return transform.position;
                }
            }
        }

        [HideInInspector]
        public Vector3 GrabPositionOffset {
            get {
                if (primaryGrabOffset) {
                    // Reverse X axis if on other hand
                    var grabbedBy = GetPrimaryGrabber();
                    if (MirrorOffsetForOtherHand && grabbedBy != null && grabbedBy.HandSide == ControllerHand.Left) {
                        return new Vector3(primaryGrabOffset.transform.localPosition.x * -1, primaryGrabOffset.transform.localPosition.y, primaryGrabOffset.transform.localPosition.z);
                    }
                    return primaryGrabOffset.transform.localPosition;
                }

                return Vector3.zero;
            }
        }

        [HideInInspector]
        public Vector3 GrabRotationOffset {
            get {
                if (primaryGrabOffset) {
                    var grabbedBy = GetPrimaryGrabber();
                    if (MirrorOffsetForOtherHand && grabbedBy != null && grabbedBy.HandSide == ControllerHand.Left) {
                        return new Vector3(primaryGrabOffset.transform.localEulerAngles.x, primaryGrabOffset.transform.localEulerAngles.y * -1, primaryGrabOffset.transform.localEulerAngles.z);
                    }
                    return primaryGrabOffset.transform.localEulerAngles;
                }
                return Vector3.zero;
            }
        }
       
        /// <summary>
        /// If true, the local X position of the Offset will be reverse when held in the left hand
        /// </summary>
        public bool MirrorOffsetForOtherHand = true;
        
        private Transform _grabTransform;
        
        // Position this on the grabber to get a precise location
        public Transform grabTransform
        {
            get
            {
                if(_grabTransform != null) {
                    return _grabTransform;
                }

                _grabTransform = new GameObject().transform;
                _grabTransform.parent = this.transform;
                _grabTransform.name = "Grab Transform";
                _grabTransform.localPosition = Vector3.zero;
                _grabTransform.hideFlags = HideFlags.HideInHierarchy;

                return _grabTransform;
            }
        }

        /// <summary>
        /// If Grab Mechanic is set to Snap, the closest GrabPoint will be used. Add a SnapPoint Component to a GrabPoint to specify custom hand poses and rotation.
        /// </summary>
        [Tooltip("If Grab Mechanic is set to Snap, the closest GrabPoint will be used. Add a SnapPoint Component to a GrabPoint to specify custom hand poses and rotation.")]
        public List<Transform> GrabPoints;

        Transform originalParent;
        InputBridge input;
        ConfigurableJoint connectedJoint;
        BNGPlayerController player;
        Collider col;
        Rigidbody rigid;
        Grabber flyingTo;

        protected List<GrabbableEvents> events;
        bool didParentHands = false;                

        void Awake() {
            col = GetComponent<Collider>();
            rigid = GetComponent<Rigidbody>();

            if(GameObject.FindGameObjectWithTag("Player")) {
                input = GameObject.FindGameObjectWithTag("Player").GetComponent<InputBridge>();
            }
            else {
                Debug.LogError("No InputBridge Found on Player GameObject. Make sure you have one Player with the 'Player' Tag and the InputBridge component");
            }
            
            events = GetComponents<GrabbableEvents>().ToList();
            collisions = new List<Collider>();

            // Try parent if no rigid found here
            if (rigid == null && transform.parent != null) {
                rigid = transform.parent.GetComponent<Rigidbody>();
            }

            // Store initial rigidbody properties so we can reset them later as needed
            if (rigid) {
                initialCollisionMode = rigid.collisionDetectionMode;
                initialInterpolationMode = rigid.interpolation;
                wasKinematic = rigid.isKinematic;
                usedGravity = rigid.useGravity;
            }
            
            // Store initial parent so we can reset later if needed
            UpdateOriginalParent(transform.parent);

            validGrabbers = new List<Grabber>();

            OriginalScale = transform.localScale.x;

            if(GameObject.FindGameObjectWithTag("Player")) {
                player = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<BNGPlayerController>();
            }
        }        

        void Update() {

            if(remoteGrabbing) {
                Vector3 remoteDestination = getRemotePosition(flyingTo);
                Quaternion remoteRotation = getRemoteRotation();

                
                float dist2 = Vector3.SqrMagnitude(remoteDestination - transform.position);
                float dist = Vector3.Distance(transform.position, remoteDestination);
                // reached destination, snap to final transform position
                if (dist < 0.01f) {
                    transform.position = remoteDestination;
                    transform.rotation = grabTransform.rotation;

                    if (flyingTo != null) {
                        flyingTo.GrabGrabbable(this);
                    }
                }
                // Getting close so speed up
                else if (dist < 0.05f) {
                    transform.position = Vector3.Lerp(transform.position, remoteDestination, Time.deltaTime * GrabSpeed * 2);
                    transform.rotation = Quaternion.Lerp(transform.rotation, remoteRotation, Time.deltaTime * GrabSpeed * 2);
                }
                // Normal Lerp
                else {
                    transform.position = Vector3.Lerp(transform.position, remoteDestination, Time.deltaTime * GrabSpeed);
                    transform.rotation = Quaternion.Lerp(transform.rotation, remoteRotation, Time.deltaTime * GrabSpeed);
                }
            }

            if (BeingHeld) {
                // Something happened to our Grabber. Drop the item
                if (heldByGrabbers == null) {
                    DropItem(null, true, true);
                    return;
                }

                // Make sure all collisions are valid
                filterCollisions();

                if(collisions != null && collisions.Count > 0) {
                    lastCollisionSeconds = Time.time;
                }

                for (int x = 0; x < heldByGrabbers.Count; x++) {
                    Grabber g = heldByGrabbers[x];

                    // Should we drop the item if it's too far away?
                    if (BreakDistance > 0 && Vector3.Distance(grabPosition, g.transform.position) > BreakDistance) {
                        DropItem(g, true, true);
                        break;
                    }

                    // Should we drop the item if no longer holding the required Grabbable?
                    if(OtherGrabbableMustBeGrabbed != null && !OtherGrabbableMustBeGrabbed.BeingHeld) {
                        // Fixed joints work ok. Configurable Joints have issues
                        if(GetComponent<ConfigurableJoint>() != null) {
                            DropItem(g, true, true);
                            break;
                        }
                    }

                    // Fire off any relevant events
                    callEvents(g);
                }

                // Rotate the grabber to look at our secondary object
                checkSecondaryLook();
            }            
        }
        
        void FixedUpdate() {
            if(BeingHeld) {
                updateJoints(GetPrimaryGrabber());
            }
        }        

        /// <summary>
        /// Is this object able to be grabbed. Does not check for valid Grabbers, only if it isn't being held, is active, etc.
        /// </summary>
        /// <returns></returns>
        public bool IsValidGrabbable() {

            // Not valid if not active
            if (!isActiveAndEnabled) {
                return false;
            }

            // Not valid if being held and the object has no secondary grab behavior
            if(BeingHeld == true && SecondaryGrabBehavior == OtherGrabBehavior.None) {
                return false;
            }

            // Make sure grabbed conditions are met
            if (OtherGrabbableMustBeGrabbed != null && !OtherGrabbableMustBeGrabbed.BeingHeld) {
                return false;
            }

            return true;
        }

        void updateJoints(Grabber g) {

            if (GrabPhysics == GrabPhysics.PhysicsJoint) {

                // Set to continuous dynamic while being held
                if(rigid.isKinematic) {
                    rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
                }
                else {
                    rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
                
                rigid.interpolation = RigidbodyInterpolation.Interpolate;

                // Update Joint poisition in real time
                if (GrabMechanic == GrabType.Snap) {
                    connectedJoint.anchor = Vector3.zero;
                    connectedJoint.connectedAnchor = GrabPositionOffset;
                }

                // Check if something is requesting a springy joint
                // For example, a gun may wish to not lock the joint in order to apply recoil to a weapon via AddForce
                bool forceSpring = Time.time < requestSpringTime;

                // Nothing touching it so we can stick to hand rigidly
                if (collisions.Count == 0 && !forceSpring) {
                    // Lock Angular, XYZ Motion
                    // Make joint very rigid
                    connectedJoint.rotationDriveMode = RotationDriveMode.XYAndZ;

                    connectedJoint.xMotion = ConfigurableJointMotion.Locked;
                    connectedJoint.yMotion = ConfigurableJointMotion.Locked;
                    connectedJoint.zMotion = ConfigurableJointMotion.Locked;
                    connectedJoint.angularXMotion = ConfigurableJointMotion.Limited;
                    connectedJoint.angularYMotion = ConfigurableJointMotion.Limited;
                    connectedJoint.angularZMotion = ConfigurableJointMotion.Limited;

                    //SoftJointLimitSpring sp = connectedJoint.linearLimitSpring;
                    //sp.spring = 0;
                    //sp.damper = 5;

                    // Stiff if we're holding it
                    JointDrive xDrive = connectedJoint.xDrive;
                    xDrive.positionSpring = 9001;

                    JointDrive slerpDrive = connectedJoint.slerpDrive;
                    slerpDrive.positionSpring = 1000;

                    bool recentCollision = Time.time - lastCollisionSeconds <= 0.1f;
                    bool playerRecentlyMoved = false;
                    if(player != null) {
                        playerRecentlyMoved = player.RecentlyMoved;
                    }

                    // Set parent to us to keep movement smoothed
                    if (ParentToHands && playerRecentlyMoved && g != null && !recentCollision) {
                        transform.parent = g.transform;
                    }
                    else if(ParentToHands && !playerRecentlyMoved) {
                        transform.parent = null;
                    }
                }
                else {
                    // Make Springy
                    connectedJoint.rotationDriveMode = RotationDriveMode.Slerp;
                    connectedJoint.xMotion = ConfigurableJointMotion.Free;
                    connectedJoint.yMotion = ConfigurableJointMotion.Free;
                    connectedJoint.zMotion = ConfigurableJointMotion.Free;
                    connectedJoint.angularXMotion = ConfigurableJointMotion.Free;
                    connectedJoint.angularYMotion = ConfigurableJointMotion.Free;
                    connectedJoint.angularZMotion = ConfigurableJointMotion.Free;

                    //SoftJointLimitSpring sp = connectedJoint.linearLimitSpring;
                    //sp.spring = 5000;
                    //sp.damper = 5;

                    JointDrive xDrive = connectedJoint.xDrive;
                    xDrive.positionSpring = CollisionSpring;
                    //connectedJoint.xDrive = xDrive;

                    JointDrive slerpDrive = connectedJoint.slerpDrive;
                    slerpDrive.positionSpring = CollisionSlerp;
                    //connectedJoint.slerpDrive = slerpDrive;

                    // No parent if we are in contact with something. Player movement should be independent
                    if (ParentToHands) {
                        transform.parent = null;
                    }
                }
            }

            // Snap / Lerp to center or precise position on object
            if (GrabPhysics == GrabPhysics.Kinematic) {
                // Distance moved equals elapsed time times speed.
                float distCovered = (Time.time - LastGrabTime) * GrabSpeed;

                // Fraction of journey completed equals current distance divided by total distance.
                float fractionOfJourney = distCovered / journeyLength;

                if (GrabMechanic == GrabType.Snap) {
                    // Set our position as a fraction of the distance between the markers.

                    // Update local transform in real time
                    
                    if (g != null) {
                        transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero - GrabPositionOffset, fractionOfJourney);  
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, grabTransform.localRotation, Time.deltaTime * 10);
                    }
                    else {
                        transform.position = Vector3.Lerp(transform.position, GrabPositionOffset, fractionOfJourney);
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, grabTransform.localRotation, Time.deltaTime * 10);
                    }
                }
                else if (GrabMechanic == GrabType.Precise) {
                    transform.position = grabTransform.position;
                    transform.eulerAngles = grabTransform.eulerAngles;
                }
            }

            if (ParentHandModel && !didParentHands) {

                if (GrabMechanic == GrabType.Precise) {
                    parentHandGraphics(g);
                }
                else {
                    float distance = Vector3.Distance(grabPosition, getRemotePosition(g));

                    // Left hand needs some minor adjustments for X offsets
                    if(g.HandSide == ControllerHand.Left && grabTransform != null) {
                        Vector3 pos = grabPosition;
                        pos -= new Vector3(-grabTransform.localPosition.x * 2, 0, 0);
                        distance = Vector3.Distance(pos, getRemotePosition(g));
                    }

                    if (distance <= 0.001f) {
                        // Snap position
                        parentHandGraphics(g);
                    }
                }
            }
        }
                
        void checkSecondaryLook() {

            // Create transform to look at if we are looking at a precise grab
            if(SecondaryGrabbable != null && SecondaryGrabbable.BeingHeld) {
                if(SecondaryLookAtTransform == null) {
                    Grabber thisGrabber = GetPrimaryGrabber();
                    Grabber secondaryGrabber = SecondaryGrabbable.GetPrimaryGrabber();

                    GameObject o = new GameObject();
                    SecondaryLookAtTransform = o.transform;
                    SecondaryLookAtTransform.name = "LookAtTransformTemp";
                    // Precise grab can use current grabber position
                    if(SecondaryGrabbable.GrabMechanic == GrabType.Precise) {
                        SecondaryLookAtTransform.position = secondaryGrabber.transform.position;
                    }
                    // Otherwise use snap point
                    else {
                        Transform grabPoint = SecondaryGrabbable.GetClosestGrabPoint(secondaryGrabber);
                        if (grabPoint) {
                            SecondaryLookAtTransform.position = grabPoint.position;
                        }
                        else {
                            SecondaryLookAtTransform.position = SecondaryGrabbable.transform.position;
                        }

                        SecondaryLookAtTransform.position = SecondaryGrabbable.transform.position;
                    }

                    if(SecondaryLookAtTransform && thisGrabber) {
                        SecondaryLookAtTransform.parent = thisGrabber.transform;
                        SecondaryLookAtTransform.localEulerAngles = Vector3.zero;
                        SecondaryLookAtTransform.localPosition = new Vector3(0, 0, SecondaryLookAtTransform.localPosition.z);

                        // Move parent back to grabber
                        SecondaryLookAtTransform.parent = secondaryGrabber.transform;
                    }
                }
            }

            // We should not be aiming at anything if a Grabbable was specified
            if(SecondaryGrabbable != null && !SecondaryGrabbable.BeingHeld && SecondaryLookAtTransform != null) {
                clearLookAtTransform();
            }

            Grabber heldBy = GetPrimaryGrabber();
            if(heldBy) {
                Transform grabberTransform = heldBy.transform;

                if (SecondaryLookAtTransform != null) {

                    // Can use this to offset look
                    //var rotForwardToDown = Quaternion.FromToRotation(Vector3.forward, -Vector3.up);

                    Vector3 initialRotation = grabberTransform.localEulerAngles;

                    Quaternion dest = Quaternion.LookRotation(SecondaryLookAtTransform.position - grabberTransform.position, Vector3.up);
                    grabberTransform.rotation = Quaternion.Slerp(grabberTransform.rotation, dest, Time.deltaTime * SecondHandLookSpeed);

                    // Exclude rotations to only x and y
                    grabberTransform.localEulerAngles = new Vector3(grabberTransform.localEulerAngles.x, grabberTransform.localEulerAngles.y, initialRotation.z);
                }
                else {
                    grabberTransform.localEulerAngles = angleLerp(grabberTransform.localEulerAngles, GrabRotationOffset, Time.deltaTime * 20);
                }
            }            
        }        

        public virtual void GrabItem(Grabber grabbedBy) {

            // Make sure we release this item
            if(BeingHeld && SecondaryGrabBehavior != OtherGrabBehavior.DualGrab) {
                DropItem(false, true);
            }            

            // Make sure all values are reset first
            ResetGrabbing();

            // Officially being held
            BeingHeld = true;
            LastGrabTime = Time.time;

            // Set where the item will move to on the grabber
            primaryGrabOffset = GetClosestGrabPoint(grabbedBy);

            // Update held by properties
            addGrabber(grabbedBy);
            grabTransform.parent = grabbedBy.transform;            

            grabbedBy.transform.localEulerAngles = GrabRotationOffset;                        

            // Hide the hand graphics if necessary
            if (HideHandGraphics) {
                grabbedBy.HideHandGraphics();
            }

            // Use center of grabber if snapping
            if (GrabMechanic == GrabType.Snap) {
                grabTransform.localEulerAngles = Vector3.zero;
                grabTransform.localPosition = GrabPositionOffset;
            }
            // Precision hold can use position of what we're grabbing
            else if (GrabMechanic == GrabType.Precise) {
                grabTransform.position = transform.position;
                grabTransform.rotation = transform.rotation;
            }

            // First remove any connected joints if necessary
            var projectile = GetComponent<Projectile>();
            if (projectile) {
                var fj = GetComponent<FixedJoint>();
                if (fj) {
                    Destroy(fj);
                }
            }

            // Set up the new connected joint
            if (GrabPhysics == GrabPhysics.PhysicsJoint && GrabMechanic == GrabType.Precise) {
                connectedJoint = grabbedBy.GetComponent<ConfigurableJoint>();
                connectedJoint.connectedBody = rigid;
                // Just let the autoconfigure handle the calculations for us
                connectedJoint.autoConfigureConnectedAnchor = true;
            }

            // Set up the connected joint for snapping
            if (GrabPhysics == GrabPhysics.PhysicsJoint && GrabMechanic == GrabType.Snap) {

                // Need to Fix Rotation on Snap Physics when close by
                transform.rotation = grabTransform.rotation;


                // Setup joint
                setupConfigJoint(grabbedBy);
            }

            if (GrabPhysics == GrabPhysics.Kinematic) {

                if (ParentToHands) {
                    transform.parent = grabbedBy.transform;
                }

                if (rigid != null) {
                    rigid.isKinematic = true;
                }
            }

            // Let events know we were grabbed
            for (int x = 0; x < events.Count; x++) {
                events[x].OnGrab(grabbedBy);
            }
            
            journeyLength = Vector3.Distance(grabPosition, grabbedBy.transform.position);
        }
       
        public virtual void GrabRemoteItem(Grabber grabbedBy) {
            flyingTo = grabbedBy;
            grabTransform.parent = grabbedBy.transform;
            grabTransform.localEulerAngles = Vector3.zero;
            grabTransform.localPosition = Vector3.zero - GrabPositionOffset;

            grabTransform.transform.localEulerAngles = GrabRotationOffset;

            if (rigid) {
                rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rigid.isKinematic = true;
            }

            remoteGrabbing = true;
        }

        public void ResetGrabbing() {
            if (rigid) {
                rigid.isKinematic = wasKinematic;
            }

            flyingTo = null;

            remoteGrabbing = false;

            collisions = new List<Collider>();
        }

        public virtual void DropItem(Grabber droppedBy, bool resetVelocity, bool resetParent) {

            // Nothing holding us
            if(heldByGrabbers == null) {
                BeingHeld = false;
                return;
            }

            if (resetParent) {
                ResetParent();
            }

            //disconnect all joints and set the connected object to null
            removeConfigJoint();

            //  If something called drop on this item we want to make sure the parent knows about it
            // Reset's Grabber position, grabbable state, etc.
            if (droppedBy) {
                droppedBy.DidDrop();
            }

            // Release item and apply physics force to it
            if (rigid != null) {
                rigid.isKinematic = wasKinematic;
                rigid.useGravity = usedGravity;
                rigid.interpolation = initialInterpolationMode;
                rigid.collisionDetectionMode = initialCollisionMode;
            }

            if (events != null) {
                for (int x = 0; x < events.Count; x++) {
                    events[x].OnRelease();
                }
            }

            // No longer have a primary Grab Offset set
            primaryGrabOffset = null;

            // No longer looking at a 2h object
            clearLookAtTransform();

            removeGrabber(droppedBy);

            BeingHeld = false;
            didParentHands = false;
            LastDropTime = Time.time;

            // Apply velocity last
            if (rigid && resetVelocity && droppedBy && AddControllerVelocityOnDrop) {
                // Make sure velocity is passed on
                Vector3 velocity = droppedBy.GetGrabberAveragedVelocity() + droppedBy.GetComponent<Rigidbody>().velocity;
                Vector3 angularVelocity = droppedBy.GetGrabberAveragedAngularVelocity() + droppedBy.GetComponent<Rigidbody>().angularVelocity;
                
                // Oculus Quest Angular Velocity is backwards
                if (input.IsOculusQuest()) {
                    angularVelocity = -angularVelocity;
                }

                if(gameObject.activeSelf) {
                    StartCoroutine(Release(velocity, angularVelocity));
                }
            }
        }

        Vector3 angleLerp(Vector3 startAngle, Vector3 finishAngle, float t) {
            float xLerp = Mathf.LerpAngle(startAngle.x, finishAngle.x, t);
            float yLerp = Mathf.LerpAngle(startAngle.y, finishAngle.y, t);
            float zLerp = Mathf.LerpAngle(startAngle.z, finishAngle.z, t);
            Vector3 Lerped = new Vector3(xLerp, yLerp, zLerp);
            return Lerped;
        }

        void clearLookAtTransform() {
            if (SecondaryLookAtTransform != null && SecondaryLookAtTransform.transform.name == "LookAtTransformTemp") {
                GameObject.Destroy(SecondaryLookAtTransform.gameObject);
            }

            SecondaryLookAtTransform = null;
        }

        void callEvents(Grabber g) {
            if (events.Any()) {
                ControllerHand hand = g.HandSide;

                // Right Hand Controls
                if (hand == ControllerHand.Right) {
                    foreach (var e in events) {
                        e.OnGrip(input.RightGrip);
                        e.OnTrigger(input.RightTrigger);

                        if (input.RightTriggerDown) {
                            e.OnTriggerDown();
                        }
                        if (input.AButton) {
                            e.OnButton1();
                        }
                        if (input.AButtonDown) {
                            e.OnButton1Down();
                        }
                        if (input.BButton) {
                            e.OnButton2();
                        }
                        if (input.BButtonDown) {
                            e.OnButton2Down();
                        }
                    }
                }

                // Left Hand Controls
                if (hand == ControllerHand.Left) {
                    foreach (var e in events) {
                        e.OnGrip(input.LeftGrip);
                        e.OnTrigger(input.LeftTrigger);

                        if (input.LeftTriggerDown) {
                            e.OnTriggerDown();
                        }
                        if (input.XButton) {
                            e.OnButton1();
                        }
                        if (input.XButtonDown) {
                            e.OnButton1Down();
                        }
                        if (input.YButton) {
                            e.OnButton2();
                        }
                        if (input.YButtonDown) {
                            e.OnButton2Down();
                        }
                    }
                }
            }
        }       

        public virtual void DropItem(Grabber droppedBy) {
            DropItem(droppedBy, true, true);
        }

        public virtual void DropItem(bool resetVelocity, bool resetParent) {
            DropItem(GetPrimaryGrabber(), resetVelocity, resetParent);
        }

        public void ResetScale() {
            transform.localScale = new Vector3(OriginalScale, OriginalScale, OriginalScale);
        }

        public void ResetParent() {
            transform.parent = originalParent;
        }

        public void UpdateOriginalParent(Transform newOriginalParent) {
            originalParent = newOriginalParent;
        }

        public void UpdateOriginalParent() {
            UpdateOriginalParent(transform.parent);
        }

        public ControllerHand GetControllerHand(Grabber g) {
            if(g != null) {
                return g.HandSide;
            }

            return ControllerHand.None;
        }
        
        /// <summary>
        /// Returns the Grabber that first grabbed this item. Return null if not being held.
        /// </summary>
        /// <returns></returns>
        public Grabber GetPrimaryGrabber() {
            if(heldByGrabbers != null) {
                foreach(var g in heldByGrabbers) {
                    if(g != null && g.HeldGrabbable == this) {
                        return g;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get the closest valid grabber. 
        /// </summary>
        /// <returns>Returns null if no valid Grabbers in range</returns>
        public Grabber GetClosestGrabber() {

            Grabber closestGrabber = null;
            float lastDistance = 9999;

            if (validGrabbers != null) {

                for (int x = 0; x < validGrabbers.Count; x++) {
                    Grabber g = validGrabbers[x];
                    if (g != null) {
                        float dist = Vector3.Distance(grabPosition, g.transform.position);
                        if(dist < lastDistance) {
                            closestGrabber = g;
                        }
                    }
                }
            }

            return closestGrabber;
        }

        public Transform GetClosestGrabPoint(Grabber grabber) {
            Transform grabPoint = null;
            float lastDistance = 9999;
            if(GrabPoints != null) {
                foreach (var g in GrabPoints) {

                    // Transform may have been destroyed
                    if(g == null) {
                        continue;
                    }

                    // Check for GrabPoint component that may override some values
                    GrabPoint gp = g.GetComponent<GrabPoint>();
                    if(gp) {
                        float currentAngle = Quaternion.Angle(grabber.transform.rotation, g.transform.rotation);
                        if (currentAngle > gp.MaxDegreeDifferenceAllowed) {
                            continue;
                        }
                    }

                    float thisDist = Vector3.Distance(g.transform.position, grabber.transform.position);
                    if (thisDist < lastDistance) {
                        grabPoint = g;
                        lastDistance = thisDist;
                    }
                }
            }

            return grabPoint;
        }

        IEnumerator Release (Vector3 velocity, Vector3 angularVelocity) {

            rigid.velocity = velocity * ThrowForceMultiplier;
            rigid.angularVelocity = angularVelocity;

            yield return new WaitForFixedUpdate();

            rigid.velocity = velocity * ThrowForceMultiplier;
            rigid.angularVelocity = angularVelocity;
        }

        public virtual bool IsValidCollision(Collision collision) {

            // Ignore Projectiles from grabbable collision
            // This way our grabbable stays rigid when projectils come in contact
            string transformName = collision.transform.name;
            if (transformName.Contains("Projectile") || transformName.Contains("Bullet") || transformName.Contains("Clip")) {
                return false;
            }

            // Ignore Character Joints as these cause jittery issues
            if (transformName.Contains("Joint")) {
                return false;
            }

            // Ignore Character Controllers
            CharacterController cc = collision.gameObject.GetComponent<CharacterController>();
            if (cc) {
                Physics.IgnoreCollision(col, cc,  true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Is Object able to be grabbed right now. Must have a valid grabber or remotegrabber
        /// </summary>
        /// <returns></returns>
        bool isGrabbable() {
            return IsValidGrabbable() && validGrabbers.Count > 0;
        }

        void parentHandGraphics(Grabber g) {
            if (g.HandsGraphics != null) {
                // Set to specified Grab Transform
                if(primaryGrabOffset != null) {
                    g.HandsGraphics.transform.parent = primaryGrabOffset;
                }
                else {
                    g.HandsGraphics.transform.parent = transform;
                }
                
                didParentHands = true;
            }
        }

        Vector3 getHandOffset() {

            if (primaryGrabOffset) {
                // Reverse X axis if on other hand
                var grabbedBy = GetPrimaryGrabber();
                if (MirrorOffsetForOtherHand && grabbedBy != null && grabbedBy.HandSide == ControllerHand.Left) {
                    return new Vector3(primaryGrabOffset.transform.localPosition.x * -1, primaryGrabOffset.transform.localPosition.y, primaryGrabOffset.transform.localPosition.z);
                }
                return primaryGrabOffset.transform.localPosition;
            }

            return Vector3.zero;
        }

        void setupConfigJoint(Grabber g) {
            connectedJoint = g.GetComponent<ConfigurableJoint>();
            connectedJoint.autoConfigureConnectedAnchor = false;
            connectedJoint.connectedBody = rigid;

            float anchorOffsetVal = (1 / g.transform.localScale.x) * -1;
            connectedJoint.anchor = Vector3.zero;

            connectedJoint.connectedAnchor = GrabPositionOffset;
        }

        void removeConfigJoint() {
            if (connectedJoint != null) {
                connectedJoint.anchor = Vector3.zero;
                connectedJoint.connectedBody = null;
            }
        }

        void addGrabber(Grabber g) {
            if (heldByGrabbers == null) {
                heldByGrabbers = new List<Grabber>();
            }

            if (!heldByGrabbers.Contains(g)) {
                heldByGrabbers.Add(g);
            }
        }

        void removeGrabber(Grabber g) {
            if (heldByGrabbers == null) {
                heldByGrabbers = new List<Grabber>();
            }
            else if (heldByGrabbers.Contains(g)) {
                heldByGrabbers.Remove(g);
            }

            Grabber removeGrabber = null;
            // Clean up any other latent grabbers
            foreach (var grab in heldByGrabbers) {
                if (grab.HeldGrabbable == null || grab.HeldGrabbable != this) {
                    removeGrabber = grab;
                }
            }

            if (removeGrabber) {
                heldByGrabbers.Remove(removeGrabber);
            }
        }

        Vector3 getRemotePosition(Grabber toGrabber) {

            if (toGrabber != null) {
                Transform pointPosition = GetClosestGrabPoint(toGrabber);
                if (pointPosition) {
                    Vector3 originalPos = grabTransform.position;
                    Vector3 offsetPosition = pointPosition.localPosition;
                    if(toGrabber.HandSide == ControllerHand.Left && MirrorOffsetForOtherHand) {
                        offsetPosition = new Vector3(-offsetPosition.x, offsetPosition.y, offsetPosition.z);
                    }

                    grabTransform.localPosition -= offsetPosition;
                    Vector3 result = grabTransform.position;

                    grabTransform.position = originalPos;

                    return result;
                }
            }

            return grabTransform.position;
        }

        Quaternion getRemoteRotation() {

            if (flyingTo != null) {
                Transform point = GetClosestGrabPoint(flyingTo);
                if (point) {
                    Quaternion originalRot = grabTransform.rotation;
                    grabTransform.localRotation *= point.localRotation;
                    Quaternion result = grabTransform.rotation;

                    grabTransform.rotation = originalRot;

                    return result;
                }
            }

            return grabTransform.rotation;
        }

        void filterCollisions() {
            for (int x = 0; x < collisions.Count; x++) {
                if (collisions[x] == null || !collisions[x].enabled || !collisions[x].gameObject.activeSelf) {
                    collisions.Remove(collisions[x]);
                    break;
                }
            }
        }

        public void AddValidGrabber(Grabber grabber) {

            if (validGrabbers == null) {
                validGrabbers = new List<Grabber>();
            }

            if (!validGrabbers.Contains(grabber)) {
                validGrabbers.Add(grabber);
            }
        }

        public void RemoveValidGrabber(Grabber grabber) {
            if (validGrabbers != null && validGrabbers.Contains(grabber)) {
                validGrabbers.Remove(grabber);
            }
        }

        private void OnCollisionEnter(Collision collision) {
            // Keep track of how many objects we are colliding with
            if(BeingHeld && IsValidCollision(collision) && !collisions.Contains(collision.collider)) {
                collisions.Add(collision.collider);
            }
        }

        private void OnCollisionExit(Collision collision) {
            if (BeingHeld && IsValidCollision(collision) && collisions.Contains(collision.collider)) {
                collisions.Remove(collision.collider);
            }
        }

        void OnDrawGizmosSelected() {
            // Show Grip Point
            Gizmos.color = Color.green;

            if(GrabPoints != null && GrabPoints.Count > 0) {
                foreach (var p in GrabPoints) {
                    if(p != null) {
                        Gizmos.DrawSphere(p.position, 0.02f);
                    }
                }
            }
            else {
                Gizmos.DrawSphere(transform.position, 0.02f);
            }
        }     
    }
}