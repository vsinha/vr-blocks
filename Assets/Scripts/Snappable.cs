using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using VRTK;

[RequireComponent(typeof(VRTK_InteractableObject))]
public class Snappable : MonoBehaviour {
    private SnapParent snapParentPrefab;

    public Collider coll;

    private float _snapSpacing = 0.2f;

    private List<Joint> joints = new List<Joint>();
    private List<Snappable> connectedObjects = new List<Snappable>();
    private List<SnapPoint> snapPoints;

    // Use this for initialization
    void Start () {
        snapParentPrefab = (SnapParent)Resources.Load("Prefabs/SnapParent", typeof(SnapParent));
        snapPoints = transform.GetComponentsInChildren<SnapPoint>().ToList();

        coll = this.GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update () {

    }

    public void SnapPointCollision(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        if (thisSnapPoint.parent.GetInstanceID() == otherSnapPoint.parent.GetInstanceID()) return;
        if (this.IsConnectedTo(otherSnapPoint.parent)) return;  // we're already connected

        UngrabAll();

        DisableCollisions(thisSnapPoint, otherSnapPoint);

        this.transform.rotation = RotateToMatchSnapPoints(thisSnapPoint, otherSnapPoint);
        this.transform.position = MoveToMatchSnapPoints(thisSnapPoint, otherSnapPoint, _snapSpacing);

        MakeJoint(thisSnapPoint, otherSnapPoint);

        connectedObjects.Add(otherSnapPoint.parent);

        //Debug.Log(this.transform.position + " " + otherSnapPoint.transform.parent.position + " " + otherSnapPoint.transform.parent.localPosition);
    }

    private void MakeJoint(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        Debug.Log("making joint");
        var joint = this.gameObject.AddComponent<FixedJoint>();
        joint.connectedAnchor = thisSnapPoint.transform.position;
        joint.connectedBody = otherSnapPoint.transform.parent.GetComponent<Rigidbody>();
        joints.Add(joint);
    }

    private Vector3 MoveToMatchSnapPoints(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint, float spacing)
    {
        // move us to the other snap point
        var a = this.transform.position;
        var b = thisSnapPoint.transform.position;
        float dist = Vector3.Distance(a, b);
        var c = otherSnapPoint.transform.position;
        return c - thisSnapPoint.transform.right * (dist * (1 + spacing));
    }

    private Quaternion RotateToMatchSnapPoints(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        // take the opposite of the snap point we intend to snap to
        // change this for red axis
        var otherSnapPointInverted = otherSnapPoint.transform.rotation * Quaternion.Euler(0, 180, 0);

        // take the difference between our rotation and the rotation of our snap point
        var differenceBetweenUsAndOurSnapPoint = Quaternion.Inverse(thisSnapPoint.transform.rotation) * this.transform.rotation;

        // combine the rotation
        return otherSnapPointInverted * differenceBetweenUsAndOurSnapPoint;
    }

    private void DisableCollisions(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        // make sure none of these two objects' snap points try to interact
        foreach(SnapPoint s in this.snapPoints) {
            foreach(SnapPoint t in otherSnapPoint.parent.snapPoints) {
                Physics.IgnoreCollision(s.coll, t.coll);
            }
        }

        // make sure the parent blocks also don't interact
        Physics.IgnoreCollision(this.coll, otherSnapPoint.parent.coll);

        // simply shut down the two which have connected
        thisSnapPoint.enabled = false;
        otherSnapPoint.enabled = false;
    }

    internal bool IsConnectedTo(Snappable snappable)
    {
        return connectedObjects.Contains(snappable);
    }

    private void UngrabAll()
    {
        UnGrab(this);

        foreach(Snappable obj in connectedObjects) {
            UnGrab(obj);
        }
    }

    private void UnGrab(Snappable obj)
    {
        var interactable = obj.GetComponent<VRTK_InteractableObject>();

        var grabber = interactable.GetGrabbingObject();
        if (grabber != null) {
            grabber.GetComponent<VRTK_InteractGrab>().ForceRelease();
        }
    }
}
