using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace DunGen.Editor
{
    public sealed class DungeonGeneratorWindow : EditorWindow
    {
        private DungeonGenerator generator = new DungeonGenerator();
        private GameObject lastDungeon;


        [MenuItem("Window/DunGen/Generate Dungeon")]
        private static void OpenWindow()
        {
            EditorWindow.GetWindow<DungeonGeneratorWindow>(false, "New Dungeon", true);
        }

        private void OnGUI()
        {
            EditorUtil.DrawDungeonGenerator(generator, false);

            if (GUILayout.Button("Generate"))
                GenerateDungeon();
        }

        private void GenerateDungeon()
        {
            lastDungeon = new GameObject("Dungeon Layout");
            generator.Root = lastDungeon;

            Undo.RegisterCreatedObjectUndo(lastDungeon, "Create Procedural Dungeon");
            bool success = generator.Generate();

            if (!success)
            {
                GameObject.DestroyImmediate(lastDungeon);
                lastDungeon = generator.Root = null;
            }
        }
    }
}