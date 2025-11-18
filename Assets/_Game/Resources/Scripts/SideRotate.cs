using UnityEngine;
using System.Collections;

public class SideRotate : MonoBehaviour {

	public float speed = 0; 
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		transform.Rotate(Vector3.forward * Time.deltaTime * speed, Space.World);
		
	}
}
