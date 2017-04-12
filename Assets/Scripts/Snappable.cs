using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VRTK;

[RequireComponent(typeof(VRTK_InteractableObject))]
public class Snappable : MonoBehaviour {
    private SnapParent snapParentPrefab;

    private float _snapSpacing = 0.2f;
    private List<Joint> joints = new List<Joint>();
    private List<Snappable> connectedObjects = new List<Snappable>();

    private float speed = 1.0f;
    
    // Use this for initialization
    void Start () {
        snapParentPrefab = (SnapParent)Resources.Load("Prefabs/SnapParent", typeof(SnapParent));
    }

    // Update is called once per frame
    void Update () {

    }

    public void SnapPointCollision(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        if (thisSnapPoint.transform.parent.GetInstanceID() == otherSnapPoint.transform.parent.GetInstanceID()) {
            // don't connect us to ourself
            return;
        }

        if (this.IsConnectedTo(otherSnapPoint.transform.parent.GetComponent<Snappable>())) {
            // we're already connected
            return;
        }

        UngrabAll();

        DisableCollisionsBetweenSnapPoints(thisSnapPoint, otherSnapPoint);

        this.transform.rotation = RotateToMatchSnapPoints(thisSnapPoint, otherSnapPoint);
        this.transform.position = MoveToMatchSnapPoints(thisSnapPoint, otherSnapPoint, _snapSpacing);
    
        MakeJoint(thisSnapPoint, otherSnapPoint);

        //StartCoroutine(AnimatedSnap(thisSnapPoint, otherSnapPoint, targetPosition, targetRotation));

        connectedObjects.Add(otherSnapPoint.transform.parent.GetComponent<Snappable>());
        Debug.Log(connectedObjects.Count + " connected objects");

        //Debug.Log(this.transform.position + " " + otherSnapPoint.transform.parent.position + " " + otherSnapPoint.transform.parent.localPosition);
    }

    //private IEnumerator AnimatedSnap(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint, Vector3 targetPosition, Quaternion targetRotation)
    //{



    //    float step = speed * Time.deltaTime;
    //    transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
    //    transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, step);
    //}

    private void MakeJoint(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
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

    private static void DisableCollisionsBetweenSnapPoints(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        // disable collisions
        thisSnapPoint.enabled = false;
        otherSnapPoint.coll.enabled = false;
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
