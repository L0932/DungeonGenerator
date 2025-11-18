using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DunGen
{
	/// <summary>
	/// A container for all of the information about a tile's posoitioning in the generated dungeon
	/// </summary>
	[Serializable]
	public sealed class TilePlacementData
	{
        public GameObject Root { get { return root; } }
        public Tile Tile { get { return tile; } }

		/// <summary>
		/// Gets the depth of this tile in the dungeon along the main path
		/// </summary>
		public int PathDepth
		{
			get { return pathDepth; }
			internal set { pathDepth = value; }
		}
		/// <summary>
		/// Gets the normalized depth (0.0-1.0) of this tile in the dungeon along the main path
		/// </summary>
		public float NormalizedPathDepth
		{
			get { return normalizedPathDepth; }
			internal set { normalizedPathDepth = value; }
		}
		/// <summary>
		/// Gets the depth of this tile in the dungeon along the branch it's on
		/// </summary>
		public int BranchDepth
		{
			get { return branchDepth; }
			internal set { branchDepth = value; }
		}
		/// <summary>
		/// Gets the normalized depth (0.0-1.0) of this tile in the dungeon along the branch it's on
		/// </summary>
		public float NormalizedBranchDepth
		{
			get { return normalizedBranchDepth; }
			internal set { normalizedBranchDepth = value; }
		}
		/// <summary>
		/// Whether or not this tile lies on the dungeon's main path
		/// </summary>
		public bool IsOnMainPath
		{
			get { return isOnMainPath; }
			internal set { isOnMainPath = value; }
		}
        /// <summary>
        /// The boundaries of this tile
        /// </summary>
        public Bounds Bounds
        {
            get { return bounds; }
            internal set { bounds = value; }
        }
        /// <summary>
        /// Gets the depth of this tile. Returns the branch depth if on a branch path, otherwise, returns the main path depth
        /// </summary>
        public int Depth { get { return (isOnMainPath) ? pathDepth : branchDepth; } }
        /// <summary>
        /// Gets the normalized depth (0-1) of this tile. Returns the branch depth if on a branch path, otherwise, returns the main path depth
        /// </summary>
        public float NormalizedDepth { get { return (isOnMainPath) ? normalizedPathDepth : normalizedBranchDepth; } }

        public List<Doorway> UsedDoorways = new List<Doorway>();
        public List<Doorway> UnusedDoorways = new List<Doorway>();
        public List<Doorway> AllDoorways = new List<Doorway>();

		[SerializeField]
		private int pathDepth;
		[SerializeField]
		private float normalizedPathDepth;
		[SerializeField]
		private int branchDepth;
		[SerializeField]
		private float normalizedBranchDepth;
		[SerializeField]
		private bool isOnMainPath;
        [SerializeField]
        private Bounds bounds;
        [SerializeField]
        private GameObject root;
        [SerializeField]
        private Tile tile;


        internal TilePlacementData(PreProcessTileData preProcessData, bool isOnMainPath, DungeonArchetype archetype, TileSet tileSet)
        {
            root = (GameObject)GameObject.Instantiate(preProcessData.Prefab);

            Bounds = preProcessData.Proxy.GetComponent<Collider>().bounds;
            IsOnMainPath = isOnMainPath;

            tile = Root.GetComponent<Tile>();

            if(tile == null)
                tile = Root.AddComponent<Tile>();

            tile.Placement = this;
            tile.Archetype = archetype;
            tile.TileSet = tileSet;

            foreach (var doorway in Root.GetComponentsInChildren<Doorway>())
            {
                doorway.Tile = tile;
                AllDoorways.Add(doorway);
            }

            UnusedDoorways.AddRange(AllDoorways);
            root.SetActive(false);
        }

        public void ProcessDoorways()
        {
            foreach (var d in UsedDoorways)
                foreach (var obj in d.AddWhenNotInUse)
                    GameObject.DestroyImmediate(obj);

            foreach (var d in UnusedDoorways)
                foreach (var obj in d.AddWhenInUse)
                    GameObject.DestroyImmediate(obj);

            foreach (var d in AllDoorways)
                d.placedByGenerator = true;
        }

		public void RecalculateBounds(bool ignoreSpriteRenderers, Vector3 upVector)
		{
			Bounds = UnityUtil.CalculateObjectBounds(Root, true, ignoreSpriteRenderers);
            Bounds = UnityUtil.CondenseBounds(Bounds, AllDoorways, upVector);
		}

        public Doorway PickRandomDoorway(System.Random randomStream, bool mustBeAvailable, DungeonArchetype archetype)
        {
            float straightenChance = (archetype == null) ? 0.0f : archetype.StraightenChance;
            double rnd = randomStream.NextDouble();

            // Try to pick the opposing doorway, based on archetype's StraightenChance
            if (isOnMainPath && UsedDoorways.Count == 1 && rnd < straightenChance)
            {
                // Find the doorway opposite to the first
                foreach (Doorway d in UnusedDoorways)
                {
                    if (UsedDoorways[0].transform.forward == -d.transform.forward)
                        return d;
                }
            }

            int index = PickRandomDoorwayIndex(randomStream, mustBeAvailable);
            return (index == -1) ? null : AllDoorways[index];
        }

        public int PickRandomDoorwayIndex(System.Random randomStream, bool mustBeAvailable)
        {
            var possibleDoorways = (mustBeAvailable) ? UnusedDoorways : AllDoorways;

            if (possibleDoorways.Count == 0)
                return -1;

            return AllDoorways.IndexOf(possibleDoorways[randomStream.Next(0, possibleDoorways.Count)]);
        }
	}
}

