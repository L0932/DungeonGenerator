//#define USE_SECTR_VIS

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using DunGen.Graph;
using DunGen.Analysis;

using Random = System.Random;
using Stopwatch = System.Diagnostics.Stopwatch;


namespace DunGen
{
	[Serializable]
	public class DungeonGenerator
	{
		public int Seed;
        public bool ShouldRandomizeSeed = true;
		public Random RandomStream { get; protected set; }
		public int MaxAttemptCount = 20;
        public bool IgnoreSpriteBounds = true;
        public Vector3 UpVector = Vector3.up;
        public bool AllowImmediateRepeats = true;
        public bool DebugRender = false;
        public bool AllowBacktracking = true;

		public GameObject Root;
        public DungeonFlow DungeonFlow;
        public event GenerationStatusDelegate OnGenerationStatusChanged;
        public GenerationStatus Status { get; private set; }
        public GenerationStats GenerationStats { get; private set; }
        public int ChosenSeed { get; protected set; }
        public Dungeon CurrentDungeon { get { return currentDungeon; } }

		protected int retryCount;
        protected int roomRetryCount;
		protected Dungeon currentDungeon;
        protected readonly List<PreProcessTileData> preProcessData = new List<PreProcessTileData>();
        protected readonly List<GameObject> useableTiles = new List<GameObject>();
        protected int targetLength;

        private int nextNodeIndex;
        private DungeonArchetype currentArchetype;
        private GraphLine previousLineSegment;
		private bool isAnalysis;
        private GameObject lastTilePrefabUsed;

#if USE_SECTR_VIS
        public bool IsCullingEnabled = true;
        public SECTRCulling Culling = new SECTRCulling();
#endif


        public DungeonGenerator()
		{
			GenerationStats = new GenerationStats();
		}

        public DungeonGenerator(GameObject root)
			:this()
        {
            Root = root;
        }

		protected bool OuterGenerate(int? seed)
		{
            ShouldRandomizeSeed = !seed.HasValue;

            if (seed.HasValue)
                Seed = seed.Value;

			return Generate();
		}

		public bool Generate()
		{
			isAnalysis = false;
			return OuterGenerate();
		}

		protected virtual bool OuterGenerate()
		{
            Status = GenerationStatus.NotStarted;
            DungeonArchetypeValidator validator = new DungeonArchetypeValidator(DungeonFlow);

            if (!validator.IsValid())
            {
                ChangeStatus(GenerationStatus.Failed);
                return false;
            }

			ChosenSeed = (ShouldRandomizeSeed) ? new Random().Next() : Seed;
			RandomStream = new Random(ChosenSeed);

			if(Root == null)
				Root = new GameObject(Constants.DefaultDungeonRootName);

			bool success = InnerGenerate(false);

            if (!success)
                Clear();

            return success;
		}

		public GenerationAnalysis RunAnalysis(int iterations, float maximumAnalysisTime)
		{
			DungeonArchetypeValidator validator = new DungeonArchetypeValidator(DungeonFlow);

            // No need to validate outside of the editor
            if (Application.isEditor)
            {
                if (!validator.IsValid())
                {
                    ChangeStatus(GenerationStatus.Failed);
                    return null;
                }
            }

            bool prevShouldRandomizeSeed = ShouldRandomizeSeed;

			isAnalysis = true;
            ShouldRandomizeSeed = true;
			GenerationAnalysis analysis = new GenerationAnalysis(iterations);
			Stopwatch sw = Stopwatch.StartNew();

			for (int i = 0; i < iterations; i++)
			{
				if(maximumAnalysisTime > 0 && sw.Elapsed.TotalMilliseconds >= maximumAnalysisTime)
					break;

				if(OuterGenerate())
				{
					analysis.IncrementSuccessCount();
					analysis.Add(GenerationStats);
				}
			}
            
			Clear();
            
			analysis.Analyze();
            ShouldRandomizeSeed = prevShouldRandomizeSeed;

			return analysis;
		}

        public void RandomizeSeed()
        {
            Seed = new Random().Next();
        }

