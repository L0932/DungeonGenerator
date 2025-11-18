using UnityEngine;
using plyGame;
using System.Collections.Generic;

[System.Serializable]
public struct Entity {
	public GameObject Prefab;
	public int Count;
	public int Chance;
}

public enum ObjectType {
	None,
	Normal,
	Special,
	Boss
}

public class DRoom : MonoBehaviour {

	public ObjectType roomType;
	public RoomData roomData;
	public bool traps;

	//public int enemyCount;
	//public List<GameObject> Enemies, Items;
	
	public Entity[] Enemies;
	public GameObject boss, itemDisplay;

	//public delegate void EnemyDeath();
	//public static event EnemyDeath OnEnemyKilled;

	
	//public Entity[] Items;
	private ActorClass player;
	private ActorAttribute charge;
	private ClearRoomGUI clearGUI;


	private bool active;
	private int roomNumber, enemyDeathCount;
	private Bounds floorBounds;
	private RoomTrigger trigger;
	private List<DRoom> neighbors;

	private GameObject _enemies; //hierarchy object
	private GameObject _items; //hierarchy object

	private List<Transform> doorTransforms, floorTransforms, torchTransforms, trapTransforms;
	private List<GameObject> Instances, enemyInstances;

	private Transform Entries, Floor, Torches;


	
	void Awake(){

		active = false;

		//Test.OnEnemyKilled += EnemyKilled;
		//DGenerator.OnEnemyKilled += EnemyKilled;

		clearGUI = ScriptManager.ClearRoomScript;
		floorBounds = transform.Find ("Floor").GetComponent<BoxCollider> ().bounds;

		Instances = new List<GameObject>();
		enemyInstances = new List<GameObject>();

		if(ContainsEntries()){
			fillDoorPositions();
		}

		neighbors = new List<DRoom>(4);

		_enemies = new GameObject ("Enemies");
		_enemies.transform.parent = transform;

		_items = new GameObject ("Items");
		_items.transform.parent = transform;
	}


	void Start(){
		RoomSetup();

		//player = Player.Instance.actor.actorClass;//DGenerator.player.GetComponent<ActorClass>();
		//charge = player.GetAttribute ("Charge", plyGameObjectIdentifyingType.screenName);
	}


	void Update(){
		
		if (!player && Player.IsReady) {
			player = Player.Instance.actor.actorClass;
			charge = player.GetAttribute ("Charges", plyGameObjectIdentifyingType.screenName);
		}
	}


	#region public functions



	public void EnemyKilled(){

		enemyDeathCount++;
		//clearGUI.ClearRoom(transform);


		if (enemyDeathCount >= enemyInstances.Count) {
			Debug.Log ("Increasing Charges!!!");

			Debug.Log ("Charge Consumable Value: " + charge.ConsumableValue);
			//Debug.Log ("Charge Base Valie" + charge.
			if(charge.ConsumableValue <= 6){
				//charge.ChangeBaseValueBy(1);
				//charge.SetBaseValue((int)charge.lastValue + 1);
				charge.SetConsumableValue(charge.ConsumableValue + 1, null);

				//if(clearGUI)
					//clearGUI.ClearRoom(transform);

				Debug.Log ("Charge Consumable Value After: " + charge.ConsumableValue);
			}
		}

		Debug.Log ("Enemy killed in room " + roomNumber);
	}

	public void SetRoomActive(bool b){
		active = b;
	}

	public void SetRoomNum(int n){
		roomNumber = n;
	}

	public int GetRoomNum(){
		return roomNumber;
	}
	public Bounds GetFloorBounds(){
		return floorBounds;
	}

	public void AddNeighbor(int idx, DRoom droom){

		neighbors.Insert (idx, droom);
	}


	public void SetActiveGameObjects(bool b){
		if(Instances != null){
			foreach(GameObject obj in Instances){
				if(obj != null){
					obj.SetActive(b);
				}
			}
		}
	}

	public void PlaceDoor(int index, GameObject door){
		if(doorTransforms == null){
			fillDoorPositions();
		}
		
		if(index < doorTransforms.Count){
			
			Vector3 pos = doorTransforms[index].position;
			Quaternion rot = doorTransforms[index].rotation;
			GameObject doorObject = Instantiate (door, pos, rot) as GameObject;

			//doorObject.GetComponentInChildren<DEntry>().SetIndex(index);
			doorObject.transform.parent = transform;
		}
	}
	
	public void AssignParent(Transform t){
		transform.parent = t;
	}

	/*
	public void RoomTrigger(){

		if(itemDisplay)

	}*/

	#endregion

	private void RoomSetup(){

		// Initialize Cells
		foreach (Transform child in transform) {
			if (child.name == "Floor") {
				Floor = child;

				floorTransforms = new List<Transform>();
				trapTransforms = new List<Transform>();

				foreach (Transform cell in Floor) {
					if (cell.gameObject.layer != LayerMask.NameToLayer ("Obstacle")) { 
						if(cell.tag != "Cell-Null")
							floorTransforms.Add (cell); //Cells
					} else {
						trapTransforms.Add (cell); //Obstacle Cells
					}
				}
			}
		}

		//Init Room Trigger Var
		trigger = GetComponentInChildren<RoomTrigger> ();

		if (trigger)
			trigger.SetRoomType (roomType);
		else
			Debug.LogError ("No RoomTrigger attached");

		PopulateRoom();
	}

