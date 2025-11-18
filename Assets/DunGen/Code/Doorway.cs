using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DunGen;
using System;

/// <summary>
/// A component to handle doorway placement and behaviour
/// </summary>
[AddComponentMenu("DunGen/Doorway")]
public class Doorway : MonoBehaviour
{
    /// <summary>
    /// The socket group this doorway belongs to. Allows you to use different sized doorways and have them connect correctly
    /// </summary>
    public DoorwaySocketType SocketGroup;
    /// <summary>
    /// When this doorway is in use, a prefab will be picked at random from this list and is spawned at the doorway location - avoids duplicates
    /// </summary>
    public List<GameObject> DoorPrefabs = new List<GameObject>();
    /// <summary>
    /// When this doorway is in use, objects in this list will remain in the scene, otherwise, they are destroyed
    /// </summary>
	public List<GameObject> AddWhenInUse = new List<GameObject>();
    /// <summary>
    /// When this doorway is NOT in use, objects in this list will remain in the scene, otherwise, they are destroyed
    /// </summary>
	public List<GameObject> AddWhenNotInUse = new List<GameObject>();
    /// <summary>
    /// The size of the doorway, for use with portal culling
    /// </summary>
    public Vector2 Size = new Vector2(1, 2);
    /// <summary>
    /// The Tile that this doorway belongs to
    /// </summary>
    public Tile Tile { get; internal set; }
    /// <summary>
    /// The ID of the key used to unlock this door
    /// </summary>
    public int? LockID;
    /// <summary>
    /// Gets the lock status of the door
    /// </summary>
    public bool IsLocked { get { return LockID.HasValue; } }
    /// <summary>
    /// Does this doorway have a prefab object placed as a door?
    /// </summary>
    public bool HasDoorPrefab { get { return doorPrefab != null; } }


    [SerializeField]
    [HideInInspector]
    private GameObject doorPrefab;

    internal bool placedByGenerator;


    private void OnDrawGizmos()
    {
        if (!placedByGenerator)
            DebugDraw();
    }

    internal void SetUsedPrefab(GameObject doorPrefab)
    {
        this.doorPrefab = doorPrefab;
    }

    internal void RemoveUsedPrefab()
    {
        if (doorPrefab != null)
            GameObject.DestroyImmediate(doorPrefab);
    }

    internal void DebugDraw()
    {
        Vector2 halfSize = Size * 0.5f;

        Gizmos.color = EditorConstants.DoorDirectionColour;
        float lineLength = Mathf.Min(Size.x, Size.y);
        Gizmos.DrawLine(transform.position + transform.up * halfSize.y, transform.position + transform.up * halfSize.y + transform.forward * lineLength);

        Gizmos.color = EditorConstants.DoorRectColour;
        Vector3 topLeft = transform.position - (transform.right * halfSize.x) + (transform.up * Size.y);
        Vector3 topRight = transform.position + (transform.right * halfSize.x) + (transform.up * Size.y);
        Vector3 bottomLeft = transform.position - (transform.right * halfSize.x);
        Vector3 bottomRight = transform.position + (transform.right * halfSize.x);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