		protected virtual bool InnerGenerate(bool isRetry)
		{
            if (isRetry)
            {
                if (retryCount >= MaxAttemptCount && Application.isEditor)
                {
                    Debug.LogError(string.Format("Failed to generate the dungeon {0} times. This could indicate a problem with the way the tiles are set up. Try to make sure most rooms have more than one doorway and that all doorways are easily accessible.", MaxAttemptCount));
                    ChangeStatus(GenerationStatus.Failed);
                    return false;
                }

                retryCount++;
				GenerationStats.IncrementRetryCount();
            }
            else
			{
                retryCount = 0;
				GenerationStats.Clear();
			}

			currentDungeon = Root.GetComponent<Dungeon>();
			if(currentDungeon == null)
				currentDungeon = Root.AddComponent<Dungeon>();

            currentDungeon.DebugRender = DebugRender;
			currentDungeon.PreGenerateDungeon(this);

            // The actual generation steps
			Clear();
            GenerationStats.BeginTime(GenerationStatus.PreProcessing);
            PreProcess();
            GenerationStats.BeginTime(GenerationStatus.MainPath);
            if (!GenerateMainPath())
            {
                //Debug.Log("Failed to generate dungeon, trying again..");
                ChosenSeed = RandomStream.Next();
                RandomStream = new Random(ChosenSeed);

                return InnerGenerate(true);
            }
            GenerationStats.BeginTime(GenerationStatus.Branching);
            GenerateBranchPaths();
            GenerationStats.BeginTime(GenerationStatus.PostProcessing);
            PostProcess();
            GenerationStats.EndTime();

            // Activate all door gameobjects that were added to doorways
            currentDungeon.ExposeRoomProperties();
            foreach (var door in currentDungeon.Doors)
                door.SetActive(true);

            ChangeStatus(GenerationStatus.Complete);
            return true;
		}

		protected virtual void Clear()
		{
			currentDungeon.Clear();

            foreach (var p in preProcessData)
                GameObject.DestroyImmediate(p.Proxy);

            useableTiles.Clear();
            preProcessData.Clear();
		}

        private void ChangeStatus(GenerationStatus status)
        {
            var previousStatus = Status;
            Status = status;

            if (previousStatus != status && OnGenerationStatusChanged != null)
                OnGenerationStatusChanged(this, status);
        }

        protected virtual void PreProcess()
        {
            if (preProcessData.Count > 0)
                return;

            ChangeStatus(GenerationStatus.PreProcessing);

            foreach(var tileSet in DungeonFlow.GetUsedTileSets())
                foreach (var tile in tileSet.TileWeights.Weights)
                {
                    if (tile.Value != null)
                        useableTiles.Add(tile.Value);
                }

            foreach (var tile in useableTiles)
                preProcessData.Add(new PreProcessTileData(tile, IgnoreSpriteBounds, UpVector));
        }