	private void PopulateRoom(){
		
		if(ContainsFloor()){
			AddEnemies();
			AddItems();
		}

		if(ContainsTorches()){
			//Add to list for Activation
			AddTorches();
		}

		if (traps) {
			AddSpikes ();
		}

		if (boss) {
			AddBoss ();
		}
	}

	private void AddSpikes(){

		GameObject allSpikes = new GameObject ("SpikeTraps");
		allSpikes.transform.parent = transform;

		foreach (Transform cell in trapTransforms) {
			if(cell.tag == "Spikes"){
				GameObject spikes = Instantiate (roomData.trapTypes[0]) as GameObject;
				Transform t_spikes = spikes.transform;

				t_spikes.name = "Spikes";
				t_spikes.position = cell.position;

				t_spikes.parent = allSpikes.transform;

				Instances.Add (spikes);
			}
		}
	}

	//TODO: Rewrite AddItems and AddEnemies to a single function.
	private void AddEnemies() {
		
		if(Enemies == null || Enemies.Length == 0){
			return;
		}
		
		int roll, count, chance;
		GameObject obj, prefab;
		
		for(int i = 0; i < Enemies.Length; i++){
			
			count = Enemies[i].Count;
			chance = Enemies[i].Chance;
			prefab = Enemies[i].Prefab;
			
			for(int j = 0; j < count; j++){
				roll = Random.Range (0, 100);
				if(prefab != null && roll < chance){
					int r = Random.Range (0, floorTransforms.Count);
					obj = Instantiate(prefab, floorTransforms[r].position + new Vector3(0.0f,1f,0.0f), Quaternion.identity) as GameObject;
					obj.SetActive(false);
					obj.transform.parent = _enemies.transform;

					AIPath sc = obj.GetComponent<AIPath>();

					if(sc)
						sc.SetRoomScript(this);

					enemyInstances.Add (obj); // Temp TODO: Change to have one list only

					trigger.SetEnemyCount (enemyInstances.Count);

					Instances.Add (obj); // Temp General list for all 
				}
			}
		}
	}
	
	private void AddItems(){


		if (itemDisplay) {

			GameObject _itemDisplay = Instantiate (itemDisplay, transform.position, itemDisplay.transform.rotation) as GameObject;
			_itemDisplay.transform.parent = transform;

			foreach(Transform itemGroup in _itemDisplay.transform){

				List<Transform> items = new List<Transform>();

				foreach(Transform item in itemGroup){
					items.Add (item);
				}
			
				int r = Random.Range (0, items.Count);

				//items[r].gameObject.SetActive(true);
				Instances.Add (items[r].gameObject);
			}
		}

		/*
		int roll, count, chance;
		GameObject obj, prefab;
		
		for (int i = 0; i < Items.Length; i++) {
			
			roll = Random.Range (0, 100);
			count = Items[i].Count;
			chance = Items[i].Chance;
			prefab = Items[i].Prefab;
			
			for(int j = 0; j < count; j++){
				roll = Random.Range (0, 100);
				if(prefab != null && roll < chance){
					int r = Random.Range (0, floorTransforms.Count);
					obj = Instantiate(prefab, floorTransforms[r].position + new Vector3(0,1,0), Quaternion.identity) as GameObject;
					obj.SetActive(false);
					obj.transform.parent = _items.transform;
					Instances.Add(obj);
				}
			}
		}
		*/
	}
	
	private void AddTorches(){

		fillTorchPositions ();

		foreach (Transform child in torchTransforms) {
			if (child != null) {
				Instances.Add (child.gameObject);
			}
		}
	}

	private void AddBoss(){
		GameObject obj;
		obj = Instantiate (boss, transform.position, Quaternion.identity) as GameObject;
		obj.SetActive (false);
		enemyInstances.Add (obj);
		Instances.Add (obj);
	}

	private bool ContainsEntries(){
		if(Entries)
			return true;

		foreach(Transform child in transform){
			if(child.name == "Entries"){
				Entries = child;
				return true;
			}
		}
		return false;
	}

	private bool ContainsFloor(){
		if(Floor)
			return true;

		foreach(Transform child in transform){
			if(child.name == "Floor"){
				Floor = child;
				fillFloorPositions();
				return true;
			}
		}
		return false;
	}

	private bool ContainsTorches(){
		if(Torches)
			return true;
		
		GameObject torches = Instantiate (roomData.torchTypes [0]) as GameObject;
		
		Torches = torches.transform;
		Torches.name = "Torches";
		
		Torches.position = transform.position;
		Torches.parent = transform;

		return true;
	}



	private void fillDoorPositions(){

		doorTransforms = new List<Transform>();

		foreach(Transform child in Entries){
			doorTransforms.Add(child);
		}
	}
	
	private void fillFloorPositions(){
		floorTransforms = new List<Transform>();

		foreach(Transform child in Floor){
			if(child.gameObject.layer != LayerMask.NameToLayer("Obstacle")){
				if(child.tag != "Cell-Null"){
					floorTransforms.Add(child);
				}
			}
		}
	}

	private void fillTorchPositions(){
		torchTransforms = new List<Transform>();
		
		foreach(Transform child in Torches){

			Transform[] TorchGroup = child.GetComponentsInChildren<Transform>(true);

			foreach(Transform child2 in TorchGroup){
				if(child2.name == "Left_Fire" || child2.name == "Right_Fire"){
					torchTransforms.Add(child2);
				}
			}
		}
	}
}


