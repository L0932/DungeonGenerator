//#define USE_SECTR_VIS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using DunGen.Graph;
using System.IO;

namespace DunGen.Editor
{
	public static class EditorUtil
    {
        /**
         * Utilities for drawing custom classes in the inspector
         */

        public static T CreateAsset<T>(string pathOverride = null, bool selectNewAsset = true) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = (pathOverride != null) ? "Assets/" + pathOverride : AssetDatabase.GetAssetPath(Selection.activeObject);

            if (path == "")
                path = "Assets";
            else if (Path.GetExtension(path) != "")
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).Name + ".asset");
            string dir = Application.dataPath + "/" + path.TrimStart("Assets/".ToCharArray());

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(asset, assetPathAndName);
            AssetDatabase.SaveAssets();

            if (selectNewAsset)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }

            return asset;
        }

		/// <summary>
		/// Draws a GUI for a game object chance table. Allowing for addition/removal of rows and
		/// modification of values and weights
		/// </summary>
		/// <param name="table">The table to draw</param>
        public static void DrawGameObjectChangeTableGUI(string objectName, GameObjectChanceTable table, List<bool> showWeights, bool allowSceneObjects = false)
        {
            EditorGUILayout.LabelField(objectName + " Weights", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;

            int toDeleteIndex = -1;
            GUILayout.BeginVertical("box");

            for (int i = 0; i < table.Weights.Count; i++)
            {
                var w = table.Weights[i];
                GUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                w.Value = (GameObject)EditorGUILayout.ObjectField("", w.Value, typeof(GameObject), allowSceneObjects);

                if (GUILayout.Button("x", EditorStyles.miniButton, EditorConstants.SmallButtonWidth))
                    toDeleteIndex = i;

                EditorGUILayout.EndHorizontal();

                showWeights[i] = EditorGUILayout.Foldout(showWeights[i], "Weights");

                if (showWeights[i])
                {
                    w.MainPathWeight = EditorGUILayout.FloatField("Main Path", w.MainPathWeight);
                    w.BranchPathWeight = EditorGUILayout.FloatField("Branch Path", w.BranchPathWeight);

                    if (w.UseDepthScale)
                        w.DepthWeightScale = EditorGUILayout.CurveField("Depth Scale", w.DepthWeightScale, Color.white, new Rect(0, 0, 1, 1));
                }

                GUILayout.EndVertical();
            }

            if (toDeleteIndex >= 0)
            {
                table.Weights.RemoveAt(toDeleteIndex);
                showWeights.RemoveAt(toDeleteIndex);
            }

            if (GUILayout.Button("Add New " + objectName))
            {
                table.Weights.Add(new GameObjectChance() { UseDepthScale = true });
                showWeights.Add(false);
            }

            EditorGUILayout.EndVertical();
        }

		/// <summary>
		/// Draws a simple GUI for an IntRange
		/// </summary>
		/// <param name="name">A descriptive label</param>
		/// <param name="range">The range to modify</param>
        public static void DrawIntRange(string name, IntRange range)
        {
            EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField(name, EditorConstants.LabelWidth);
            GUILayout.FlexibleSpace();
			range.Min = EditorGUILayout.IntField(range.Min, EditorConstants.IntFieldWidth);
			EditorGUILayout.LabelField("-", EditorConstants.SmallWidth);
			range.Max = EditorGUILayout.IntField(range.Max, EditorConstants.IntFieldWidth);

            EditorGUILayout.EndHorizontal();
        }

		/// <summary>
		/// Draws the GUI for a list of Unity.Object. Allows users to add/remove/modify a specific type
		/// deriving from Unity.Object (such as GameObject, or a Component type)
		/// </summary>
		/// <param name="header">A descriptive header</param>
		/// <param name="objects">The object list to edit</param>
		/// <param name="allowedSelectionTypes">The types of objects that are allowed to be selected</param>
		/// <typeparam name="T">The type of object in the list</typeparam>
		public static void DrawObjectList<T>(string header, IList<T> objects, GameObjectSelectionTypes allowedSelectionTypes) where T : UnityEngine.Object
        {
			bool allowSceneSelection = (allowedSelectionTypes & GameObjectSelectionTypes.InScene) == GameObjectSelectionTypes.InScene;
			bool allowPrefabSelection = (allowedSelectionTypes & GameObjectSelectionTypes.Prefab) == GameObjectSelectionTypes.Prefab;

            EditorGUILayout.PrefixLabel(header);
            EditorGUI.indentLevel = 0;

            int toDeleteIndex = -1;
            GUILayout.BeginVertical("box");

            for (int i = 0; i < objects.Count; i++)
            {
                T obj = objects[i];
                EditorGUILayout.BeginHorizontal();

                T tempObj = (T)EditorGUILayout.ObjectField("", obj, typeof(T), allowSceneSelection);

				if(tempObj != null)
				{
					var prefabType = PrefabUtility.GetPrefabType(tempObj);

					if ((prefabType == PrefabType.Prefab && allowPrefabSelection) || (prefabType != PrefabType.Prefab && allowSceneSelection))
						objects[i] = tempObj;
				}

				if (GUILayout.Button("x", EditorStyles.miniButton, EditorConstants.SmallButtonWidth))
                    toDeleteIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            if (toDeleteIndex >= 0)
                objects.RemoveAt(toDeleteIndex);

            if (GUILayout.Button("Add New"))
                objects.Add(default(T));

            EditorGUILayout.EndVertical();
        }

		public static void DrawKeySelection(string label, KeyManager manager, List<KeyLockPlacement> keys, bool includeRange)
		{
			if(manager == null)
				return;

			manager.ExposeKeyList();

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

			int toDeleteIndex = -1;
			string[] keyNames = manager.Keys.Select(x => x.Name).ToArray();

			for (int i = 0; i < keys.Count; i++)
			{
				EditorGUILayout.BeginVertical("box");

				var key = manager.GetKeyByID(keys[i].ID);

				EditorGUILayout.BeginHorizontal();

				int nameIndex = EditorGUILayout.Popup(Array.IndexOf(keyNames, key.Name), keyNames);
				keys[i].ID = manager.GetKeyByName(keyNames[nameIndex]).ID;

				if (GUILayout.Button("x", EditorStyles.miniButton, EditorConstants.SmallButtonWidth))
					toDeleteIndex = i;

				EditorGUILayout.EndHorizontal();

                if(includeRange)
				    EditorUtil.DrawIntRange("Count", keys[i].Range);

				EditorGUILayout.EndVertical();
			}

			if(toDeleteIndex > -1)
				keys.RemoveAt(toDeleteIndex);

			if(GUILayout.Button("Add"))
				keys.Add(new KeyLockPlacement() { ID = manager.Keys[0].ID });

			EditorGUILayout.EndVertical();
		}

        public static void DrawDungeonGenerator(DungeonGenerator generator, bool isRuntimeDungeon)
        {
            generator.DungeonFlow = (DungeonFlow)EditorGUILayout.ObjectField("Dungeon Flow", generator.DungeonFlow, typeof(DungeonFlow), false);

            generator.ShouldRandomizeSeed = EditorGUILayout.Toggle("Randomize Seed", generator.ShouldRandomizeSeed);

            if (!generator.ShouldRandomizeSeed)
                generator.Seed = EditorGUILayout.IntField("Seed", generator.Seed);

            generator.MaxAttemptCount = EditorGUILayout.IntField("Max Failed Attempts", generator.MaxAttemptCount);
            generator.IgnoreSpriteBounds = EditorGUILayout.Toggle("Ignore Sprite Bounds", generator.IgnoreSpriteBounds);
            generator.UpVector = EditorGUILayout.Vector3Field("Up Vector", generator.UpVector);
            generator.AllowImmediateRepeats = EditorGUILayout.Toggle("Allow Immediate Repeats", generator.AllowImmediateRepeats);

            if (isRuntimeDungeon)
                generator.DebugRender = EditorGUILayout.Toggle("Debug Render", generator.DebugRender);

#if USE_SECTR_VIS

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SECTR Integration", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            generator.IsCullingEnabled = EditorGUILayout.Toggle("Enable", generator.IsCullingEnabled);
            GUI.enabled = generator.IsCullingEnabled;

            var culling = generator.Culling;
            culling.BoundsUpdateModes = (SECTR_Member.BoundsUpdateModes)EditorGUILayout.EnumPopup("Bounds Update Mode", culling.BoundsUpdateModes);
            culling.DirShadowCaster = (Light)EditorGUILayout.ObjectField("Dir Shadow Caster", culling.DirShadowCaster, typeof(Light), true);
            culling.ExtraBounds = EditorGUILayout.FloatField("Extra Bounds", culling.ExtraBounds);
            culling.CullEachChild = EditorGUILayout.Toggle("Cull Each Child", culling.CullEachChild);

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
#endif
        }
	}
}
