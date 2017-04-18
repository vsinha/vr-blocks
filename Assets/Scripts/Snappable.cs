﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using VRTK;

public class Snappable : MonoBehaviour {
    private SnapParent snapParentPrefab;

    private float _snapSpacing = 0.2f;

    private List<Joint> joints = new List<Joint>();
    //private List<Snappable> connectedObjects = new List<Snappable>();
    private List<SnapPoint> snapPoints;
    private AudioClip blockSnapAudio;
    private SnapIndicator snapIndicatorPrefab;

    // Use this for initialization
    void Start () {

        snapParentPrefab = (SnapParent)Resources.Load("Prefabs/SnapParent", typeof(SnapParent));
        snapIndicatorPrefab = (SnapIndicator)Resources.Load("Prefabs/SnapCylinder", typeof(SnapIndicator));
        blockSnapAudio = (AudioClip)Resources.Load("Sounds/snap1", typeof(AudioClip));

        snapPoints = transform.GetComponentsInChildren<SnapPoint>().ToList();
    }

    // Update is called once per frame
    void Update () {

    }

    private void LateUpdate()
    {
        if (transform.childCount == 0) {
            Destroy(this.gameObject);
        }
    }

    public void SnapPointCollision(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        if (thisSnapPoint.parentSnappable.GetInstanceID() == otherSnapPoint.parentSnappable.GetInstanceID()) return;
        //if (this.IsConnectedTo(otherSnapPoint.parentSnappable)) return;  // we're already connected

        // sound
        AudioSource.PlayClipAtPoint(blockSnapAudio, thisSnapPoint.transform.position, 1f);

        // join indicator
        var cylinder = Instantiate(snapIndicatorPrefab);
        cylinder.Initialize(thisSnapPoint.transform, otherSnapPoint.transform);

        // prepare for move
        UnGrab(this);
        UnGrab(otherSnapPoint.parentSnappable);
        DisableSnapPointInteractions(this, otherSnapPoint.parentSnappable);

        // move & rotate
        this.transform.rotation = RotateToMatchSnapPoints(thisSnapPoint, otherSnapPoint);
        this.transform.position = MoveToMatchSnapPoints(thisSnapPoint, otherSnapPoint, _snapSpacing);

        // reparent all children
        this.ReparentChildren(otherSnapPoint.parentSnappable.transform);
    }

    private void ReparentChildren(Transform newParent)
    {
        // as we remove items from the original list of children, the list changes size
        // keep our own List of references and iterate over that
        List<Transform> children = new List<Transform>();

        for (var i = 0; i < this.transform.childCount; i++) {
            children.Add(this.transform.GetChild(i));
        }

        foreach(Transform child in children) {
            child.SetParent(newParent);

            foreach (Transform s in child) {
                var snapPoint = s.GetComponent<SnapPoint>();
                if (snapPoint != null) {
                    snapPoint.UpdateParentRef();
                }
            }
        }
    }

    //private void MakeJoint(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    //{
    //    Debug.Log("making joint");
    //    var joint = this.gameObject.AddComponent<FixedJoint>();
    //    joint.connectedAnchor = thisSnapPoint.transform.position;
    //    joint.connectedBody = otherSnapPoint.transform.parent.GetComponent<Rigidbody>();
    //    joints.Add(joint);
    //}

    private Vector3 MoveToMatchSnapPoints(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint, float spacing)
    {
        // move us to the other snap point
        var a = this.transform.position;
        var b = thisSnapPoint.transform.position;
        var c = otherSnapPoint.transform.position;
        var finalPosition = a + (c - b);

        return finalPosition;
    }

    private Quaternion RotateToMatchSnapPoints(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        // take the difference between our rotation and the rotation of our snap point
        var differenceBetweenUsAndOurSnapPoint = Quaternion.Inverse(thisSnapPoint.transform.rotation) * this.transform.rotation;

        // take the opposite of the snap point we intend to snap to
        // change this for red axis
        var otherSnapPointInverted = otherSnapPoint.transform.rotation * Quaternion.Euler(0, 180f, 0);

        // generate 4 possible rotations around the snap point axis, each 90 degrees apart
        List<Quaternion> otherSnapPointOptions = new List<Quaternion>();
        for (var i = 0; i < 4; i++) {
            otherSnapPointOptions.Add(otherSnapPointInverted * Quaternion.Euler(90f * i, 0, 0));
        }

        var selectedSnapPointRotation = MinRotationDistance(thisSnapPoint.transform.rotation, otherSnapPointOptions);

        // combine the rotation
        return selectedSnapPointRotation * differenceBetweenUsAndOurSnapPoint;
    }

    internal Quaternion MinRotationDistance(Quaternion ourRotation, List<Quaternion> options)
    {
        var distance = float.MaxValue;
        Quaternion selection = ourRotation;
        int selectionIndex = -1;

        for (var i = 0; i < options.Count; i++) {
            var d = Quaternion.Angle(ourRotation, options[i]);
            if (d < distance) {
                distance = d;
                selection = options[i];
                selectionIndex = i;
            }
        }

        return selection;
    }

    //private void DisableSnapPointInteractions(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    //{
    //    // make sure none of these two objects' snap points try to interact
    //    foreach(SnapPoint s in this.snapPoints) {
    //        foreach(SnapPoint t in otherSnapPoint.parentSnappable.snapPoints) {
    //            Physics.IgnoreCollision(s.coll, t.coll);
    //            //Physics.IgnoreCollision(s.transform.parent.GetComponent<Collider>(), t.transform.parent.GetComponent<Collider>());
    //        }
    //    }

    //    // make sure the parent blocks also don't interact
    //    //Physics.IgnoreCollision(this.coll, otherSnapPoint.parentSnappable.coll);

    //    // simply shut down the two which have connected
    //    thisSnapPoint.enabled = false;
    //    otherSnapPoint.enabled = false;
    //}

    private void DisableSnapPointInteractions(Snappable s1, Snappable s2)
    {
        var c1 = GetComponentsInChildren<SnapPoint>();
        var c2 = GetComponentsInChildren<SnapPoint>();

        for (var i = 0; i < c1.Length; i++) {
            for (var j = 0; j < c2.Length; j++) {
                Physics.IgnoreCollision(c1[i].coll, c2[j].coll);
            }
        }
    }

    public void UnGrab(Snappable obj)
    {
        var interactable = obj.GetComponent<VRTK_InteractableObject>();

        if (interactable == null) return;

        var grabber = interactable.GetGrabbingObject();
        if (grabber != null) {
            grabber.GetComponent<VRTK_InteractGrab>().ForceRelease();
        }
    }
}
