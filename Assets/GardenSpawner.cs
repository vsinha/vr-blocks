using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GardenSpawner : MonoBehaviour {

    GameObject[] prefabs;
    private float maxRadius = 300f;
    private float minRadius = 100f;
    private float _delaySeconds = 3f;
    private int numSpawnedObjects = 0;
    private int _maxSpawnedObjects = 100;

    // Use this for initialization
    void Start () {
        prefabs = Resources.LoadAll("Prefabs/GardenObjects", typeof(GameObject)).Cast<GameObject>().ToArray();
        Debug.Log("loaded " + prefabs.Length + " prefabs for the garden");

        StartCoroutine(SpawnObjectOccasionally(_delaySeconds));
	}

    private IEnumerator SpawnObjectOccasionally(float delay)
    {
        while (numSpawnedObjects < _maxSpawnedObjects) {
            yield return new WaitForSeconds(delay);
            SpawnObject();
            numSpawnedObjects += 1;
        }
    }

    private void SpawnObject()
    {
        Vector3 position = SelectSpawnLocation();
        float scale = 2f * Mathf.Sqrt(position.magnitude);

        // select a random prefab
        var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];

        var obj = Instantiate(prefab, position, Quaternion.Euler(0, 180, 0));

        var bounds = GetMaxBounds(obj);
        var longestAxis = LongestAxis(bounds.extents);
        var rotation = Quaternion.FromToRotation(longestAxis, Vector3.down);
        obj.transform.rotation = rotation;

        obj.transform.localScale = scale * Vector3.one;
    }

    private Vector3 SelectSpawnLocation()
    {
        // random location in a circle away from the player
        //float radius = UnityEngine.Random.Range(minRadius, maxRadius);

        float range = maxRadius - minRadius;
        float radius = Mathf.Pow(0.5f + UnityEngine.Random.value, 2) * range;

        Vector2 point = UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 pointOnFlatCircle = new Vector3(point.x, 0, point.y);
        var loc = pointOnFlatCircle * radius;
        return loc;
    }

    private float MaxComponent(Vector3 vec)
    {
        return Mathf.Max(new float[] { vec.x, vec.y, vec.z });
    }

    private Vector3 LongestAxis(Vector3 vec)
    {
        var maxComponent = MaxComponent(vec);
        var index = 0;

        for (var i = 0; i < 3; i++) {
            if (vec[i] == maxComponent) {
                index = i;               
            }
        }

        var v = Vector3.zero;
        v[index] = 1;
        return v;
    }

    Bounds GetMaxBounds(GameObject g)
    {
        var b = new Bounds(g.transform.position, Vector3.zero);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>()) {
            b.Encapsulate(r.bounds);
        }
        return b;
    }
}