        protected virtual bool GenerateMainPath()
        {
            ChangeStatus(GenerationStatus.MainPath);
            targetLength = DungeonFlow.Length.GetRandom(RandomStream);
            nextNodeIndex = 0;
            List<GraphNode> handledNodes = new List<GraphNode>(DungeonFlow.Nodes.Count);
            bool isDone = false;
            int i = 0;

            // Keep track of these now, we'll need them later when we know the actual length of the dungeon
            List<List<TileSet>> tiles = new List<List<TileSet>>(targetLength);
            List<DungeonArchetype> archetypes = new List<DungeonArchetype>(targetLength);
            List<GraphNode> nodes = new List<GraphNode>(targetLength);
            List<GraphLine> lines = new List<GraphLine>(targetLength);

            // We can't rigidly stick to the target length since we need at least one room for each node and that might be more than targetLength
            while(!isDone)
            {
                float depth = Mathf.Clamp(i / (float)(targetLength - 1), 0, 1);
                GraphLine lineSegment = DungeonFlow.GetLineAtDepth(depth);

                // This should never happen
                if(lineSegment == null)
                    return false;

                // We're on a new line segment, change the current archetype
                if (lineSegment != previousLineSegment)
                {
                    currentArchetype = lineSegment.DungeonArchetypes[RandomStream.Next(0, lineSegment.DungeonArchetypes.Count)];
                    previousLineSegment = lineSegment;
                }

                List<TileSet> useableTileSets = null;
                GraphNode nextNode = null;
                var orderedNodes = DungeonFlow.Nodes.OrderBy(x => x.Position).ToArray();

                // Determine which node comes next
                foreach (var node in orderedNodes)
                {
                    if (depth >= node.Position && !handledNodes.Contains(node))
                    {
                        nextNode = node;
                        handledNodes.Add(node);
                        break;
                    }
                }

                // Assign the TileSets to use based on whether we're on a node or a line segment
                if (nextNode != null)
                {
                    useableTileSets = nextNode.TileSets;
                    nextNodeIndex = (nextNodeIndex >= orderedNodes.Length - 1) ? -1 : nextNodeIndex + 1;
                    archetypes.Add(null);
                    lines.Add(null);
                    nodes.Add(nextNode);

                    if (nextNode == orderedNodes[orderedNodes.Length - 1])
                        isDone = true;
                }
                else
                {
                    useableTileSets = currentArchetype.TileSets;
                    archetypes.Add(currentArchetype);
                    lines.Add(lineSegment);
                    nodes.Add(null);
                }

                tiles.Add(useableTileSets);

                i++;
            }


            if (AllowBacktracking)
            {
                int tileRetryCount = 0;
                int totalForLoopRetryCount = 0;

                for (int j = 0; j < tiles.Count; j++)
                {
                    var tile = AddTile((j == 0) ? null : currentDungeon.MainPathTiles[j - 1],
                                        tiles[j],
                                        j / (float)(tiles.Count - 1),
                                        archetypes[j]);

                    // if no tile could be generated delete last successful tile and retry from previous index
                    // else return false
                    if (j > 5 && tile == null && tileRetryCount < 5 && totalForLoopRetryCount < 20)
                    {
                        currentDungeon.RemoveLastConnection();
                        currentDungeon.RemoveTile(currentDungeon.MainPathTiles[j - 1]);

                        j -= 2; // -2 because loop adds 1
                        tileRetryCount++;
                        totalForLoopRetryCount++;

                    }
                    else if (tile == null)
                    {
                        return false;

                    }
                    else
                    {
                        tile.Node = nodes[j];
                        tile.Line = lines[j];
                        tileRetryCount = 0;
                    }

                }
            }
            else
            {
                for (int j = 0; j < tiles.Count; j++)
                {
                    var tile = AddTile((j == 0) ? null : currentDungeon.MainPathTiles[j - 1],
                                        tiles[j],
                                        j / (float)(tiles.Count - 1),
                                        archetypes[j]);

                    // Return false if no tile could be generated
                    if (tile == null)
                        return false;
                    else
                    {
                        tile.Node = nodes[j];
                        tile.Line = lines[j];
                    }
                }
            }

            return true;
        }

        protected virtual void GenerateBranchPaths()
        {
            ChangeStatus(GenerationStatus.Branching);

            foreach (var tile in currentDungeon.MainPathTiles)
            {
                // This tile was created from a graph node, there should be no branching
                if (tile.Archetype == null)
                    continue;

                int branchCount = tile.Archetype.BranchCount.GetRandom(RandomStream);

                if (branchCount == 0)
                    continue;

                Tile previousTile = tile;

                for (int i = 0; i < branchCount; i++)
                {
                    List<TileSet> useableTileSets;

                    if (i == (branchCount - 1) && tile.Archetype.GetHasValidBranchCapTiles())
                    {
                        if (tile.Archetype.BranchCapType == BranchCapType.InsteadOf)
                            useableTileSets = tile.Archetype.BranchCapTileSets;
                        else
                            useableTileSets = tile.Archetype.TileSets.Concat(tile.Archetype.BranchCapTileSets).ToList();
                    }
                    else
                        useableTileSets = tile.Archetype.TileSets;

                    Tile newTile = AddTile(previousTile, useableTileSets, i / (float)(branchCount - 1), tile.Archetype);

                    if (newTile == null)
                        continue;

                    newTile.Placement.BranchDepth = i;
                    newTile.Placement.NormalizedBranchDepth = i / (float)(branchCount - 1);
                    newTile.Node = previousTile.Node;
                    newTile.Line = previousTile.Line;
                    previousTile = newTile;
                }
            }
        }

