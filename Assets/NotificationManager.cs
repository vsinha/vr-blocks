using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NotificationManager : MonoBehaviour {

	public GameObject NotificationPrefab;
	public GameObject PicturePrefab;
	public GameObject CallPrefab;
	private GameObject callInstance;

	public BoxCollider Volume; 

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	internal void OnNotification(MessageAction<NotificationData> obj)
	{
		if (String.IsNullOrEmpty (obj.data.applicationName)) { 
			return; 
		}

		var instance = GameObject.Instantiate (NotificationPrefab, Vector3.zero, this.transform.rotation);

		var message = instance.GetComponent<NotificationMessage> ();
		message.SetFromData (obj.data);

		// Place message inside volume 
		PlaceItem (instance);
	}


	internal void OnCall(MessageAction<CallData> obj)
	{
		if (this.callInstance != null) { 
			Destroy(this.callInstance); 
			this.callInstance = null;
			return; 
		}

		this.callInstance = GameObject.Instantiate (CallPrefab, Vector3.zero, this.transform.rotation);
		var message = this.callInstance.GetComponent<CallMessage> ();
		message.LoadFrom (obj.data);

		PlaceItem (this.callInstance);
	}

	internal void OnPicture(MessageAction<PictureData> obj)
	{
		var instance = GameObject.Instantiate (PicturePrefab, Vector3.zero, this.transform.rotation);

		var message = instance.GetComponent<PictureMessage> ();
		message.LoadFrom (obj.data);

		// Place message inside volume 
		PlaceItem (instance);

	}

	void PlaceItem (GameObject instance)
	{
		var bottom = Volume.transform.position.y - Volume.transform.lossyScale.y / 2;
		var top = Volume.transform.position.y + Volume.transform.lossyScale.y / 2;
		var x = Volume.transform.position.x;
		var z = Volume.transform.position.z;
		var y = bottom;
		while (y <= top) {
			// Start at the bottom of the box 
			var c = Physics.OverlapBox (new Vector3 (x, y, z), instance.transform.lossyScale / 6f);
			if (c.Length <= 1) {
				instance.transform.position = new Vector3 (x, y, z);
				break;
			}
			y += 0.02f;
			// Step
		}
	}

	internal void OnRemoteMessage(MessageAction<MessageData> obj)
	{
		var instance = GameObject.Instantiate (NotificationPrefab, Vector3.zero, this.transform.rotation);

		var message = instance.GetComponent<NotificationMessage> ();
		message.SetFromData (obj.data);

		// Place message inside volume 
		PlaceItem (instance);

	}

}
