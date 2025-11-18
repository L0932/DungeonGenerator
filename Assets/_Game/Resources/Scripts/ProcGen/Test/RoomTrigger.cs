using UnityEngine;
using System.Collections;

public class RoomTrigger : MonoBehaviour {

	// Use this for initialization
	private DRoom dRoomScript;

	private int roomNum, enemyCount;
	private ObjectType roomType;

	private bool triggered;
	private Transform parent;
	private BoxCollider collider;

	void Start () {
		InitValues();

	}
	
	// Update is called once per frame
	void Update () {

		if (isActiveAndEnabled) {
			//Debug.Log ("Enemy Count: " + e
		}

		//Debug.Log ("Trigger script updating");

	
	}

	void OnTriggerEnter(Collider col){

		if(col.tag == "Player" && !triggered){
			triggered = true;

			//Debug.Log ("Room Type Entered: " + roomType);
			//Debug.Log ("Room Instance Entered: " + roomNum);
			dRoomScript.SetRoomActive (true);
			Debug.Log ("Room enemies :" + enemyCount);
			/*
			if(roomType == ObjectType.Special){

			}*/
			
			if(ContainsParent()){
				DGenerator.EnterRoom(collider.size, transform.position);
				if(dRoomScript != null){
					dRoomScript.SetActiveGameObjects(true);
				}
				else{
					Debug.LogError("A room does not have a DRoom script attached");
				}
			}
		}
	}

	void OnTriggerExit(Collider col){
		triggered = false;

		if(col.tag == "Player"){

			dRoomScript.SetRoomActive (false);

			if(dRoomScript != null){
				dRoomScript.SetActiveGameObjects(false);
			}
			else{
				Debug.LogError("A room does not have a DRoom script attached");
			}
		}
	}

	public void SetRoomType(ObjectType type){
		roomType = type;
	}

	public void SetEnemyCount(int num){
		enemyCount = num;
	}

	private void InitValues(){
		//roomNum = dRoomScript.GetRoomNum ();
		triggered = false;
		parent = gameObject.transform.parent;
		dRoomScript = parent.GetComponent<DRoom>();
		collider = transform.GetComponent<BoxCollider>();
	}

	private bool ContainsParent(){
		if(parent)
			return true;

		parent = gameObject.transform.parent;
		dRoomScript = parent.GetComponent<DRoom>();

		if(parent != null)
			return true;

		return false;
	}
}
