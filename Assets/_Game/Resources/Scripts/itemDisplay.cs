using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]

public class ItemList{
	public System.String type;
	public GameObject highlight;
	public List<GameObject> items;
}

public class itemDisplay : MonoBehaviour {

	//[SerializeField]
	public List<ItemList> itemList; 
	private List<Transform> transforms;

	//public Transform player;
	// Use this for initialization
	void Awake() {
		//player = GameObject.Find ("Player").transform;
		transforms = new List<Transform> ();

		foreach (Transform t in transform) {
			transforms.Add (t);
		}

		ItemSetup ();
	}


	void ItemSetup(){
		if (itemList.Count != transforms.Count)
			return;

		for (int i = 0; i < itemList.Count; i++) {
			int r = Random.Range (0, itemList[i].items.Count);

			Vector3 offset = new Vector3(0, 1, 0);
			//Vector3 highlightYOffset = new Vector3(0f,2f,0f);

			GameObject obj = Instantiate(itemList[i].items[r], transforms[i].position + offset, Quaternion.identity) as GameObject;
			Vector3 hPos = transforms[i].position;//Vector3.Scale (obj.transform.position, new Vector3(1,0,1)) + highlightYOffset;
			GameObject child = Instantiate(itemList[i].highlight, new Vector3(hPos.x, .5f, hPos.z), Quaternion.identity) as GameObject;

			obj.transform.parent = transforms[i];
			child.transform.parent = obj.transform;
			
		}
	}

	/*
	// Update is called once per frame
	void Update () {

		if(player != null)
		{
			var rotationAngle = Quaternion.LookRotation ( player.position - transform.position); // we get the angle has to be rotated
			transform.rotation = Quaternion.Slerp ( transform.rotation, rotationAngle, Time.deltaTime * 2);
			//transform.LookAt(player);
		}
	}
	*/
}
