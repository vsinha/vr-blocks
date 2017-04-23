using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnappableAssign : MonoBehaviour {
    private GameObject snappableMesh;

    public void AssignSnappableChild(GameObject mesh,  bool randomColor = true)
    {
        this.snappableMesh = mesh;

        // Set parent, and layer 
        mesh.transform.SetParent(this.transform);
        mesh.layer = LayerMask.NameToLayer("SnappableObjects");

        // TODO: Demo code 
        if(randomColor)
        {
            mesh.AddComponent<MakeRandomColor>();
        }
    }

    public GameObject GenerateSnapPoint(string name = "Generated snap point")
    {
        // Create object 
        var snapObj = new GameObject(name);
        snapObj.layer = LayerMask.NameToLayer("SnapPoints");

        // Create collider and parent 
        var collider = snapObj.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        snapObj.transform.SetParent(this.snappableMesh.transform);

        // Add snap point behavior 
        var behavior = snapObj.AddComponent<SnapPoint>();
        behavior.parentSnappable = this.GetComponent<Snappable>();
        behavior.coll = collider;
        return snapObj;
    }
}
