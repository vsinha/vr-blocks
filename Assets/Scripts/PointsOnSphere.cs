using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointsOnSphere : MonoBehaviour {

    private void Start()
    {
        float scaling = 1f;
        Vector3[] points = GeneratePointsOnSphere(30);
        List<GameObject> spheres = new List<GameObject>();

        for (var i = 0; i < points.Length; i++) {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spheres.Add(sphere);

            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.transform.SetParent(this.transform);
            sphere.transform.localPosition = points[i] * scaling;

            var direction = sphere.transform.position - this.transform.position;

            sphere.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90f, 0);
        }
    }

    private Vector3[] GeneratePointsOnSphere(int n)
    {
        List<Vector3> points = new List<Vector3>();
        float inc = Mathf.PI * (3 - Mathf.Sqrt(5));
        float off = 2.0f / n;

        float x, y, z, r, phi;

        for (var k = 0; k < n; k++) {
            y = k * off - 1 + (off / 2);
            r = Mathf.Sqrt(1 - y * y);
            phi = k * inc;
            x = Mathf.Cos(phi) * r;
            z = Mathf.Sin(phi) * r;

            points.Add(new Vector3(x, y, z));
        }

        return points.ToArray();
    }
}
