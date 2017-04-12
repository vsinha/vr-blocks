using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionTest : MonoBehaviour {

    public GameObject otherCube;
    public GameObject otherSnapPoint;
    public GameObject thisSnapPoint;
    private Quaternion toRotation;
    private Quaternion fromRotation;
    private float speed = 1f;

    // Use this for initialization
    void Start () {
        //this.transform.Rotate(this.transform.up, 180.0f);

        // take the opposite of the snap point we intend to snap to
        // change this for red axis
        var otherSnapPointInverted = otherSnapPoint.transform.rotation * Quaternion.Euler(180, 0, 0);

        // take the difference between our rotation and the rotation of our snap point
        var differenceBetweenUsAndOurSnapPoint = Quaternion.Inverse(thisSnapPoint.transform.rotation) * this.transform.rotation;

        // apply the combined rotation
        //this.transform.rotation = otherSnapPointInverted * differenceBetweenUsAndOurSnapPoint;

        fromRotation = transform.rotation;
        toRotation = otherSnapPointInverted * differenceBetweenUsAndOurSnapPoint;
    }
	
	// Update is called once per frame
	void Update () {
        this.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, Time.time * speed);
	}
}
