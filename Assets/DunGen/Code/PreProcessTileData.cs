using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DunGen
{
    public sealed class PreProcessTileData
    {
        public GameObject Prefab { get; private set; }
        public GameObject Proxy { get; private set; }

        public readonly List<DoorwaySocketType> DoorwaySockets = new List<DoorwaySocketType>();
        public readonly List<Doorway> Doorways = new List<Doorway>();


        public PreProcessTileData(GameObject prefab, bool ignoreSpriteRendererBounds, Vector3 upVector)
        {
            Prefab = prefab;
            Proxy = new GameObject(prefab.name + "_PROXY");

            // Reset prefab transforms
            prefab.transform.position = Vector3.zero;
            prefab.transform.rotation = Quaternion.identity;
            prefab.transform.localScale = Vector3.one;

            CalculateProxyBounds(ignoreSpriteRendererBounds, upVector);
            GetAllDoorways();
        }

        public bool ChooseRandomDoorway(System.Random random, DoorwaySocketType? socketGroupFilter, Vector3? allowedDirection, out int doorwayIndex, out Doorway doorway)
        {
            doorwayIndex = -1;
            doorway = null;

            IEnumerable<Doorway> possibleDoorways = Doorways;

            if (socketGroupFilter.HasValue)
                possibleDoorways = possibleDoorways.Where(x => { return DoorwaySocket.IsMatchingSocket(x.SocketGroup, socketGroupFilter.Value); });
            if (allowedDirection.HasValue)
                possibleDoorways = possibleDoorways.Where(x => { return x.transform.forward == allowedDirection; });

            if (possibleDoorways.Count() == 0)
                return false;

            doorway = possibleDoorways.ElementAt(random.Next(0, possibleDoorways.Count()));
            doorwayIndex = Doorways.IndexOf(doorway);

            return true;
        }

        private void CalculateProxyBounds(bool ignoreSpriteRendererBounds, Vector3 upVector)
        {
            Bounds bounds = UnityUtil.CalculateObjectBounds(Prefab, true, ignoreSpriteRendererBounds);
            bounds = UnityUtil.CondenseBounds(bounds, Prefab.GetComponentsInChildren<Doorway>(true), upVector);
            bounds.size *= 0.99f;

            var collider = Proxy.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
        }

        private void GetAllDoorways()
        {
            DoorwaySockets.Clear();

            foreach (var d in Prefab.GetComponentsInChildren<Doorway>(true))
            {
                Doorways.Add(d);

                if (!DoorwaySockets.Contains(d.SocketGroup))
                    DoorwaySockets.Add(d.SocketGroup);
            }
        }
    }
}
