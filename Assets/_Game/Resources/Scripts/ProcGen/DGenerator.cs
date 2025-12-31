using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
//using UnityEditor;

using Debug = UnityEngine.Debug;

public class DGenerator : MonoBehaviour
{
    public RoomData roomData;
    public Transform spawnPoint;

    public int maxRegularRooms;
    public List<List<GameObject>> roomLists;

    public static GameObject player;
    private static GridGraph gridGraph;
    private List<Transform> doorTransforms;

    private int roomInstance;
    private Vector3 startPos;
    private Vector3 camPos;
    private Vector3 newCamPos;
    private int camSpeed = 10;
    private DGrid roomGrid;

    private Vector3[] roomPositions;
    private DRoom[,] dRooms;

    private Transform floor;
    private RoomClass room;

    private static GameObject FogEffect;
    private static BoxCollider boxCol;

    private Bounds combinedBounds;

    private int maxBossRooms = 1;
    private int maxItemRooms = 1;
    private List<int> maxRooms;

    public int TotalRooms { get { return totalRooms; } }
    private int totalRooms;

    [HideInInspector]
    public Transform bossRoom;
    [HideInInspector]
    public Transform itemRoom;

    int mapSizeX = 10;
    int mapSizeY = 10;

    // track which grid cells actually got rooms
    private int minGridX, maxGridX, minGridY, maxGridY;
    private bool hasAnyRoom;

    // world-space grid cell size (X/Z)
    private Vector3 cellSize;

    // for Y of dungeon bounds
    private float floorCenterY;
    private float floorHeight = 5f; // fallback

    // Door side indices: must match DRoom.PlaceDoor ordering
    private const int NORTH = 0;
    private const int EAST = 1;
    private const int SOUTH = 2;
    private const int WEST = 3;

    void Awake()
    {
        InitData();
        Generate();
    }

