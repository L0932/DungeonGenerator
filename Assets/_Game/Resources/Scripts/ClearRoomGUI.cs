using UnityEngine;
using System.Collections;


[ExecuteInEditMode]
public class ClearRoomGUI : MonoBehaviour {

	public GameObject clearObject;
	private GameObject clearInstance;
	// Use this for initialization
	void Start () {

		//GameObject obj = Instantiate (clearObject, clearObject.transform.position, clearObject.transform.rotation) as GameObject;

		//obj.transform.SetParent (transform, false);
		//obj.transform.parent = transform;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void ClearRoom(Transform room){

		/*
		Debug.Log ("Room cleared. Adding UI Object");
		clearInstance = Instantiate (clearObject, room.position, clearObject.transform.rotation) as GameObject;
		//clearInstance.GetComponent<GUIObject> ().SetPosition (pos);//transform.position = pos + new Vector3 (0, 10f, 0);
		//clearInstance.transform.parent = transform;
		clearInstance.transform.SetParent(transform, false);
		clearInstance.GetComponent<GUIObject> ().target = room;
		//clearInstance.GetComponent<GUIObject> ().SetPosition (pos);//transform.position = pos + new Vector3 (0, 10f, 0);
		*/
		
	}
}
