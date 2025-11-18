using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LargeMap : MonoBehaviour {

	//public DGenerator generator;

	private bool state;
	private GameObject childMap;
	// Use this for initialization
	void Start () {
		state = false;
		childMap = transform.GetChild(0).gameObject;

		transform.position = DGenerator.GetDungeonBounds().center + new Vector3(0, 390, 0);
	}
	
	// Update is called once per frame
	void Update () {

		if(Input.GetButtonDown("OpenMap") && childMap){
			state = !state;
			childMap.SetActive(state);
		}
	
	}
}
