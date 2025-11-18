using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using DunGen.Graph;

namespace DunGen
{
	public class Dungeon : MonoBehaviour
	{
		public DungeonFlow DungeonFlow { get; protected set; }
        public bool DebugRender = false;

        public ReadOnlyCollection<Tile> AllTiles { get; private set; }
        public ReadOnlyCollection<Tile> MainPathTiles { get; private set; }
        public ReadOnlyCollection<Tile> BranchPathTiles { get; private set; }
        public ReadOnlyCollection<GameObject> Doors { get; private set; }
        public ReadOnlyCollection<DoorwayConnection> Connections { get; private set; }
        public DungeonGraph ConnectionGraph { get; private set; }

		private readonly List<Tile> allTiles = new List<Tile>();
        private readonly List<Tile> mainPathTiles = new List<Tile>();
        private readonly List<Tile> branchPathTiles = new List<Tile>();
        private readonly List<GameObject> doors = new List<GameObject>();
        private readonly List<DoorwayConnection> connections = new List<DoorwayConnection>();


		internal void PreGenerateDungeon(DungeonGenerator dungeonGenerator)
		{
			DungeonFlow = dungeonGenerator.DungeonFlow;

            AllTiles = new ReadOnlyCollection<Tile>(new Tile[0]);
            MainPathTiles = new ReadOnlyCollection<Tile>(new Tile[0]);
            BranchPathTiles = new ReadOnlyCollection<Tile>(new Tile[0]);
            Doors = new ReadOnlyCollection<GameObject>(new GameObject[0]);
            Connections = new ReadOnlyCollection<DoorwayConnection>(new DoorwayConnection[0]);
		}

        internal void PostGenerateDungeon(DungeonGenerator dungeonGenerator)
        {
            ConnectionGraph = new DungeonGraph(this);
        }

		public void Clear()
		{
			foreach(var tile in allTiles)
		        GameObject.DestroyImmediate(tile.gameObject);

			allTiles.Clear();
			mainPathTiles.Clear();
			branchPathTiles.Clear();
            doors.Clear();
            connections.Clear();

            ExposeRoomProperties();
		}

        internal void MakeConnection(Doorway a, Doorway b, System.Random randomStream)
        {
            var conn = new DoorwayConnection(a, b);

            a.Tile.Placement.UnusedDoorways.Remove(a);
            a.Tile.Placement.UsedDoorways.Add(a);

            b.Tile.Placement.UnusedDoorways.Remove(b);
            b.Tile.Placement.UsedDoorways.Add(b);

            connections.Add(conn);

            // Add door prefab
            List<GameObject> doorPrefabs = (a.DoorPrefabs.Count > 0) ? a.DoorPrefabs : b.DoorPrefabs;
            
            if (doorPrefabs.Count > 0 && !(a.HasDoorPrefab || b.HasDoorPrefab))
            {
                GameObject doorPrefab = doorPrefabs[randomStream.Next(0, doorPrefabs.Count)];

                if (doorPrefab != null)
                {
                    GameObject door = (GameObject)GameObject.Instantiate(doorPrefab);
                    door.transform.position = a.transform.position;
                    door.transform.rotation = a.transform.rotation;
                    door.transform.localScale = a.transform.localScale;

                    door.transform.parent = a.transform;
                    doors.Add(door);

                    a.SetUsedPrefab(door);
                    b.SetUsedPrefab(door);
                }
            }
        }

        internal void AddTile(Tile tile)
        {
            allTiles.Add(tile);

            if (tile.Placement.IsOnMainPath)
                mainPathTiles.Add(tile);
            else
                branchPathTiles.Add(tile);
        }

        internal void RemoveLastConnection()
        {
            var conn = connections.Last();

            Doorway a = conn.A;
            Doorway b = conn.B;

            a.RemoveUsedPrefab();
            b.RemoveUsedPrefab();

            a.Tile.Placement.UnusedDoorways.Add(a);
            a.Tile.Placement.UsedDoorways.Remove(a);

            b.Tile.Placement.UnusedDoorways.Add(b);
            b.Tile.Placement.UsedDoorways.Remove(b);

            //var door = doors.Last();
            if (doors.Count > 0)
                doors.RemoveAt(doors.Count - 1);

            connections.Remove(conn);
        }

        internal void RemoveTile(Tile tile)
        {
            if (allTiles.Contains(tile))
                allTiles.Remove(tile);

            if (mainPathTiles.Contains(tile))
                mainPathTiles.Remove(tile);

            if (branchPathTiles.Contains(tile))
                branchPathTiles.Remove(tile);

            Destroy(tile.gameObject);
        }

        internal void ExposeRoomProperties()
        {
            AllTiles = new ReadOnlyCollection<Tile>(allTiles);
            MainPathTiles = new ReadOnlyCollection<Tile>(mainPathTiles);
            BranchPathTiles = new ReadOnlyCollection<Tile>(branchPathTiles);
            Doors = new ReadOnlyCollection<GameObject>(doors);
            Connections = new ReadOnlyCollection<DoorwayConnection>(connections);
        }

        public void OnDrawGizmos()
        {
            if (DebugRender)
                DebugDraw();
        }

        public void DebugDraw()
        {
            Color mainPathStartColour = Color.red;
            Color mainPathEndColour = Color.green;
            Color branchPathStartColour = Color.blue;
            Color branchPathEndColour = new Color(0.5f, 0, 0.5f);
            float boundsBoxOpacity = 0.75f;

            foreach (var tile in allTiles)
            {
                Bounds bounds = tile.Placement.Bounds;
                bounds.size = bounds.size * 1.01f;

                Color tileColour = (tile.Placement.IsOnMainPath) ?
                                    Color.Lerp(mainPathStartColour, mainPathEndColour, tile.Placement.NormalizedDepth) :
                                    Color.Lerp(branchPathStartColour, branchPathEndColour, tile.Placement.NormalizedDepth);

                tileColour.a = boundsBoxOpacity;
                Gizmos.color = tileColour;

                Gizmos.DrawCube(bounds.center, bounds.size);

            }
        }
	}
}
