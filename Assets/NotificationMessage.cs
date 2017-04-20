using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class NotificationMessage : MonoBehaviour {

	public Text Title;
	public Text AppName;
	public Text Body;
	public Image Image; 

	void Start() { 
		this.GetComponent<Animation> ().Play ();
	}

	internal void SetFromData(NotificationData data) { 
		this.Title.text = data.title;
		this.Body.text = data.content ?? "";
		this.AppName.text = data.applicationName;

		if (!String.IsNullOrEmpty (data.iconImage)) {
			var binary = Convert.FromBase64String (data.iconImage);
			var tex = new Texture2D (2, 2); 
			tex.LoadImage (binary); 
			Image.sprite = Sprite.Create (tex, new Rect (0, 0, tex.width, tex.height), new Vector2 (0.5f, 0.5f), 40);
		}

	}

	internal void SetFromData(MessageData data) { 
		this.AppName.text = "New Message";
		this.Title.text = "From: " + data.contact;
		this.Body.text = data.content;
	}
}