        protected virtual Tile AddTile(Tile attachTo, IList<TileSet> useableTileSets, float normalizedDepth, DungeonArchetype archetype, bool isRetry = false)
        {
            if (!isRetry)
                roomRetryCount = 0;

            TileSet tileSet = useableTileSets[RandomStream.Next(0, useableTileSets.Count)];
            Doorway fromDoorway = (attachTo == null) ? null : attachTo.Placement.PickRandomDoorway(RandomStream, true, archetype);

            if(attachTo != null && fromDoorway == null)
                return null;

            var tileWeights = tileSet.TileWeights.Clone();

            if (attachTo != null)
            {
                for (int i = tileWeights.Weights.Count - 1; i >= 0; i--)
                {
                    var c = tileWeights.Weights[i];
                    var cTemplate = preProcessData.Where(x => { return x.Prefab == c.Value; }).FirstOrDefault();

                    if (cTemplate == null || !cTemplate.DoorwaySockets.Contains(fromDoorway.SocketGroup))
                        tileWeights.Weights.RemoveAt(i);
                }
            }

            if (tileWeights.Weights.Count == 0)
            {
                roomRetryCount++;

                if (roomRetryCount > MaxAttemptCount)
                    return null;
                else
                    return AddTile(attachTo, useableTileSets, normalizedDepth, archetype, true);
            }

            GameObject tilePrefab = tileSet.TileWeights.GetRandom(RandomStream, (Status == GenerationStatus.MainPath), normalizedDepth, lastTilePrefabUsed, AllowImmediateRepeats);
            var toTemplate = preProcessData.Where(x => { return x.Prefab == tilePrefab; }).FirstOrDefault();

            if (toTemplate == null)
                return null;

            int toDoorwayIndex = 0;
            Doorway toDoorway = null;

            if (fromDoorway != null)
            {
                Tile toTile = toTemplate.Prefab.GetComponent<Tile>();
                Vector3? allowedDirection;

                if(toTile == null || toTile.AllowRotation)
                    allowedDirection = null;
                else
                    allowedDirection = -fromDoorway.transform.forward;

                if (!toTemplate.ChooseRandomDoorway(RandomStream, fromDoorway.SocketGroup, allowedDirection, out toDoorwayIndex, out toDoorway))
                    return null;

                MoveIntoPosition(toTemplate.Proxy, fromDoorway, toDoorway);

                if (IsCollidingWithAnyTile(toTemplate.Proxy))
                {
                    roomRetryCount++;

                    if (roomRetryCount > MaxAttemptCount)
                        return null;
                    else
                        return AddTile(attachTo, useableTileSets, normalizedDepth, archetype, true);
                }
            }

            TilePlacementData newTile = new TilePlacementData(toTemplate, (Status == GenerationStatus.MainPath), archetype, tileSet);

			if(newTile == null)
				return null;

			if(newTile.IsOnMainPath)
			{
				if(attachTo != null)
					newTile.PathDepth = attachTo.Placement.PathDepth + 1;
			}
			else
			{
				newTile.PathDepth = attachTo.Placement.PathDepth;
				newTile.BranchDepth = (attachTo.Placement.IsOnMainPath) ? 0 : attachTo.Placement.BranchDepth + 1;
			}

            if (fromDoorway != null)
            {
                // Moving enabled objects is very slow in the editor so we disable it first
                if (!Application.isPlaying)
                    newTile.Root.SetActive(false);

                newTile.Root.transform.parent = Root.transform;
                toDoorway = newTile.AllDoorways[toDoorwayIndex];

                MoveIntoPosition(newTile.Root, fromDoorway, toDoorway);

                // Remember to re-enable any object we disabled earlier
                if (!Application.isPlaying)
                    newTile.Root.SetActive(true);

                currentDungeon.MakeConnection(fromDoorway, toDoorway, RandomStream);
            }
            else
                newTile.Root.transform.parent = Root.transform;

            if (newTile != null)
                currentDungeon.AddTile(newTile.Tile);

            newTile.RecalculateBounds(IgnoreSpriteBounds, UpVector);
            lastTilePrefabUsed = tilePrefab;

            return newTile.Tile;
        }

        protected PreProcessTileData PickRandomTemplate(DoorwaySocketType? socketGroupFilter)
        {
            var possibleTemplates = (socketGroupFilter.HasValue) ? preProcessData.Where(x => { return x.DoorwaySockets.Contains(socketGroupFilter.Value); }) : preProcessData;
            return possibleTemplates.ElementAt(RandomStream.Next(0, possibleTemplates.Count()));
        }

        protected int NormalizedDepthToIndex(float normalizedDepth)
        {
            return Mathf.RoundToInt(normalizedDepth * (targetLength - 1));
        }

        protected float IndexToNormalizedDepth(int index)
        {
            return index / (float)targetLength;
        }

