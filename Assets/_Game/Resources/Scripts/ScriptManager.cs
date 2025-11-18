using UnityEngine;
using System.Collections;

public class ScriptManager : MonoBehaviour {

	//static ScriptManager instance;

	/*
	public static ScriptManager Instance
	{
		get {

			if (instance == null) {
				GameObject go = new GameObject ();
				instance = go.AddComponent<ScriptManager> ();
			}

			return instance;
		}
	}*/

	public GameObject MapGUI;
	public static ClearRoomGUI ClearRoomScript;

	void Awake(){

		ClearRoomScript = MapGUI.GetComponent<ClearRoomGUI>();
	}
}
