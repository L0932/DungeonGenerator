using UnityEngine;
using System.Collections;

public class GUIObject : MonoBehaviour {

	// Use this for initialization

	public Transform target;
	private bool once;

	void Start () {
		once = true;
	}
	
	// Update is called once per frame
	void Update () {
	
		if (once && target) {
			transform.position = target.position;
			once = false;
		}

	}

	public void SetPosition(Vector3 pos){
		transform.position = pos;
	}
}
