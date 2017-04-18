using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeRandomColor : MonoBehaviour {

	void Start () {
        var rend = this.GetComponent<Renderer>();
        rend.material.color = GameObject.Find("[ColorSelector]").GetComponent<RandomColor>().SelectRandomColor();
    }
}
