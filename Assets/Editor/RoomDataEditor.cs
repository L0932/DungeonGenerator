using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//[CustomEditor(typeof(RoomData))]
public class RoomDataEditor : Editor {

	override public void OnInspectorGUI(){
		//RoomData soScript = (RoomData)target;

		if(GUI.changed)
			Debug.Log ("Changed");

		if(GUILayout.Button ("Clear Data")){
			//soScript.data = new List<RoomClass>();
			//soScript.data.Add(new RoomClass());
		}
	}


	
}
