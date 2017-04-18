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

    // Use this for initialization
    void Start ()
    {
        Debug.Log("this: " + this.transform.eulerAngles);
        Debug.Log("otherSnapPoint: " + otherSnapPoint.transform.eulerAngles);

        // take the opposite of the snap point we intend to snap to
        var otherSnapPointInverted = otherSnapPoint.transform.rotation * Quaternion.Euler(180, 0, 0);

        // generate 4 possible rotations around the snap point axis, each 90 degrees apart
        List<Quaternion> otherSnapPointOptions = new List<Quaternion>();
        for (var i = 0; i < 4; i++) {
            otherSnapPointOptions.Add(otherSnapPointInverted * Quaternion.Euler(0, 90f * i, 0));
        }
        
        otherSnapPointInverted = MinRotationDistance(thisSnapPoint.transform.rotation, otherSnapPointOptions);

        Debug.Log("otherSnapPointInverted: " + otherSnapPointInverted.eulerAngles);

        // take the difference between our rotation and the rotation of our snap point
        var differenceBetweenUsAndOurSnapPoint = Quaternion.Inverse(thisSnapPoint.transform.rotation) * this.transform.rotation;

        Debug.Log("differenceBetweenUsAndOurSnapPoint: " + otherSnapPointInverted.eulerAngles);

        // apply the combined rotation
        fromRotation = transform.rotation;
        toRotation = otherSnapPointInverted * differenceBetweenUsAndOurSnapPoint;

        this.transform.rotation = toRotation;
    }

    private Quaternion MinRotationDistance(Quaternion ourRotation, List<Quaternion> options)
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

        Debug.Log("selected rotation " + selectionIndex);
        return selection;
    }

    // Update is called once per frame
    void Update () {
    }
}
