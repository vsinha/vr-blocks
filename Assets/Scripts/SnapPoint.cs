using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class SnapPoint : MonoBehaviour {
    private bool didJustSnap; // becomes unset, used to protect against handling the OnTriggerEnter twice

    public bool isSnapped; // persistent

    public Snappable parentSnappable;
    public SphereCollider coll;

    private bool shouldSnapIfSnapEnableStateChanges;

    // Use this for initialization
    void Start () {
        coll = GetComponent<SphereCollider>();

        UpdateParentSnappableRef();

        if (parentSnappable == null) {
            Debug.LogError("SnapPoint is not the child of a snappable object");
        }
	}
	
	void LateUpdate () {
        didJustSnap = false;
	}

    private void OnTriggerEnter(Collider other)
    {

        // if we have our 'snap disabled' button down, then ignore
        if (!parentSnappable.IsSnapEnabled()) {
            // but if we now were to unpress the button, we'd want to snap immediately, so add the handler for it
            this.shouldSnapIfSnapEnableStateChanges = true;
            return;
        }

        SnapTo(other);
    }

    private void SnapTo(Collider other)
    {
        var otherSnapPoint = other.gameObject.GetComponent<SnapPoint>();

        if (otherSnapPoint == null) return;                                         // not a snap point
        if (this.isSnapped || otherSnapPoint.isSnapped) return;                     // connected already
        if (this.parentSnappable.GetInstanceID() == otherSnapPoint.parentSnappable.GetInstanceID()) return; // attached to the same object
        if (this.didJustSnap) return;


        if (this.parentSnappable.IsGrabbed()) { //&& !this.IsConnectedTo(otherSnapPoint)) {
            // we collided with another snap point

            // silence the other one
            otherSnapPoint.didJustSnap = true;
            otherSnapPoint.isSnapped = true;
            this.isSnapped = true;
            this.didJustSnap = true;

            // call our snap handler
            Debug.Log(this.parentSnappable.name + " snapping to " + otherSnapPoint.parentSnappable.name);
            this.parentSnappable.SnapPointCollision(this, otherSnapPoint);
        }
    }

    public void UpdateParentSnappableRef()
    {
        parentSnappable = this.transform.GetComponentInParent<Snappable>(); // grandparent
    }

    private void OnTriggerStay(Collider other)
    {
        // TODO make this a cylinder or something
        Debug.DrawLine(this.transform.position, other.transform.position, Color.red);

        if (shouldSnapIfSnapEnableStateChanges) {
            if (parentSnappable.IsSnapEnabled()) {
                SnapTo(other);
            }
        }

    }
}
