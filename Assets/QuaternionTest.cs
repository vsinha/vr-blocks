using System;
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

        Debug.Log("this: " + this.transform.eulerAngles);
        Debug.Log("otherSnapPoint: " + otherSnapPoint.transform.eulerAngles);

        // take the opposite of the snap point we intend to snap to
        // change this for red axis
        var otherSnapPointInverted = otherSnapPoint.transform.rotation * Quaternion.Euler(180, 0, 0);

        Debug.Log("otherSnapPointInverted: " + otherSnapPointInverted.eulerAngles);

        // take the difference between our rotation and the rotation of our snap point
        var differenceBetweenUsAndOurSnapPoint = Quaternion.Inverse(thisSnapPoint.transform.rotation) * this.transform.rotation;

        Debug.Log("differenceBetweenUsAndOurSnapPoint: " + otherSnapPointInverted.eulerAngles);

        // apply the combined rotation

        fromRotation = transform.rotation;
        toRotation = otherSnapPointInverted * differenceBetweenUsAndOurSnapPoint;

        var toE = toRotation.eulerAngles;
        toRotation.eulerAngles = new Vector3(toE.x + Closest90(Mathf.Abs(toE.x - this.transform.eulerAngles.x)), toE.y, toE.z);

        this.transform.rotation = toRotation;
        Debug.Log("this again: " + this.transform.eulerAngles);
    }

    private float Closest90(float x)
    {
        float y;

        if (x <= 45)                     y = 180f;
        else if (x > 45 && x <= 135)     y = 90f;
        else if (x > 135 && x <= 225)    y = 180f;
        else if (x > 225 && x <= 315)    y = 270f;
        else                             y = 0;    // (x > 315 && x <= 360)

        Debug.Log("closest 90 of " + x + " is " + y);

        return y;
    }

    // Update is called once per frame
    void Update () {
        //this.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, Time.time * speed);
    }
}
