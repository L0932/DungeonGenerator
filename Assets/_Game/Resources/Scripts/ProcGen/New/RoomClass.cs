using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class RoomClass {

	public string name;
	public GameObject gameObject;
	public Vector3 floorSize;

	private Transform[] doorLocations;
	
	public RoomClass(){
		name = "Empty";
		floorSize = Vector3.zero;
	}

	public RoomClass(GameObject obj, Vector3 size){
		name = obj.name;
		gameObject = obj;
		floorSize = size;
	}

	public RoomClass(string nm, GameObject go, Vector3 sz){
		name = nm;
		gameObject = go;
		floorSize = sz;
	}


	public Vector3 GetFloorSize() {
		
		if(floorSize == Vector3.zero){
			Transform floor = gameObject.transform.GetChild(0);
			floorSize = floor.GetComponent<BoxCollider>().size;
		}
		return floorSize;
	}

	public void PlaceDoor(int index, GameObject door){

	}
}
