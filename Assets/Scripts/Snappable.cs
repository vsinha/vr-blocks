using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using VRTK;

[RequireComponent(typeof(VRTK_InteractableObject))]
public class Snappable : MonoBehaviour {

	public SnapBehaviour SnapBehaviour = SnapBehaviour.Default;

    private float _snapSpacing = 0.2f;
    private float _throwThresholdVelocity = 2.0f;
    private float _detonationDistance = 10.0f;

    private List<SnapPoint> snapPoints;

    private AudioClip blockSnapAudio;
    private AudioClip detonationAudio;

    //private SnapIndicator snapIndicatorPrefab;
    private SnapParent snapParentPrefab;

    private VRTK_InteractableObject interactable;
    private Rigidbody rb;
    private bool wasThrown = false;

    // Use this for initialization
    void Start () {
        rb = this.GetComponent<Rigidbody>();
        interactable = this.GetComponent<VRTK_InteractableObject>();
        interactable.InteractableObjectUngrabbed += Interactable_InteractableObjectUngrabbed;

        snapParentPrefab = (SnapParent)Resources.Load("Prefabs/SnapParent", typeof(SnapParent));
        //snapIndicatorPrefab = (SnapIndicator)Resources.Load("Prefabs/SnapCylinder", typeof(SnapIndicator));
        blockSnapAudio = (AudioClip)Resources.Load("Sounds/snap1", typeof(AudioClip));
        detonationAudio = (AudioClip)Resources.Load("Sounds/explosion", typeof(AudioClip));

        snapPoints = transform.GetComponentsInChildren<SnapPoint>().ToList();
    }

    private void Interactable_InteractableObjectUngrabbed(object sender, InteractableObjectEventArgs e)
    {
        if (rb.velocity.magnitude > _throwThresholdVelocity) {
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            wasThrown = true;
        }
    }

    // Update is called once per frame
    void Update () {
         Detonate();
    }

    private void LateUpdate()
    {
        if (transform.childCount == 0) {
            Destroy(this.gameObject);
        }
    }

    public void SnapPointCollision(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint)
    {
        // sound
        AudioSource.PlayClipAtPoint(blockSnapAudio, thisSnapPoint.transform.position, 1f);

        //// join indicator
        //var cylinder = Instantiate(snapIndicatorPrefab);
        //cylinder.Initialize(thisSnapPoint.transform, otherSnapPoint.transform);

        // prepare for move
        UnGrab(this);
        UnGrab(otherSnapPoint.parentSnappable);

        // disable interaction between the two snap points
        Physics.IgnoreCollision(thisSnapPoint.coll, otherSnapPoint.coll);

        // Perform snap 
		this.PerformSnapBehaviour (thisSnapPoint, otherSnapPoint);

        // reparent all children
        this.ReparentChildren(otherSnapPoint.parentSnappable.transform);

    }

	private void PerformSnapBehaviour(SnapPoint thisSnapPoint, SnapPoint otherSnapPoint) { 

		var targetBehavior = SnapBehaviour.Default;

		var thisBehavior = thisSnapPoint.GetComponentInParent<Snappable> ().SnapBehaviour;
		var otherBehavior = thisSnapPoint.GetComponentInParent<Snappable> ().SnapBehaviour;

		// Both match so use that (e.g. face to face) 
		if (thisBehavior == otherBehavior) { 
			targetBehavior = thisBehavior;
		}
			
		if (targetBehavior == SnapBehaviour.Default) { 
			// Just move, do not change rotation 
			this.transform.position = MoveToMatchSnapPoints(thisSnapPoint, otherSnapPoint, _snapSpacing);
		} else if (targetBehavior == SnapBehaviour.RotateFaces) { 
			// Match rotation and move
			this.transform.rotation = RotateToMatchSnapPoints(thisSnapPoint, otherSnapPoint);
			this.transform.position = MoveToMatchSnapPoints(thisSnapPoint, otherSnapPoint, _snapSpacing);
		}
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
                    snapPoint.UpdateParentSnappableRef();
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

    //private void DisableSnapPointInteractions(Snappable s1, Snappable s2)
    //{

    //    //var c1 = GetComponentsInChildren<SnapPoint>();
    //    //var c2 = GetComponentsInChildren<SnapPoint>();

    //    //for (var i = 0; i < c1.Length; i++) {
    //    //    for (var j = 0; j < c2.Length; j++) {
    //    //        Physics.IgnoreCollision(c1[i].coll, c2[j].coll);
    //    //    }
    //    //}
    //}

    public void UnGrab(Snappable obj)
    {
        var interactable = obj.GetComponent<VRTK_InteractableObject>();

        if (interactable == null) return;

        var grabber = interactable.GetGrabbingObject();
        if (grabber != null) {
            grabber.GetComponent<VRTK_InteractGrab>().ForceRelease();
        }
    }


    internal bool IsGrabbed()
    {
        return interactable.IsGrabbed();
    }

    internal bool IsSnapEnabled()
    {
        // enabled unless some button is being held down
        var grabber = interactable.GetGrabbingObject();
        if (grabber) {
            return !grabber.GetComponent<VRTK_ControllerEvents>().buttonOnePressed;
        }
        return true; 
    }

    internal void Detonate()
    {
        // if we've been thrown
        if (wasThrown && this.transform.position.magnitude > _detonationDistance) {

            Debug.Log("detonating");

            AudioSource.PlayClipAtPoint(detonationAudio, this.transform.position, 1f);


            // unparent all our children
            List<Transform> children = new List<Transform>();

            for (var i = 0; i < this.transform.childCount; i++) {
                children.Add(this.transform.GetChild(i));
            }

            foreach (Transform child in children) {
                Debug.Log("unparenting");
                var rb = child.gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.AddExplosionForce(1000f, this.transform.position, 2f);
                child.transform.SetParent(null);
            }

            Destroy(this);
        }
    }
}
