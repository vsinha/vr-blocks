using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapPoint : MonoBehaviour {
    private bool didJustSnap; // becomes unset, used to protect against handling the OnTriggerEnter twice

    public bool isSnapped; // persistent

    public Snappable parentSnappable;
    public SphereCollider coll;

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
        var otherSnapPoint = other.gameObject.GetComponent<SnapPoint>();

        if (otherSnapPoint == null) return;                                         // not a snap point
        if (this.isSnapped || otherSnapPoint.isSnapped) return;                     // connected already
        if (this.parentSnappable.GetInstanceID() == otherSnapPoint.parentSnappable.GetInstanceID()) return; // attached to the same object

        if (this.parentSnappable.IsGrabbed() && this.didJustSnap == false) { //&& !this.IsConnectedTo(otherSnapPoint)) {
            // we collided with another snap point
            Debug.Log("trigger enter (" + this.name + " " + this.transform.parent.name + "), (" + other.name + " " + other.transform.parent.name + ")");

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
    }
}
