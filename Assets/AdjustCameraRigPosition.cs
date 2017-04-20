using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

public class AdjustCameraRigPosition : MonoBehaviour {

    public bool performOnStart;
    public float delay = 0.0f;

    public bool bindLeftController;
    public bool bindRightController;

    private Vector3 originalPosition;

    void Start ()
    {
        originalPosition = this.transform.position;

        if (performOnStart) {
            StartCoroutine(AdjustPositionRoutine(delay));
        }

        if (bindLeftController == true) {
            BindController(VRTK_DeviceFinder.GetControllerLeftHand());
        }

        if (bindRightController == true) {
            BindController(VRTK_DeviceFinder.GetControllerRightHand());
        }

    }

    private void BindController(GameObject controller)
    {
        controller.GetComponent<VRTK_ControllerEvents>().ButtonOnePressed += AdjustCameraRigPosition_ButtonOnePressed;
    }

    private void AdjustCameraRigPosition_ButtonOnePressed(object sender, ControllerInteractionEventArgs e)
    {
        AdjustPosition();
    }

    private IEnumerator AdjustPositionRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        AdjustPosition();
    }

    public void AdjustPosition()
    {
        var target = GameObject.Find("[TargetHeadPosition]");
        if (target == null) {
            Debug.Log("add a [TargetHeadPosition] transform at the desired camera position!");
            return;
        }

        var head = GameObject.FindGameObjectWithTag("MainCamera");
        if (head == null) {
            Debug.Log("No object tagged \"MainCamera\"");
            return;
        }

        this.transform.position += target.transform.position - head.transform.position;
    }
}