    void Start()
    {
        //InitGridGraph();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            RegenerateDungeon();
        }
    }

    public Vector3 GetStartPos()
    {
        return startPos;
    }

    //void InitGridGraph()
    //{
    //    if (AstarPath.active == null)
    //        throw new System.Exception("There is no AstarPath object in the scene");

    //    AstarData data = AstarPath.active.astarData;
    //    gridGraph = data.gridGraph;

    //    if (gridGraph == null)
    //        UnityEngine.Debug.Log("Grid graph not available");

    //    AstarPath.active.Scan();
    //}

    void InitData()
    {
        FogEffect = GameObject.Find("Fog");

        totalRooms = maxRegularRooms + maxItemRooms + maxBossRooms;
        totalRooms = Mathf.Clamp(totalRooms, 1, mapSizeX * mapSizeY);

        maxRooms = new List<int>();

        // TODO: make it a public list with named entries.
        maxRooms.Add(0); // padding / dummy index 0
        maxRooms.Add(maxRegularRooms);
        maxRooms.Add(maxItemRooms);
        maxRooms.Add(maxBossRooms);

        dRooms = new DRoom[mapSizeX, mapSizeY];
        roomGrid = new DGrid(mapSizeX, mapSizeY);

        roomPositions = new Vector3[totalRooms];

        // reset grid bounds tracking
        minGridX = int.MaxValue;
        maxGridX = int.MinValue;
        minGridY = int.MaxValue;
        maxGridY = int.MinValue;
        hasAnyRoom = false;

        cellSize = Vector3.zero;
        floorCenterY = 0f;
        floorHeight = 5f;
    }

    public void Generate()
    {
        GenerateStartRoom();
        GenerateRooms();
    }

    void GenerateStartRoom()
    {
        // logical grid coord (center of grid)
        startPos = new Vector3(mapSizeX / 2, 0, mapSizeY / 2);

        roomGrid.PlaceAt(startPos);
        roomPositions[0] = startPos;

        int r = UnityEngine.Random.Range(0, roomData.GetRoomCountAt(0));
        room = roomData.GetRoomAt(0, r);

        GridCoord pos = DGrid.VecToGridCoord(startPos);

        // instantiate at origin first so we can measure it
        Vector3 tempPos = Vector3.zero;
        GameObject roomObject = Instantiate(room.gameObject, tempPos, Quaternion.identity) as GameObject;
        DRoom dRoom = roomObject.GetComponent<DRoom>();
        dRooms[pos.x, pos.y] = dRoom;

        // get actual floor bounds and use them as our cell size
        Bounds floorBounds = dRoom.GetFloorBounds(); // must be WORLD-space
        cellSize = new Vector3(floorBounds.size.x, 0f, floorBounds.size.z);

        floorCenterY = floorBounds.center.y;
        floorHeight = floorBounds.size.y > 0f ? floorBounds.size.y : 5f;

        // now place the start room based on grid and real cell size
        Vector3 startWorldPos = new Vector3(
            startPos.x * cellSize.x,
            0f,
            startPos.z * cellSize.z
        );
        roomObject.transform.position = startWorldPos;

        dRoom.SetRoomNum(roomInstance);
        dRoom.AssignParent(transform);

        spawnPoint.transform.position = startWorldPos + new Vector3(0, 5, 0);

        // register grid occupancy
        RegisterRoomAt(pos);
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
        int maxAttempts = totalRooms * 50; // arbitrary high number to avoid infinite loops

        while (currRoomNum < totalRooms && attempts < maxAttempts)
        {
            attempts++;
            roomInstance += 1;

            // choose random room that has already been placed
            prevRoomPos = roomPositions[UnityEngine.Random.Range(0, currRoomNum)];

            // look for unused nearby spots for the picked room
            if (roomGrid.CellsAvailable(prevRoomPos))
            {
                next = roomGrid.NextRoomPos(prevRoomPos); // pick random unused spots

                if (next.vect.x < 0f)  // means no valid neighbor
                    continue;

                nextRoomPos = next.vect;

                if (roomGrid.PlaceAt(nextRoomPos))
                {
                    // if the grid placement was successful, then place rooms in the world.
                    if (maxRooms[roomGroup] == 0 || currRoomGroupCount >= maxRooms[roomGroup])
                    {
                        // roomGroup 1 = regularRooms, roomGroup 2 = ItemRooms, roomGroup 3 = BossRooms
                        currRoomGroupCount = 0;
                        roomGroup++;
                    }

                    if (roomGroup < maxRooms.Count && maxRooms[roomGroup] != 0)
                    {
                        int r = UnityEngine.Random.Range(0, roomData.GetRoomCountAt(roomGroup));

                        // TODO: make a list of all the rooms available for each roomGroup.
                        room = roomData.GetRoomAt(roomGroup, r);

                        nextGridPos = DGrid.VecToGridCoord(nextRoomPos);
                        prevGridPos = DGrid.VecToGridCoord(prevRoomPos);

                        worldPos = new Vector3(
                            nextRoomPos.x * cellSize.x,
                            0f,
                            nextRoomPos.z * cellSize.z
                        );

                        GameObject roomObject = Instantiate(room.gameObject, worldPos, Quaternion.identity) as GameObject;

                        DRoom nextDRoom = dRooms[nextGridPos.x, nextGridPos.y] = roomObject.GetComponent<DRoom>();

                        switch (nextDRoom.roomType)
                        {
                            case ObjectType.Boss:
                                bossRoom = roomObject.transform;
                                break;
                            case ObjectType.Special: // TODO: Change Special to Item
                                itemRoom = roomObject.transform;
                                break;
                        }

                        PlaceDoors(prevGridPos, nextGridPos);

                        nextDRoom.SetRoomNum(roomInstance);
                        nextDRoom.AssignParent(transform);

                        roomPositions[currRoomNum] = nextRoomPos;
                        currRoomGroupCount++;
                        currRoomNum++;

                        // register grid occupancy
                        RegisterRoomAt(nextGridPos);
                    }
                }
            }
        }

        if (attempts >= maxAttempts)
        {
            UnityEngine.Debug.LogWarning(
                "Max attempts reached during dungeon generation. Generated "
                + currRoomNum + " out of " + totalRooms + " rooms."
            );
        }

        RebuildDungeonBounds();
    }

    public void RegenerateDungeon()
    {
        // clear existing dungeon
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // reset data
        roomInstance = 0;
        InitData();

        // generate new dungeon
        Generate();

        // update A* graph
        //InitGridGraph();

        UnityEngine.Debug.Log("Dungeon regenerated!");
    }

    void RebuildDungeonBounds()
    {
        if (!hasAnyRoom)
        {
            Debug.LogWarning("RebuildDungeonBounds: no rooms registered on grid");
            return;
        }

        if (cellSize == Vector3.zero)
        {
            Debug.LogWarning("RebuildDungeonBounds: cellSize is zero, using fallback");
            cellSize = new Vector3(10f, 0f, 10f);
        }

        // how many cells are actually used
        int cellsX = (maxGridX - minGridX + 1);
        int cellsY = (maxGridY - minGridY + 1);

        float width = cellsX * cellSize.x;
        float depth = cellsY * cellSize.z;
        float height = Mathf.Max(floorHeight, 5f);

        // correct center index (no +1)
        float centerGridX = (minGridX + maxGridX) * 0.5f;
        float centerGridY = (minGridY + maxGridY) * 0.5f;

        Vector3 center = new Vector3(
            centerGridX * cellSize.x,
            floorCenterY,
            centerGridY * cellSize.z
        );

        combinedBounds = new Bounds(center, new Vector3(width, height, depth));

        if (boxCol == null)
            boxCol = gameObject.AddComponent<BoxCollider>();

        boxCol.gameObject.layer = LayerMask.NameToLayer("DungeonBounds");
        boxCol.center = transform.InverseTransformPoint(combinedBounds.center);
        boxCol.size = combinedBounds.size;

        Debug.Log(
            $"RebuildDungeonBounds (grid-based): center={combinedBounds.center}, size={combinedBounds.size}, " +
            $"cellsX={cellsX}, cellsY={cellsY}"
        );

        CameraLookDown camScript = Camera.main != null
            ? Camera.main.GetComponent<CameraLookDown>()
            : null;

        if (camScript != null)
            camScript.SetTarget(boxCol);
    }

    void RegisterRoomAt(GridCoord gridPos)
    {
        if (!hasAnyRoom)
        {
            minGridX = maxGridX = gridPos.x;
            minGridY = maxGridY = gridPos.y;
            hasAnyRoom = true;
            return;
        }

        if (gridPos.x < minGridX) minGridX = gridPos.x;
        if (gridPos.x > maxGridX) maxGridX = gridPos.x;
        if (gridPos.y < minGridY) minGridY = gridPos.y;
        if (gridPos.y > maxGridY) maxGridY = gridPos.y;
    }

    void PlaceDoors(GridCoord prev, GridCoord next)
    {
        DRoom roomA = dRooms[prev.x, prev.y];
        DRoom roomB = dRooms[next.x, next.y];

        if (roomA == null || roomB == null)
        {
            Debug.LogWarning("PlaceDoors called with null room(s).");
            return;
        }

        // Direction from A to B in grid space
        int dx = next.x - prev.x;
        int dy = next.y - prev.y;

        // Only allow direct neighbors
        if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1)
        {
            Debug.LogWarning($"PlaceDoors: rooms at {prev.x},{prev.y} and {next.x},{next.y} are not 4-way neighbors.");
            return;
        }

        int sideA = -1;
        int sideB = -1;

        if (dx == 1)        // B is east of A
        {
            sideA = EAST;
            sideB = WEST;
        }
        else if (dx == -1)  // B is west of A
        {
            sideA = WEST;
            sideB = EAST;
        }
        else if (dy == 1)   // B is north of A
        {
            sideA = NORTH;
            sideB = SOUTH;
        }
        else if (dy == -1)  // B is south of A
        {
            sideA = SOUTH;
            sideB = NORTH;
        }

        if (sideA < 0 || sideB < 0)
        {
            Debug.LogWarning("PlaceDoors: could not determine sides.");
            return;
        }

        // Choose door prefab based on the OTHER room’s type (like you were doing)
        int typeA = (int)roomA.roomType;
        int typeB = (int)roomB.roomType;

        GameObject doorForA = roomData.doorTypes[typeB];
        GameObject doorForB = roomData.doorTypes[typeA];

        roomA.PlaceDoor(sideA, doorForA);
        roomB.PlaceDoor(sideB, doorForB);
    }

    public Transform GetRoomByType(ObjectType type)
    {
        switch (type)
        {
            case ObjectType.Boss:
                return bossRoom;
            case ObjectType.Special:
                return itemRoom;
        }

        return null;
    }

    public static Bounds GetDungeonBounds()
    {
        if (boxCol == null)
        {
            UnityEngine.Debug.LogError("BoxCollider not initialized in DGenerator.GetDungeonBounds");
            return new Bounds();
        }

        return boxCol.bounds;
    }

    public static void EnterRoom(Vector3 floorSize, Vector3 center)
    {
        //if (gridGraph == null || AstarPath.active == null)
        //{
        //    UnityEngine.Debug.LogError("Grid graph not initialized in DGenerator.EnterRoom");
        //    return;
        //}

        if (FogEffect)
        {
            FogEffect.transform.position = center;
        }

        gridGraph.width = (int)floorSize.x;
        gridGraph.depth = (int)floorSize.z;
        gridGraph.center = center;

        gridGraph.UpdateSizeFromWidthDepth();
        //AstarPath.active.Scan();
    }
}
