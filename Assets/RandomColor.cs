using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomColor : MonoBehaviour {

    private static Color[] Colors =
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.white
    };

    public Color SelectRandomColor()
    {
        return Colors[Random.Range(0, Colors.Length)];
    }
}
