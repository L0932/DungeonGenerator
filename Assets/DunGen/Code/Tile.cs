using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DunGen.Graph;

namespace DunGen
{
    [AddComponentMenu("DunGen/Tile")]
	public class Tile : MonoBehaviour
    {
        /// <summary>
        /// Should this tile be allowed to rotate to fit in place?
        /// </summary>
        public bool AllowRotation = true;

		/// <summary>
		/// Information about the tile's position in the generated dungeon
		/// </summary>
		public TilePlacementData Placement
		{
			get { return placement; }
			internal set { placement = value; }
		}
        /// <summary>
        /// The Dungeon Archetype that is assigned to this tile (only applicable if this tile lay on a graph line)
        /// </summary>
        public DungeonArchetype Archetype
        {
            get { return archetype; }
            internal set { archetype = value; }
        }
        /// <summary>
        /// The TileSet that is assigned to this tile
        /// </summary>
        public TileSet TileSet
        {
            get { return tileSet; }
            internal set { tileSet = value; }
        }
        /// <summary>
        /// The flow graph node this tile was spawned from (only applicable if this tile lay on a graph node)
        /// </summary>
        public GraphNode Node
        {
            get { return (node == null) ? null : node.Node; }
            internal set
            {
                if (value == null)
                    node = null;
                else
                    node = new FlowNodeReference(value.Graph, value);
            }
        }
        /// <summary>
        /// The flow graph line this tile was spawned from (only applicable if this tile lay on a graph line)
        /// </summary>
        public GraphLine Line
        {
            get { return (line == null) ? null : line.Line; }
            internal set
            {
                if (value == null)
                    line = null;
                else
                    line = new FlowLineReference(value.Graph, value);
            }
        }

        [SerializeField]
        private TilePlacementData placement;

        [SerializeField]
        private DungeonArchetype archetype;
        [SerializeField]
        private TileSet tileSet;
        [SerializeField]
        private FlowNodeReference node;
        [SerializeField]
        private FlowLineReference line;


        private void OnDrawGizmosSelected()
        {
            if (placement == null)
                return;

            Bounds bounds = placement.Bounds;
            
            //bounds = UnityUtil.CalculateObjectBounds(gameObject, true, true);
            //bounds = UnityUtil.CondenseBounds(bounds, gameObject.GetComponentsInChildren<Doorway>());
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
	}
}
