using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VRTK;

[RequireComponent(typeof(VRTK_InteractableObject))]
public class Snappable : MonoBehaviour {
    private SnapParent snapParentPrefab;

    private float _snapOffset = 1.1f;

    private List<Joint> joints = new List<Joint>();
    private List<Snappable> connectedObjects = new List<Snappable>();

    private VRTK_InteractableObject interactable;

    // Use this for initialization
    void Start () {
        snapParentPrefab = (SnapParent)Resources.Load("Prefabs/SnapParent", typeof(SnapParent));
        interactable = GetComponent<VRTK_InteractableObject>();
        interactable.InteractableObjectUngrabbed += Interactable_InteractableObjectUngrabbed;
    }

    private void Interactable_InteractableObjectUngrabbed(object sender, InteractableObjectEventArgs e)
    {
        // check all child spawnpoints for collisions
        foreach (Transform child in transform) {
            SnapPoint snapPoint = child.GetComponent<SnapPoint>();
            SnapPoint collidingSnapPoint = snapPoint.GetComponent<SnapPoint>().colliding;
            if (collidingSnapPoint) {
                Connect(snapPoint, collidingSnapPoint);
            }
        }
        // make joints
    }


    // Update is called once per frame
    void Update () {
		
	}

    private void Connect(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        var joint = this.gameObject.AddComponent<FixedJoint>();
        joint.connectedAnchor = thisSnapPoint.transform.position;
        joint.connectedBody = otherSnapPoint.transform.parent.GetComponent<Rigidbody>();
        joints.Add(joint);
        connectedObjects.Add(otherSnapPoint.transform.parent.GetComponent<Snappable>());
    }

    public void SnapPointCollision(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        //UnGrab();

        // disable collisions
        thisSnapPoint.enabled = false;
        otherSnapPoint.coll.enabled = false;


        //// move us to the other snap point
        //var a = this.transform.position;
        //var b = thisSnapPoint.transform.position;
        //float dist = Vector3.Distance(a, b);
        //var c = otherSnapPoint.transform.position;
        //this.transform.position = c - thisSnapPoint.transform.right * (dist * _snapOffset);

        //Debug.Log(this.transform.position + " " + otherSnapPoint.transform.parent.position + " " + otherSnapPoint.transform.parent.localPosition);


        // this.Connect(thisSnapPoint, otherSnapPoint);
       
    }

    internal bool IsConnectedTo(Snappable snappable)
    {
        return connectedObjects.Contains(snappable);
    }

    private void UnGrab()
    {
        var grabber = interactable.GetGrabbingObject();
        if (grabber != null) {
            grabber.GetComponent<VRTK_InteractGrab>().ForceRelease();
        }
    }
}
