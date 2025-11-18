using UnityEngine;
using System.Collections;

public class Highlight : MonoBehaviour {

	// Use this for initialization
	void Start () {

		Vector3 pos = transform.position;

		RaycastHit hit;
		if (Physics.Raycast (transform.position, -Vector3.up, out hit, Mathf.Infinity)) {
			//Debug.Log ("Hit distance" + hit.distance);
			if(pos.y > 0)
				pos.y -= hit.distance;

			transform.position = pos;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
