using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PictureMessage : MonoBehaviour {

	public GameObject ImageTarget;
	public GameObject PictureTarget; 

	private const float maxSize = 0.2f;

	internal void LoadFrom(PictureData data) { 

		var binary = Convert.FromBase64String (data.notificationImage);
		var tex = new Texture2D (2, 2); 
		tex.LoadImage (binary); 

		if (tex.height < tex.width) { 
			PictureTarget.transform.localScale = new Vector3 (0.2f, 0.2f * (float)tex.height / (float)tex.width, 0.1f);
		} else if (tex.height > tex.width) { 
			PictureTarget.transform.localScale = new Vector3 (0.2f * (float)tex.width / (float)tex.height, 0.2f, 0.1f);
		}

		var rend = ImageTarget.GetComponent<Renderer> ();
		rend.material.mainTexture = tex;
	}
}
