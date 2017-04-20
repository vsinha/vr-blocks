using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallMessage : MonoBehaviour {

	public TextMesh Name;

	internal void LoadFrom(CallData data) { 
		Name.text = data.contact ?? data.phone ?? "Unknown";
	}

	void Update() { 
		
	}
}
