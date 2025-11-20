using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Pathfinding;
//using UnityEditor;


public class DGenerator : MonoBehaviour 
{	
	public RoomData roomData;
	public Transform spawnPoint;
	
	public int maxRegularRooms;
	public List<List<GameObject>> roomLists;
	
	public static GameObject player;
	//public int roomsPerKey;
	private static GridGraph gridGraph;
	private List<Transform> doorTransforms;
	
	//public GameObject player;

	private int roomInstance;
	private Vector3 startPos;
	private Vector3	camPos;
	private Vector3	newCamPos;
	private int	camSpeed = 10;
	private DGrid roomGrid;
	
	private Vector3[] roomPositions;
	//private GameObject[,] roomArr;
	private DRoom[,] dRooms;
	
	private Transform floor;
	private RoomClass room;

	private static GameObject FogEffect;
	private static BoxCollider boxCol;


	private Bounds combinedBounds;

	private int maxBossRooms = 1;
	private int maxItemRooms = 1;
	private List<int> maxRooms;
	private int totalRooms;

	[HideInInspector]
	public Transform bossRoom;
	public Transform itemRoom;


	int mapSizeX = 10;
	int mapSizeY = 10;
	
	
	void Awake () 
	{
		InitData();
		Generate();
	}

	void Start()
	{
		InitGridGraph();
	}

	public Vector3 GetStartPos()
	{
		return startPos;
	}

	void InitGridGraph()
	{
		if ( AstarPath.active == null ) throw new System.Exception ("There is no AstarPath object in the scene");
		
		AstarData data = AstarPath.active.astarData;
		gridGraph = data.gridGraph;

		if (gridGraph == null)
			Debug.Log ("Grid graph not available");

		AstarPath.active.Scan();
	}
	
	void InitData()
	{
		FogEffect = GameObject.Find ("Fog");

		totalRooms = maxRegularRooms + maxItemRooms + maxBossRooms;
		totalRooms = Mathf.Clamp (totalRooms, 1, mapSizeX * mapSizeY);

        maxRooms = new List<int> ();

		//TODO: make it a public list with named entries. Example, for the inspector: (Boss: 5, Item: 1, etc.)
		maxRooms.Add (0); //Padding to increase size. Temp solution.
		maxRooms.Add (maxRegularRooms);
		maxRooms.Add (maxItemRooms);
		maxRooms.Add (maxBossRooms);

		dRooms = new DRoom[mapSizeX, mapSizeY];
		roomGrid = new DGrid(mapSizeX, mapSizeY);

		roomPositions = new Vector3[totalRooms];
	}
	
	public void Generate() 
	{
		GenerateStartRoom();
		GenerateRooms();
	}
	
	void GenerateStartRoom() 
	{	
		startPos = new Vector3(mapSizeX/2, 0, mapSizeY/2);
		
		roomGrid.PlaceAt(startPos);
		roomPositions[0] = startPos;
		
		int r = Random.Range(0, roomData.GetRoomCountAt (0));
		room = roomData.GetRoomAt(0, r);
		
		Vector3 startWorldPos = Vector3.Scale (startPos, room.floorSize);
		GridCoord pos = DGrid.VecToGridCoord(startPos);

		GameObject roomObject = Instantiate(room.gameObject, startWorldPos, Quaternion.identity) as GameObject;
		dRooms[pos.x, pos.y] = roomObject.GetComponent<DRoom>();

		combinedBounds = new Bounds (roomObject.transform.position, Vector3.one);
		combinedBounds.Encapsulate(dRooms[pos.x, pos.y].GetFloorBounds());

		dRooms [pos.x, pos.y].SetRoomNum (roomInstance);
		dRooms[pos.x, pos.y].AssignParent(transform);

		spawnPoint.transform.position = startWorldPos + new Vector3(0, 5, 0);
	}
	
