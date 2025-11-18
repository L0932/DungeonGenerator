using UnityEngine;
using System.Collections;

public enum TriggerType
{
	Transport,
	Animator,
}

public class DEntry : MonoBehaviour {

	public TriggerType trigger;
	public GameObject dest;
	//public GameObject model;
	private Animator controller;
	private bool open;
	private int index;

	void Awake(){

		if (trigger == TriggerType.Animator) {
			controller = GetComponentInChildren<Animator> ();
		}
		//GameObject door = Instantiate (model, transform.position, Quaternion.identity) as GameObject;
		open = false;

	}

	public void SetIndex(int val){
		index = val;
	}

	void OnTriggerEnter(Collider col){

		if (col.tag == "Player") {
			if (trigger == TriggerType.Transport) {
				col.transform.position = dest.transform.position;
			}else if(trigger == TriggerType.Animator){
				controller.SetBool("Open", true);
				open = true;
			}
		}
	}

	void OnTriggerStay(Collider col){
	
	if (col.tag == "Player") {
			if (!open) {
				controller.SetBool ("Open", true);
				open = true;
			}
		}
	}

	void OnTriggerExit(Collider col){

		StartCoroutine(PlayAnim());
	}

	IEnumerator PlayAnim(){

		yield return new WaitForSeconds(1f);

		if(trigger == TriggerType.Animator){
			controller.SetBool("Open", false);
			open = false;
		}
	}
}
