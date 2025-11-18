using UnityEngine;
using System.Collections;

public class miniCam : MonoBehaviour {

	// Use this for initialization
	void Start () {

		Camera cam = GameObject.Find ("MiniMap").GetComponent<Camera>();
		CamShade camScript = cam.GetComponent<CamShade> ();
		camScript.player = gameObject;
	}
}