        protected void MoveIntoPosition(GameObject obj, Doorway fromDoorway, Doorway toDoorway)
        {
            obj.transform.position = -toDoorway.transform.position + fromDoorway.transform.position;
            obj.transform.rotation = Quaternion.identity;

            Vector3 a = fromDoorway.transform.forward;
            Vector3 b = -toDoorway.transform.forward;

            float dot = Vector3.Dot(a, b);
            float offsetAngle = 0;
            const float epsilon = 0.00001f;

            if (dot >= 1.0f - epsilon)
                offsetAngle = 0.0f;
            else if (dot <= -1.0f + epsilon)
                offsetAngle = Mathf.PI;
            else
                offsetAngle = Mathf.Acos(dot);

            if (float.IsNaN(offsetAngle))
                Debug.LogError("[FloorGenerator] Offset angle is NaN. This should never happen. | Dot: " + dot + ", From: " + fromDoorway.transform.forward + ", To: " + toDoorway.transform.forward);

            Vector3 cross = Vector3.Cross(a, b);

            if (Vector3.Dot(UpVector, cross) > 0)
                offsetAngle *= -1;

            float angle = offsetAngle * Mathf.Rad2Deg;
            // We need to clamp the angle to the nearest increment of 90 degrees due to floating-point errors
            angle = NumberUtil.ClampToNearest(angle, 0, 90, 180, 270, 360, -90, -180);

            obj.transform.RotateAround(fromDoorway.transform.position, UpVector, angle);
        }

        protected bool IsCollidingWithAnyTile(GameObject proxy)
        {
            foreach (var r in currentDungeon.AllTiles)
                if (r.Placement.Bounds.Intersects(proxy.GetComponent<Collider>().bounds))
                    return true;

            return false;
        }

        protected void ClearPreProcessData()
        {
            foreach (var p in preProcessData)
                GameObject.DestroyImmediate(p.Proxy);

            preProcessData.Clear();
        }

		protected virtual void ConnectOverlappingDoorways(float percentageChance)
		{
			if(percentageChance <= 0)
				return;

			var doorways = Root.GetComponentsInChildren<Doorway>();
			List<Doorway> processedDoorways = new List<Doorway>(doorways.Length);

			const float epsilon = 0.00001f;

			foreach(var a in doorways)
			{
				foreach(var b in doorways)
				{
					if(a == b)
						continue;

					if(a.Tile == b.Tile)
						continue;

                    if (a.SocketGroup != b.SocketGroup)
                        continue;

					if(processedDoorways.Contains(b))
						continue;

					float distanceSqrd = (a.transform.position - b.transform.position).sqrMagnitude;

					if(distanceSqrd < epsilon)
					{
						if(RandomStream.NextDouble() < percentageChance)
							currentDungeon.MakeConnection(a, b, RandomStream);
					}
				}

				processedDoorways.Add(a);
			}
		}

        protected virtual void PostProcess()
        {
            ChangeStatus(GenerationStatus.PostProcessing);

            foreach (var tile in currentDungeon.AllTiles)
                tile.gameObject.SetActive(true);

            int length = currentDungeon.MainPathTiles.Count;

            //int maxBranchDepth = currentDungeon.BranchPathTiles.OrderByDescending(x => x.Placement.BranchDepth).Select(x => x.Placement.BranchDepth).FirstOrDefault();

            //
            // Need to sort list manually to avoid compilation problems on iOS
            int maxBranchDepth = 0;

            if (currentDungeon.BranchPathTiles.Count > 0)
            {
                List<Tile> branchTiles = currentDungeon.BranchPathTiles.ToList();
                branchTiles.Sort((a, b) =>
                    {
                        return b.Placement.BranchDepth.CompareTo(a.Placement.BranchDepth);
                    }
                );

                maxBranchDepth = branchTiles[0].Placement.BranchDepth;
            }
            // End calculate max branch depth
            //

			if(!isAnalysis)
			{
				ConnectOverlappingDoorways(DungeonFlow.DoorwayConnectionChance);

	            foreach (var tile in currentDungeon.AllTiles)
	            {
	                tile.Placement.NormalizedPathDepth = tile.Placement.PathDepth / (float)(length - 1);
	                tile.Placement.ProcessDoorways();
	            }

	            currentDungeon.PostGenerateDungeon(this);
	            PlaceLocksAndKeys();

	            // Process random props
	            foreach (var tile in currentDungeon.AllTiles)
	                foreach (var prop in tile.GetComponentsInChildren<RandomProp>())
	                    prop.Process(RandomStream, tile);

	            ProcessGlobalProps();
			}

            GenerationStats.SetRoomStatistics(currentDungeon.MainPathTiles.Count, currentDungeon.BranchPathTiles.Count, maxBranchDepth);
            ClearPreProcessData();

#if USE_SECTR_VIS
            if (IsCullingEnabled)
            {
                Culling.Clear();
                Culling.PrepareForCulling(currentDungeon);
            }
#endif
        }

