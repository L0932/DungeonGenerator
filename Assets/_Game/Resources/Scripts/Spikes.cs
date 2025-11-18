using UnityEngine;
using System.Collections;
using plyGame;

public class Spikes : MonoBehaviour {

	// Use this for initialization
	//public float speed;

	public int Damage;

	private float startTime;
	private float journeyLength;

	private Vector3 startPoint;
	private Vector3 endPoint;
	private bool moveDown;

	private ActorClass player;
	private ActorAttribute health;
	private ActorAttribute barrier;


	void Start () {
		//speed = 0.01f;
		//StartCoroutine(InitPlayerVars ());

		moveDown = true;
		startTime = Time.time;
		startPoint = transform.position;
		endPoint = transform.position + new Vector3 (0, -1.5f, 0);
		journeyLength = Vector3.Distance(transform.position, endPoint);
	}

	IEnumerator InitPlayerVars(){
		yield return new WaitForSeconds (3f);
		if (Player.IsReady) {
			player = Player.Instance.actor.actorClass;
			health = player.GetAttribute ("Health", plyGameObjectIdentifyingType.screenName);
			barrier = player.GetAttribute ("Barrier", plyGameObjectIdentifyingType.screenName);
		}

	}

	// Update is called once per frame
	void Update () {
		//transform.Translate(new Vector3(0,-1,0) * Time.deltaTime, Space.World);
		if (moveDown) {
			MoveToDistance(endPoint, 0.05f);

		} else {
			MoveToDistance (startPoint, 2f);
		}
	}

	private void MoveToDistance(Vector3 pos, float spd){

		float distCovered = (Time.time - startTime) * spd;
		float fracJourney = distCovered / journeyLength;

		transform.position = Vector3.Lerp (transform.position, pos, fracJourney);

		if (transform.position == endPoint) {
			moveDown = false;
			startTime = Time.time;
			journeyLength = Vector3.Distance(transform.position, startPoint);
		}
		else if (transform.position == startPoint) {
			moveDown = true;
			startTime = Time.time;
			journeyLength = Vector3.Distance(transform.position, endPoint);
		}
	}

	void OnTriggerEnter(Collider col){
		if (col.tag == "Player") {

			if (!player) {
				player = col.gameObject.GetComponent<Actor> ().actorClass;
				health = player.GetAttribute ("Health", plyGameObjectIdentifyingType.screenName);
				barrier = player.GetAttribute ("Barrier", plyGameObjectIdentifyingType.screenName);
			}

			//Debug.Log ("Barrier: " + barrier.ConsumableValue);
			//Debug.Log ("Health: " + health.ConsumableValue);

			//Debug.Log ("Player Spiked");

			if (health.ConsumableValue > barrier.ConsumableValue) {
				//health.ChangeBaseValueBy(Damage * -1);
				health.SetConsumableValue (health.ConsumableValue - Damage, null);
			} else {
				barrier.SetConsumableValue (barrier.ConsumableValue - Damage, null);
			}
		}
	}
}
