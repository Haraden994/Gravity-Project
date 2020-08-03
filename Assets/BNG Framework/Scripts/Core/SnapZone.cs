using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {
    public class SnapZone : MonoBehaviour {

        /// <summary>
        /// If false, Item will Move back to inventory space if player drops it
        /// </summary>
        public bool CanDropItem = true;

        /// <summary>
        /// If false the swap zone cannot have it's content replaced.
        /// </summary>
        public bool CanSwapItem = true;

        /// <summary>
        /// Multiply Item Scale times this when in snap zone
        /// </summary>
        public float ScaleItem = 1f;

        public bool DisableColliders = true;
        List<Collider> disabledColliders;

        /// <summary>
        /// Only snap if Grabbable was dropped maximum of X seconds ago
        /// </summary>
        public float MaxDropTime = 0.1f;

        /// <summary>
        /// If not empty, can only snap objects if transform name contains one of these strings
        /// </summary>
        public List<string> OnlyAllowNames;

        /// <summary>
        /// Do not allow snapping if transform contains one of these names
        /// </summary>
        public List<string> ExcludeTransformNames;

        public AudioClip SoundOnSnap;
        public AudioClip SoundOnUnsnap;

        /// <summary>
        /// Optional Unity Event  to be called when something is snapped to this SnapZone. Passes in the Grabbable that was attached.
        /// </summary>
        public GrabbableEvent OnSnapEvent;

        /// <summary>
        /// Optional Unity Event to be called when something has been detached from this SnapZone. Passes in the Grabbable is being detattached.
        /// </summary>
        public GrabbableEvent OnDetachEvent;

        GrabbablesInTrigger gZone;

        public Grabbable HeldItem;
        Grabbable trackedItem; // If we can't drop the item, track it separately

        // Closest Grabbable in our trigger
        public Grabbable ClosestGrabbable;

        SnapZoneOffset offset;

        // Start is called before the first frame update
        void Start() {
            gZone = GetComponent<GrabbablesInTrigger>();

            // Auto Equip item
            if(HeldItem != null) {
                GrabGrabbable(HeldItem);
            }
        }

        // Update is called once per frame
        void Update() {

            ClosestGrabbable = getClosestGrabbable();

            // Can we grab something
            if (HeldItem == null && ClosestGrabbable != null) {
                float secondsSinceDrop = Time.time - ClosestGrabbable.LastDropTime;
                if (secondsSinceDrop < MaxDropTime) {
                    GrabGrabbable(ClosestGrabbable);
                }
            }

            // Keep snapped to us or drop
            if (HeldItem != null) {

                // Something picked this up or changed transform parent
                if (HeldItem.BeingHeld || HeldItem.transform.parent != transform) {
                    ReleaseAll();
                }
                else {
                    // We are holding this item
                    if(ScaleItem != 1f) {
                        float scaleTo = HeldItem.OriginalScale * ScaleItem;
                        HeldItem.transform.localScale = Vector3.Lerp(HeldItem.transform.localScale, new Vector3(scaleTo, scaleTo, scaleTo), Time.deltaTime * 30f);
                    }

                    // Make  sure this can't be grabbed from the snap zone
                    if(HeldItem.enabled || (disabledColliders[0] != null && disabledColliders[0].enabled)) {
                        disableGrabbable(HeldItem);
                    }

                    // Lock into place
                    if(offset) {
                        HeldItem.transform.localPosition = offset.LocalPositionOffset;
                        HeldItem.transform.localEulerAngles = offset.LocalRotationOffset;
                    }
                    else {
                        HeldItem.transform.localPosition = Vector3.zero;
                        HeldItem.transform.localEulerAngles = Vector3.zero;
                    }
                    
                }
            }

            // Can't drop item. Lerp to position if not being held
            if (!CanDropItem && trackedItem != null && HeldItem == null) {
                if (!trackedItem.BeingHeld) {
                    GrabGrabbable(trackedItem);
                }
            }
        }

        Grabbable getClosestGrabbable() {

            Grabbable closest = null;
            float lastDistance = 9999f;

            if (gZone == null || gZone.NearbyGrabbables == null) {
                return null;
            }

            foreach(var g in gZone.NearbyGrabbables) {

                // Collider may have been disabled
                if(g.Key == null) {
                    continue;
                }

                float dist = Vector3.Distance(transform.position, g.Value.transform.position);
                if(dist < lastDistance) {

                    //  Not allowing secondary grabbables such as slides
                    if(g.Value.OtherGrabbableMustBeGrabbed != null) {
                        continue;
                    }

                    // Don't allow SnapZones in SnapZones
                    if(g.Value.GetComponent<SnapZone>() != null) {
                        continue;
                    }

                    // Don't allow InvalidSnapObjects to snap
                    if (g.Value.CanBeSnappedToSnapZone == false) {
                        continue;
                    }

                    // Must contain transform name
                    if (OnlyAllowNames != null && OnlyAllowNames.Count > 0) {
                        string transformName = g.Value.transform.name;
                        bool matchFound = false;
                        foreach(var name in OnlyAllowNames) {
                            if(transformName.Contains(name)) {
                                matchFound = true;                                
                            }
                        }

                        // Not a valid match
                        if(!matchFound) {
                            continue;
                        }
                    }

                    // Check for name exclusion
                    if (ExcludeTransformNames != null) {
                        string transformName = g.Value.transform.name;
                        foreach (var name in ExcludeTransformNames) {
                            // Not a valid match
                            if (transformName.Contains(name)) {
                                continue;
                            }
                        }
                    }

                    // Only valid to snap if being held or recently dropped
                    if (g.Value.BeingHeld || (Time.time - g.Value.LastDropTime < MaxDropTime)) {
                        closest = g.Value;
                        lastDistance = dist;
                    }
                }
            }

            return closest;
        }

        public void GrabGrabbable(Grabbable grab) {

            // Grab is already in Snap Zone
            if(grab.transform.parent != null && grab.transform.parent.GetComponent<SnapZone>() != null) {
                return;
            }

            if(HeldItem != null) {
                ReleaseAll();
            }

            HeldItem = grab;

            // Is there an offset to apply?
            SnapZoneOffset off = grab.GetComponent<SnapZoneOffset>();
            if(off) {
                offset = off;
            }
            else {
                offset = grab.gameObject.AddComponent<SnapZoneOffset>();
                offset.LocalPositionOffset = Vector3.zero;
                offset.LocalRotationOffset = Vector3.zero;
            }

            // Disable the grabbable. This is picked up through a Grab Action
            disableGrabbable(grab);

            grab.transform.parent = transform;

            // Call event
            if (OnSnapEvent != null) {
                OnSnapEvent.Invoke(grab);
            }

            if (SoundOnSnap) {
                VRUtils.Instance.PlaySpatialClipAt(SoundOnSnap, transform.position, 0.75f);
            }
        }

        void disableGrabbable(Grabbable grab) {

            if (DisableColliders) {
                disabledColliders = grab.GetComponentsInChildren<Collider>(false).ToList();
                foreach (var c in disabledColliders) {
                    c.enabled = false;
                }
            }

            // Disable the grabbable. This is picked up through a Grab Action
            grab.enabled = false;
        }

        public void GrabEquipped(Grabber grabber) {

            if (grabber != null) {
                if(HeldItem) {
                    var g = HeldItem;
                    ReleaseAll();

                    // Position next to grabber if somewhat faraways
                    if(Vector3.Distance(g.transform.position, grabber.transform.position) > 0.2f) {
                        g.transform.position = grabber.transform.position;
                    }

                    // Do grab
                    grabber.GrabGrabbable(g);
                }
            }
        }

        /// <summary>
        /// Release  everything snapped to us
        /// </summary>
        public void ReleaseAll() {

            // No need to keep checking
            if (HeldItem == null) {
                return;
            }

            // Still need to keep track of item if we can't fully drop it
            if (!CanDropItem && HeldItem != null) {
                trackedItem = HeldItem;
            }

            HeldItem.ResetScale();

            if (DisableColliders && disabledColliders != null) {
                foreach (var c in disabledColliders) {
                    if(c) {
                        c.enabled = true;
                    }
                }
            }
            disabledColliders = null;

            HeldItem.enabled = true;
            HeldItem.transform.parent = null;

            // Play Unsnap sound
            if(HeldItem != null) {
                if (SoundOnSnap) {
                    VRUtils.Instance.PlaySpatialClipAt(SoundOnUnsnap, transform.position, 0.75f);
                }

                // Call event
                if (OnDetachEvent != null) {
                    OnDetachEvent.Invoke(HeldItem);
                }
            }

            HeldItem = null;
        }
    }
}
