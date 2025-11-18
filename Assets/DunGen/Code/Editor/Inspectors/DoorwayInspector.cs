using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DunGen.Editor
{
	[CustomEditor(typeof(Doorway))]
	public class DoorwayInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			Doorway door = target as Doorway;

			if(door == null)
				return;

            door.SocketGroup = (DoorwaySocketType)EditorGUILayout.EnumPopup("Socket Group", door.SocketGroup);
            door.Size = EditorGUILayout.Vector2Field("Size", door.Size);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

			EditorGUILayout.HelpBox("When this doorway is in use (another tile is connected using this doorway), the selected objects below will appear in the scene, otherwise, they will be removed", MessageType.Info);
			EditorUtil.DrawObjectList<GameObject>("Add when in use", door.AddWhenInUse, GameObjectSelectionTypes.InScene);
			EditorGUILayout.HelpBox("When this doorway is in NOT in use (the doorway is closed and no other tile is connected using this doorway), the selected objects below will appear in the scene, otherwise, they will be removed", MessageType.Info);
			EditorUtil.DrawObjectList<GameObject>("Add when NOT in use", door.AddWhenNotInUse, GameObjectSelectionTypes.InScene);
            EditorGUILayout.HelpBox("When this doorway is in use (anoth tile is connected using this doorway), a random prefab will be selected from this list to be spawned at the doorway location.", MessageType.Info);
            EditorUtil.DrawObjectList<GameObject>("Door Prefab", door.DoorPrefabs, GameObjectSelectionTypes.Prefab);

            if (GUI.changed)
                EditorUtility.SetDirty(door);
		}
	}
}

