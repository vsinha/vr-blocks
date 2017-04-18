using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayAmbientAudio : MonoBehaviour {
    private AudioSource a;

    // Use this for initialization
    void Start () {
         a = this.GetComponent<AudioSource>();

        var clip = (AudioClip)Resources.Load("Sounds/ambientSounds", typeof(AudioClip));

        Debug.Log("loaded audio clip of length: " + clip.length);

        a.clip = clip;
        a.loop = true;
        a.Play();
	}
	
	// Update is called once per frame
	void Update () {
	}
}