	void GenerateRooms() 
	{
		VectDir next;

		Vector3 prevRoomPos, nextRoomPos, worldPos;
		GridCoord prevGridPos, nextGridPos;
		
		int currRoomNum = 1;
		int roomGroup = 1;
		int currRoomGroupCount = 0;

		int attempts = 0;
		int maxAttempts = totalRooms * 50; // Arbitrary high number to avoid infinite loops

        while (currRoomNum < totalRooms && attempts < maxAttempts) 
		{
			attempts++;
			roomInstance += 1;

			//Choose random room that has already been placed
			prevRoomPos = roomPositions[Random.Range(0, currRoomNum)];
			
			//Look for unused nearby spots for the picked room
			if(roomGrid.CellsAvailable(prevRoomPos))
			{			
				next = roomGrid.NextRoomPos(prevRoomPos); // pick random unused spots

				nextRoomPos = next.vect;

				if(roomGrid.PlaceAt(nextRoomPos))
				{ 
					//If the grid placement was sucessful, then place rooms in the world.
					if(maxRooms[roomGroup] == 0 || currRoomGroupCount >= maxRooms[roomGroup]){ // roomGroup 1 = regularRooms, roomGroup 2 = ItemRooms, roomGroup 3 = BossRooms
						currRoomGroupCount = 0;
						roomGroup++;
					}

					if(maxRooms[roomGroup] != 0){
						int r = Random.Range(0, roomData.GetRoomCountAt(roomGroup));

						//TODO: Make a list of all the rooms available for each roomGroup. Delete entries afterward to avoid reuse of the rooms in further chapters or levels.
						room = roomData.GetRoomAt(roomGroup, r);
							
						nextGridPos = DGrid.VecToGridCoord(nextRoomPos);
						prevGridPos = DGrid.VecToGridCoord(prevRoomPos);

						worldPos = Vector3.Scale(nextRoomPos, room.floorSize);

						//worldPos = Vector3.Scale (new Vector3(1,0,0), room.floorSize);
						GameObject roomObject = Instantiate(room.gameObject, worldPos, Quaternion.identity) as GameObject;

						DRoom nextDRoom = dRooms[nextGridPos.x, nextGridPos.y] = roomObject.GetComponent<DRoom>();
						
						switch(nextDRoom.roomType)
						{
							case ObjectType.Boss:
							bossRoom = roomObject.transform;
							break;
						case ObjectType.Special: //TODO: Change Special to Item
							itemRoom = roomObject.transform;
							break;
						}
				
						PlaceDoors(prevGridPos, nextGridPos);
						combinedBounds.Encapsulate(nextDRoom.GetFloorBounds());

						nextDRoom.SetRoomNum(roomInstance);
						nextDRoom.AssignParent(transform);

						roomPositions[currRoomNum] = nextRoomPos;
						currRoomGroupCount++;
						currRoomNum++;
					}
				}
			}
		}

		if (attempts >= maxAttempts) 
		{
			Debug.LogWarning ("Max attempts reached during dungeon generation. Generated " + currRoomNum + " out of " + totalRooms + " rooms.");
        }

        boxCol = gameObject.AddComponent<BoxCollider> ();
		boxCol.center = combinedBounds.center - transform.position;
		boxCol.size = combinedBounds.size;
	}
			
	void PlaceDoors(GridCoord prev, GridCoord next)
	{
		GridCoord idx = roomGrid.PolarDirectionIndeces(prev, next);
	
		if(!idx.Equals(GridCoord.Zero))
		{
			//Alpha code. results of idx values Change depending on room orientation. TODO: Get rid of that dependency in later revisions. 
			DRoom roomA = dRooms[prev.x, prev.y];
			DRoom roomB = dRooms[next.x, next.y];

			int AIdx = (int)roomA.roomType;
			int BIdx = (int)roomB.roomType;

			roomA.PlaceDoor(idx.y, roomData.doorTypes[BIdx]); 
			roomB.PlaceDoor(idx.x, roomData.doorTypes[AIdx]); 
		}
	}

	public Transform GetRoomByType(ObjectType type)
	{

		switch (type) {
			case ObjectType.Boss:
				return bossRoom;
			break;
		case ObjectType.Special:
				return itemRoom;
			break;
		}

		return null;
	}

	public static Bounds GetDungeonBounds()
	{
		if (boxCol == null) 
		{
			Debug.LogError ("BoxCollider not initialized in DGenerator.GetDungeonBounds");
			return new Bounds ();
        }

        return boxCol.bounds;
	}

	public static void EnterRoom(Vector3 floorSize, Vector3 center)
	{
		if (gridGraph == null || AstarPath.active == null) 
		{
			Debug.LogError ("Grid graph not initialized in DGenerator.EnterRoom");
			return;
        }

        if (FogEffect) 
		{
			FogEffect.transform.position = center;
		}

		gridGraph.width = (int)floorSize.x;
		gridGraph.depth = (int)floorSize.z;
		gridGraph.center = center;

		gridGraph.UpdateSizeFromWidthDepth();
		AstarPath.active.Scan();
	}
}
