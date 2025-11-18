using UnityEngine;
using System.Collections;

public class testscript : MonoBehaviour {

	public Vector3 startPos;
	public GameObject player;

	void Start(){
		startPos = GameObject.Find ("Start Room").transform.position;
		Instantiate(player, startPos, Quaternion.identity);
	}
}
