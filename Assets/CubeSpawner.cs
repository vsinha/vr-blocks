using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

[RequireComponent(typeof(VRTK_InteractableObject))]

public class CubeSpawner : MonoBehaviour {
    private VRTK_InteractableObject interactable;
    private GameObject cubePrefab;

    bool isUsing = false;

    // Use this for initialization
    void Start () {
        interactable = GetComponent<VRTK_InteractableObject>();
        interactable.InteractableObjectUsed += CubeSpawner_InteractableObjectUsed;
        interactable.InteractableObjectUntouched += Interactable_InteractableObjectUntouched;
        interactable.InteractableObjectTouched += Interactable_InteractableObjectTouched;

        cubePrefab = (GameObject)Resources.Load("Prefabs/ParentedBlock", typeof(GameObject));
	}

    private void Interactable_InteractableObjectTouched(object sender, InteractableObjectEventArgs e)
    {
    }

    private void Interactable_InteractableObjectUntouched(object sender, InteractableObjectEventArgs e)
    {
    }

    private void CubeSpawner_InteractableObjectUsed(object sender, InteractableObjectEventArgs e)
    {
        SpawnCube();
    }

    private void SpawnCube()
    {
        var controller = interactable.GetUsingObject();
        var cube = Instantiate(cubePrefab, this.transform.position + this.transform.up * 0.01f, Quaternion.identity);
        cube.GetComponent<Rigidbody>().angularVelocity = Vector3.one * 0.1f;
    }

    // Update is called once per frame
    void Update () {
        if (isUsing) {
            SpawnCube();
        }
	}
}
