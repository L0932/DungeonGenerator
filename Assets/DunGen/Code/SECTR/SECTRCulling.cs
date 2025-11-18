//#define USE_SECTR_VIS

#if USE_SECTR_VIS
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DunGen
{
    [Serializable]
    public sealed class SECTRCulling
    {
        public SECTR_Member.BoundsUpdateModes BoundsUpdateModes = SECTR_Member.BoundsUpdateModes.Static;
        public Light DirShadowCaster;
        public float ExtraBounds = 0.01f;
        public bool CullEachChild = false;

        private GameObject portalContainer;


        public void Clear()
        {
            GameObject.DestroyImmediate(portalContainer);
            portalContainer = null;
        }

        public void PrepareForCulling(Dungeon dungeon)
        {
            Dictionary<Tile, SECTR_Sector> sectors = new Dictionary<Tile, SECTR_Sector>();
            portalContainer = new GameObject("Portals");
            portalContainer.transform.parent = dungeon.transform;
            portalContainer.transform.localPosition = Vector3.zero;

            foreach (var node in dungeon.ConnectionGraph.Nodes)
            {
                var obj = node.Tile.gameObject;
                var sector = obj.AddComponent<SECTR_Sector>();
                sector.BoundsUpdateMode = BoundsUpdateModes;
                sector.DirShadowCaster = DirShadowCaster;
                sector.ExtraBounds = ExtraBounds;

                sectors[node.Tile] = sector;

                var culler = obj.AddComponent<SECTR_Culler>();
                culler.CullEachChild = CullEachChild;
            }

            foreach (var conn in dungeon.ConnectionGraph.Connections)
            {
                var doorway = conn.DoorwayA;

                GameObject portalObj = new GameObject("Portal_" + conn.A.Tile.gameObject.name + "_" + conn.B.Tile.gameObject.name);
                portalObj.transform.parent = portalContainer.transform;
                portalObj.transform.position = doorway.transform.position;
                portalObj.transform.rotation = doorway.transform.rotation;
                portalObj.transform.localScale = doorway.transform.localScale;

                float doorwayHalfSize = doorway.Size.x * 0.5f;

                Mesh portalMesh = new Mesh();
                portalMesh.vertices = new Vector3[]
                {
                    new Vector3(-doorwayHalfSize, 0, 0),
                    new Vector3(-doorwayHalfSize, doorway.Size.y, 0),
                    new Vector3(doorwayHalfSize, doorway.Size.y, 0),
                    new Vector3(doorwayHalfSize, 0, 0),
                };
                portalMesh.normals = new Vector3[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
                portalMesh.triangles = new int[] { 0, 1, 2, 2, 3, 0 };

                var portal = portalObj.AddComponent<SECTR_Portal>();
                portal.HullMesh = portalMesh;
                portal.FrontSector = sectors[conn.B.Tile];
                portal.BackSector = sectors[conn.A.Tile];
            }
        }
    }
}
#endif