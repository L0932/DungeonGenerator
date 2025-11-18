using UnityEngine;
using System.Collections;
//using UnityEditor;

public class TestScript_2 : MonoBehaviour {

	public DGenerator dGen;
	public GameObject player;
	// Use this for initialization

	void Start () {
	
		player.transform.position = new Vector3(0, 5, 0);
	}
	
	// Update is called once per frame
	void Update () {

		if(Input.GetButtonDown("Restart"))
		{
			Debug.Log ("Restarting");
			Application.LoadLevel (Application.loadedLevelName);
		}
	
	}
}
