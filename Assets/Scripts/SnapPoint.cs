using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapPoint : MonoBehaviour {
    private bool didJustSnap;

    public SphereCollider coll;
    public SnapPoint colliding = null;

    // Use this for initialization
    void Start () {
        coll = GetComponent<SphereCollider>();
	}
	
	void LateUpdate () {
        didJustSnap = false;
	}

    private void OnTriggerEnter(Collider other)
    {
        var otherSnapPoint = other.gameObject.GetComponent<SnapPoint>();
        colliding = otherSnapPoint;

        if (otherSnapPoint != null && this.didJustSnap == false && !this.IsConnectedTo(other)) {
            // we collided with another snap point

            Debug.Log("trigger enter (" + this.name + " " + this.transform.parent.name + "), (" + other.name + " " + other.transform.parent.name + ")");

            // silence the other one
            otherSnapPoint.didJustSnap = true;

            // call our snap handler
            this.transform.parent.GetComponent<Snappable>().SnapPointCollision(this, otherSnapPoint);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        colliding = null;
    }

    private bool IsConnectedTo(Collider other)
    {
        return this.transform.parent.GetComponent<Snappable>().IsConnectedTo(other.transform.parent.GetComponent<Snappable>());
    }

    //private SnapPoint ClosestSnapPoint(SnapPoint other)
    //{
    //    float minDist = float.MaxValue;
    //    SnapPoint closest = null;

    //    foreach (Transform snapPoint in this.transform.parent.transform) {

    //        var distance = Vector3.Distance(snapPoint.position, other.transform.position);

    //        if (distance < minDist) {
    //            minDist = distance;
    //            closest = snapPoint.GetComponent<SnapPoint>();
    //        }
    //    }
    //    return closest;
    //}

    private void OnTriggerStay(Collider other)
    {
        // TODO make this a cylinder or something
        Debug.DrawLine(this.transform.position, other.transform.position, Color.red);
    }
}
