using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapPoint : MonoBehaviour {
    private bool didJustSnap; // becomes unset, used to protect against handling the OnTriggerEnter twice

    public bool isSnapped; // persistent

    public Snappable parent;
    public SphereCollider coll;

    // Use this for initialization
    void Start () {
        coll = GetComponent<SphereCollider>();

        parent = this.transform.parent.GetComponent<Snappable>();
        if (parent == null) {
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
        if (this.parent.GetInstanceID() == otherSnapPoint.GetInstanceID()) return;  // attached to the same object

        if (this.didJustSnap == false && !this.IsConnectedTo(other)) {
            // we collided with another snap point

            Debug.Log("trigger enter (" + this.name + " " + this.transform.parent.name + "), (" + other.name + " " + other.transform.parent.name + ")");

            // silence the other one
            otherSnapPoint.didJustSnap = true;
            this.isSnapped = true;
            otherSnapPoint.isSnapped = true;

            // call our snap handler
            this.transform.parent.GetComponent<Snappable>().SnapPointCollision(this, otherSnapPoint);
        }
    }

    private bool IsConnectedTo(Collider other)
    {
        return this.transform.parent.GetComponent<Snappable>().IsConnectedTo(other.transform.parent.GetComponent<Snappable>());
    }

    private void OnTriggerStay(Collider other)
    {
        // TODO make this a cylinder or something
        Debug.DrawLine(this.transform.position, other.transform.position, Color.red);
    }
}
