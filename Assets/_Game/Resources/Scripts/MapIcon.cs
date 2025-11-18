using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MapIcon : MonoBehaviour {

	public ObjectType iconType;
	public Transform target;
	public Camera camera;
	
	RectTransform rt;

	// Use this for initialization
	void Start () {
		rt = GetComponent<RectTransform> ();
		target = GameObject.Find ("Dungeon").GetComponent<DGenerator> ().GetRoomByType (iconType); //bossRoom; //DGenerator.bossRoom;
	}
	
	// Update is called once per frame
	void Update () {


		if (target) {

			//Vector3 screenPos = target.InverseTransformPoint (Vector3.zero);//camera.WorldToScreenPoint (target.position);
			//rt.localPosition = new Vector3 (screenPos.x * -1, screenPos.y * -1, 0);

			//Vector3 screenPos = transform.InverseTransformVector (target.position);//camera.WorldToScreenPoint (target.position);
			//rt.position = new Vector3 (screenPos.x, 0, screenPos.z); 
			//Debug.Log ("screenPos: " + screenPos);

			//Vector3 screenPos = camera.transform.InverseTransformPoint (target.position);//camera.WorldToScreenPoint (target.position);
			//rt.localPosition = new Vector3 (screenPos.x, screenPos.y, screenPos.z);

			transform.position = target.position + new Vector3(0, 10, 0);
		}
		//Debug.Log ("rtPos: " + rt.localPosition);

	}
}
