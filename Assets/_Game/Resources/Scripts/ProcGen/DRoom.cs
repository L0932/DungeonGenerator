using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using Debug = UnityEngine.Debug;

[System.Serializable]
public struct Entity 
{
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

public class DRoom : MonoBehaviour 
{
    private const int NORTH = 0;
    private const int EAST = 1;
    private const int SOUTH = 2;
    private const int WEST = 3;

    public ObjectType roomType;
	public RoomData roomData;
	public bool traps;

	public Entity[] Enemies;
	public GameObject boss, itemDisplay;

	private ClearRoomGUI clearGUI;

	private bool active;
	private int roomNumber, enemyDeathCount;
	private Bounds floorBounds;
	private RoomTrigger trigger;
	private List<DRoom> neighbors;

	private GameObject _enemies; //hierarchy object
	private GameObject _items; //hierarchy object

    // fixed array of 4 anchors [N,E,S,W]
    private Transform[] doorAnchors;
    private List<Transform> doorTransforms, floorTransforms, torchTransforms, trapTransforms;
	private List<GameObject> Instances, enemyInstances;

	private Transform Entries, Floor, Torches;

	void Awake()
	{
		active = false;

		clearGUI = ScriptManager.ClearRoomScript;
		floorBounds = transform.Find ("Floor").GetComponent<BoxCollider> ().bounds;

		Instances = new List<GameObject>();
		enemyInstances = new List<GameObject>();

		if(ContainsEntries())
		{
			fillDoorPositions();
		}

		neighbors = new List<DRoom>(4);

		_enemies = new GameObject ("Enemies");
		_enemies.transform.parent = transform;

		_items = new GameObject ("Items");
		_items.transform.parent = transform;
	}


	void Start()
	{
		RoomSetup();
	}

	#region public functions

	public void SetRoomActive(bool b)
	{
		active = b;
	}

	public void SetRoomNum(int n)
	{
		roomNumber = n;
	}

	public int GetRoomNum()
	{
		return roomNumber;
	}
	public Bounds GetFloorBounds()
	{
		return floorBounds;
	}

	public void AddNeighbor(int idx, DRoom droom)
	{
		neighbors.Insert (idx, droom);
	}


	public void SetActiveGameObjects(bool b)
	{
		if(Instances != null)
		{
			foreach(GameObject obj in Instances)
			{
				if(obj != null)
				{
					obj.SetActive(b);
				}
			}
		}
	}

    /// <summary>
    /// Place a door on a given side:
    /// 0 = North, 1 = East, 2 = South, 3 = West
    /// </summary>
    public void PlaceDoor(int index, GameObject door)
    {
        if (door == null) return;

        if (doorAnchors == null || doorAnchors.Length == 0)
        {
            fillDoorPositions();
        }

        if (doorAnchors == null || index < 0 || index >= doorAnchors.Length)
        {
            Debug.LogWarning($"DRoom.PlaceDoor: invalid index {index} on {name}");
            return;
        }

        Transform anchor = doorAnchors[index];
        if (anchor == null)
        {
            Debug.LogWarning($"DRoom.PlaceDoor: no anchor for index {index} (room {name})");
            return;
        }

        Vector3 pos = anchor.position;
        Quaternion rot = anchor.rotation;
        GameObject doorObject = Instantiate(door, pos, rot) as GameObject;

        doorObject.transform.parent = transform;
    }

    public void AssignParent(Transform t)
	{
		transform.parent = t;
	}

	#endregion

	private void RoomSetup()
	{
		// Initialize Cells
		foreach (Transform child in transform) 
		{
			if (child.name == "Floor") 
			{
				Floor = child;

				floorTransforms = new List<Transform>();
				trapTransforms = new List<Transform>();

				foreach (Transform cell in Floor) {
					if (cell.gameObject.layer != LayerMask.NameToLayer ("Obstacle")) 
					{ 
						if(cell.tag != "Cell-Null")
							floorTransforms.Add (cell); //Cells
					} else {
						trapTransforms.Add (cell); //Obstacle Cells
					}
				}
			}
		}

		//Init Room Trigger Var
		//trigger = GetComponentInChildren<RoomTrigger> ();

		//if (trigger)
		//	trigger.SetRoomType (roomType);
		//else
		//	Debug.LogError ("No RoomTrigger attached");

		// PopulateRoom();
	}

	private void PopulateRoom()
	{	
		if(ContainsFloor()){
			AddEnemies();
			AddItems();
		}

		if(ContainsTorches())
		{
			//Add to list for Activation
			AddTorches();
		}

		if (traps)
		{
			AddSpikes();
		}

		if (boss)
		{
			AddBoss();
		}
	}

