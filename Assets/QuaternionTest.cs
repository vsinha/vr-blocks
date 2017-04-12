using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
        this.transform.Rotate(this.transform.up, 180.0f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
