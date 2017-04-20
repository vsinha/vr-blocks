using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;

[RequireComponent(typeof(VRTK_InteractableObject))]
public class Spawner: MonoBehaviour {
    private VRTK_InteractableObject interactable;

    public GameObject prefab;

    bool isUsing = false;

    // Use this for initialization
    void Start () {
        interactable = GetComponent<VRTK_InteractableObject>();
        interactable.InteractableObjectUsed += CubeSpawner_InteractableObjectUsed;
        interactable.InteractableObjectUntouched += Interactable_InteractableObjectUntouched;
        interactable.InteractableObjectTouched += Interactable_InteractableObjectTouched;
	}

    private void Interactable_InteractableObjectTouched(object sender, InteractableObjectEventArgs e)
    {
    }

    private void Interactable_InteractableObjectUntouched(object sender, InteractableObjectEventArgs e)
    {
    }

    private void CubeSpawner_InteractableObjectUsed(object sender, InteractableObjectEventArgs e)
    {
        Spawn();
    }

    private void Spawn()
    {
        var controller = interactable.GetUsingObject();
        var cube = Instantiate(prefab, this.transform.position + this.transform.up * 0.3f, Quaternion.identity);
        cube.GetComponent<Rigidbody>().angularVelocity = Vector3.one * 0.1f;
    }

    // Update is called once per frame
    void Update () {
        if (isUsing) {
            Spawn();
        }
	}
}
