using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapIndicator : MonoBehaviour {

    public Transform a;
    public Transform b;
    private bool initialized;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        if (!initialized) return;

        if (a == null || b == null) {
            Destroy(this.gameObject);
            return;
        }

        Draw(a, b);		
	}

    public void Initialize(Transform a, Transform b)
    {
        this.initialized = true;
        this.a = a;
        this.b = b;
    }

    private void Draw(Transform a, Transform b) {

        var center = (a.position + b.position) / 2.0f;
        var distance = Vector3.Distance(a.position, b.position) / 2;
        var scale = new Vector3(this.transform.localScale.x, this.transform.localScale.y, distance);

        this.transform.localScale = scale;
        this.transform.position = center;
        this.transform.LookAt(b);
    }
}