        protected virtual void ProcessGlobalProps()
        {
            Dictionary<int, GameObjectChanceTable> globalPropWeights = new Dictionary<int, GameObjectChanceTable>();

            foreach (var tile in currentDungeon.AllTiles)
            {
                foreach (var prop in tile.GetComponentsInChildren<GlobalProp>())
                {
                    GameObjectChanceTable table = null;

                    if (!globalPropWeights.TryGetValue(prop.PropGroupID, out table))
                    {
                        table = new GameObjectChanceTable();
                        globalPropWeights[prop.PropGroupID] = table;
                    }

                    float weight = (tile.Placement.IsOnMainPath) ? prop.MainPathWeight : prop.BranchPathWeight;
                    weight *= prop.DepthWeightScale.Evaluate(tile.Placement.NormalizedDepth);

                    table.Weights.Add(new GameObjectChance(prop.gameObject, weight, 0));
                }
            }

            var allGlobalProps = globalPropWeights.SelectMany(x => x.Value.Weights.Select(y => y.Value));

            foreach (var prop in allGlobalProps)
                prop.SetActive(false);

            List<int> processedPropGroups = new List<int>(globalPropWeights.Count);

            foreach (var pair in globalPropWeights)
            {
                if (processedPropGroups.Contains(pair.Key))
                {
                    Debug.LogWarning("Dungeon Flow contains multiple entries for the global prop group ID: " + pair.Key + ". Only the first entry will be used.");
                    continue;
                }

                int index = DungeonFlow.GlobalPropGroupIDs.IndexOf(pair.Key);

                if (index == -1)
                    continue;

                IntRange range = DungeonFlow.GlobalPropRanges[index];

                var weights = pair.Value.Clone();
                int propCount = range.GetRandom(RandomStream);
                propCount = Mathf.Clamp(propCount, 0, weights.Weights.Count);

                for (int i = 0; i < propCount; i++)
                {
                    var prop = weights.GetRandom(RandomStream, true, 0, null, true, true);

                    if (prop != null)
                        prop.SetActive(true);
                }

                processedPropGroups.Add(pair.Key);
            }
        }