	private void AddSpikes()
	{

		GameObject allSpikes = new GameObject ("SpikeTraps");
		allSpikes.transform.parent = transform;

		foreach (Transform cell in trapTransforms) 
		{
			if(cell.tag == "Spikes")
			{
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
	private void AddEnemies() 
	{
		if(Enemies == null || Enemies.Length == 0)
		{
			return;
		}
		
		int roll, count, chance;
		GameObject obj, prefab;
		
		for(int i = 0; i < Enemies.Length; i++)
		{	
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
	
	private void AddItems()
	{
		if (itemDisplay) {

			GameObject _itemDisplay = Instantiate (itemDisplay, transform.position, itemDisplay.transform.rotation) as GameObject;
			_itemDisplay.transform.parent = transform;

			foreach(Transform itemGroup in _itemDisplay.transform){

				List<Transform> items = new List<Transform>();

				foreach(Transform item in itemGroup){
					items.Add (item);
				}
			
				int r = Random.Range (0, items.Count);

				Instances.Add (items[r].gameObject);
			}
		}
	}
	
	private void AddTorches()
	{

		fillTorchPositions ();

		foreach (Transform child in torchTransforms) {
			if (child != null) {
				Instances.Add (child.gameObject);
			}
		}
	}

	private void AddBoss()
	{
		GameObject obj;
		obj = Instantiate (boss, transform.position, Quaternion.identity) as GameObject;
		obj.SetActive (false);
		enemyInstances.Add (obj);
		Instances.Add (obj);
	}

	private bool ContainsEntries()
	{
		if(Entries)
			return true;

		foreach(Transform child in transform)
		{
			if(child.name == "Entries"){
				Entries = child;
				return true;
			}
		}
		return false;
	}

	private bool ContainsFloor()
	{
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

	private bool ContainsTorches()
	{
		if(Torches)
			return true;

        GameObject torches = null;

        if (roomData != null &&
			roomData.torchTypes != null &&
			roomData.torchTypes.Count > 0 &&
			roomData.torchTypes[0] != null)
        {
            torches = Instantiate(roomData.torchTypes[0]);
        }
        else
        {
            Debug.LogWarning("Torch prefab missing on roomData: " + roomData);
        }

		Torches = torches.transform;
		Torches.name = "Torches";
		
		Torches.position = transform.position;
		Torches.parent = transform;

		return true;
	}

    /// <summary>
    /// Build doorAnchors[0..3] = N,E,S,W based on Entries children positions.
    /// </summary>
    private void fillDoorPositions()
    {
        if (Entries == null)
            return;

        // Ensure array exists
        if (doorAnchors == null || doorAnchors.Length != 4)
            doorAnchors = new Transform[4];

        // Clear previous references
        for (int i = 0; i < doorAnchors.Length; i++)
            doorAnchors[i] = null;

        // Use the Floor collider's center as "true" room center
        BoxCollider floorCol = null;
        if (Floor == null)
        {
            Transform floorTf = transform.Find("Floor");
            if (floorTf != null)
                Floor = floorTf;
        }
        if (Floor != null)
            floorCol = Floor.GetComponent<BoxCollider>();

        Vector3 roomCenter = (floorCol != null)
            ? floorCol.bounds.center
            : transform.position;

        foreach (Transform child in Entries)
        {
            Vector3 offset = child.position - roomCenter;
            Vector2 flat = new Vector2(offset.x, offset.z);

            if (flat.sqrMagnitude < 0.0001f)
                continue;

            // Decide whether this is more horizontal (E/W) or vertical (N/S)
            if (Mathf.Abs(flat.x) > Mathf.Abs(flat.y))
            {
                // East / West
                if (flat.x > 0f)
                {
                    if (doorAnchors[EAST] == null)
                        doorAnchors[EAST] = child;
                }
                else
                {
                    if (doorAnchors[WEST] == null)
                        doorAnchors[WEST] = child;
                }
            }
            else
            {
                // North / South
                if (flat.y > 0f)
                {
                    if (doorAnchors[NORTH] == null)
                        doorAnchors[NORTH] = child;
                }
                else
                {
                    if (doorAnchors[SOUTH] == null)
                        doorAnchors[SOUTH] = child;
                }
            }
        }
    }


    private void fillFloorPositions()
	{
		floorTransforms = new List<Transform>();

		foreach(Transform child in Floor){
			if(child.gameObject.layer != LayerMask.NameToLayer("Obstacle")){
				if(child.tag != "Cell-Null"){
					floorTransforms.Add(child);
				}
			}
		}
	}

	private void fillTorchPositions()
	{
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

    void OnDrawGizmosSelected()
    {
        // In edit mode, Entries might not be cached yet
        if (Entries == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name == "Entries")
                {
                    Entries = child;
                    break;
                }
            }
        }

        // Make sure anchors are built
        if (doorAnchors == null || doorAnchors.Length != 4)
            fillDoorPositions();

        if (doorAnchors == null)
            return;

        // Get "true" center again from Floor collider
        BoxCollider floorCol = null;
        if (Floor == null)
        {
            Transform floorTf = transform.Find("Floor");
            if (floorTf != null)
                Floor = floorTf;
        }
        if (Floor != null)
            floorCol = Floor.GetComponent<BoxCollider>();

        Vector3 roomCenter = (floorCol != null)
            ? floorCol.bounds.center
            : transform.position;

        Color[] colors = new Color[4] {
        Color.green,   // North
        Color.blue,    // East
        Color.red,     // South
        Color.yellow   // West
    };

        for (int i = 0; i < 4; i++)
        {
            Transform a = doorAnchors[i];
            if (a == null) continue;

            Gizmos.color = colors[i];
            Gizmos.DrawSphere(a.position, 0.25f);
            Gizmos.DrawLine(roomCenter, a.position);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            a.position + Vector3.up * 0.3f,
            i == 0 ? "NORTH (0)" :
            i == 1 ? "EAST (1)" :
            i == 2 ? "SOUTH (2)" :
                     "WEST (3)"
        );
#endif
        }
    }
}