        protected virtual void PlaceLocksAndKeys()
        {
            var nodes = currentDungeon.ConnectionGraph.Nodes.Select(x => x.Tile.Node).Where(x => { return x != null; }).Distinct().ToArray();
            var lines = currentDungeon.ConnectionGraph.Nodes.Select(x => x.Tile.Line).Where(x => { return x != null; }).Distinct().ToArray();

            Dictionary<Doorway, Key> lockedDoorways = new Dictionary<Doorway, Key>();

            // Lock doorways on nodes
            foreach (var node in nodes)
            {
                foreach (var l in node.Locks)
                {
                    var tile = currentDungeon.AllTiles.Where(x => { return x.Node == node; }).FirstOrDefault();
                    var connections = currentDungeon.ConnectionGraph.Nodes.Where(x => { return x.Tile == tile; }).FirstOrDefault().Connections;
                    Doorway entrance = null;
                    Doorway exit = null;

                    foreach (var conn in connections)
                    {
                        if (conn.DoorwayA.Tile == tile)
                            exit = conn.DoorwayA;
                        else if (conn.DoorwayB.Tile == tile)
                            entrance = conn.DoorwayB;
                    }

                    if (entrance != null && (node.LockPlacement & NodeLockPlacement.Entrance) == NodeLockPlacement.Entrance)
                    {
                        var key = node.Graph.KeyManager.GetKeyByID(l.ID);
                        lockedDoorways[entrance] = key;
                    }
                    if (exit != null && (node.LockPlacement & NodeLockPlacement.Exit) == NodeLockPlacement.Exit)
                    {
                        var key = node.Graph.KeyManager.GetKeyByID(l.ID);
                        lockedDoorways[entrance] = key;
                    }
                }
            }

            // Lock doorways on lines
            foreach (var line in lines)
            {
                var doorways = currentDungeon.ConnectionGraph.Connections.Where(x => { return x.DoorwayA.Tile.Line == line && x.DoorwayB.Tile.Line == line; }).Select(x => x.DoorwayA).ToList();

                if (doorways.Count == 0)
                    continue;

                foreach (var l in line.Locks)
                {
                    int lockCount = l.Range.GetRandom(RandomStream);
                    lockCount = Mathf.Clamp(lockCount, 0, doorways.Count);

                    for (int i = 0; i < lockCount; i++)
                    {
                        var doorway = doorways[RandomStream.Next(0, doorways.Count)];
                        doorways.Remove(doorway);

                        var key = line.Graph.KeyManager.GetKeyByID(l.ID);
                        lockedDoorways.Add(doorway, key);
                    }
                }
            }

			List<Doorway> locksToRemove = new List<Doorway>();

			foreach(var pair in lockedDoorways)
			{
				var door = pair.Key;
				var key = pair.Value;
				List<Tile> possibleSpawnTiles = new List<Tile>();

                foreach (var t in currentDungeon.AllTiles)
                {
                    if (t.Placement.NormalizedPathDepth >= door.Tile.Placement.NormalizedPathDepth)
                        continue;

                    bool canPlaceKey = false;

                    if (t.Node != null && t.Node.Keys.Where(x => { return x.ID == key.ID; }).Count() > 0)
                        canPlaceKey = true;
                    else if (t.Line != null && t.Line.Keys.Where(x => { return x.ID == key.ID; }).Count() > 0)
                        canPlaceKey = true;

                    if (!canPlaceKey)
                        continue;

                    //if (!door.Tile.Placement.IsOnMainPath)
                    //{
                    //    if (t.Placement.NormalizedBranchDepth >= door.Tile.Placement.NormalizedBranchDepth)
                    //        continue;
                    //}

                    possibleSpawnTiles.Add(t);
                }

				var possibleSpawnComponents = possibleSpawnTiles.SelectMany(x => x.GetComponentsInChildren<Component>().OfType<IKeySpawnable>()).ToList();

				if(possibleSpawnComponents.Count == 0)
					locksToRemove.Add(door);
				else
				{
                    int keysToSpawn = key.KeysPerLock.GetRandom(RandomStream);
                    keysToSpawn = Math.Min(keysToSpawn, possibleSpawnComponents.Count);

                    for (int i = 0; i < keysToSpawn; i++)
                    {
                        int chosenCompID = RandomStream.Next(0, possibleSpawnComponents.Count);
                        var comp = possibleSpawnComponents[chosenCompID];
                        comp.SpawnKey(key, DungeonFlow.KeyManager);

                        foreach (var k in (comp as Component).GetComponentsInChildren<Component>().OfType<IKeyLock>())
                            k.OnKeyAssigned(key, DungeonFlow.KeyManager);

                        possibleSpawnComponents.RemoveAt(chosenCompID);
                    }
				}
			}

			foreach(var door in locksToRemove)
				lockedDoorways.Remove(door);

            foreach (var pair in lockedDoorways)
            {
                pair.Key.RemoveUsedPrefab();
                LockDoorway(pair.Key, pair.Value, DungeonFlow.KeyManager);
            }
        }

        protected virtual void LockDoorway(Doorway doorway, Key key, KeyManager keyManager)
        {
            var placement = doorway.Tile.Placement;
            var prefabs = doorway.Tile.TileSet.LockPrefabs.Where(x => { return x.SocketGroup == doorway.SocketGroup; }).Select(x => x.LockPrefabs).ToArray();
            var prefab = prefabs[RandomStream.Next(0, prefabs.Length)].GetRandom(RandomStream, placement.IsOnMainPath, placement.NormalizedDepth, null, true);

            GameObject obj = (GameObject)GameObject.Instantiate(prefab);
            obj.transform.parent = doorway.transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            foreach (var keylock in doorway.GetComponentsInChildren<Component>().OfType<IKeyLock>())
                keylock.OnKeyAssigned(key, keyManager);
        }
	}
}
