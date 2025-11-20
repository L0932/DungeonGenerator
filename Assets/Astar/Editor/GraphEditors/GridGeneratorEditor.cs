using Pathfinding;
using Pathfinding.Serialization.JsonFx;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Pathfinding
{
    [CustomGraphEditor(typeof(GridGraph), "Grid Graph")]
    public class GridGraphEditor : GraphEditor
    {
        [JsonMember]
        public bool locked = true;

        float newNodeSize;

        [JsonMember]
        public bool showExtra = false;

        /** Should textures be allowed to be used.
          * This can be set to false by inheriting graphs not implementing that feature */
        [JsonMember]
        public bool textureVisible = true;

        Matrix4x4 savedMatrix;
        Vector3 savedCenter;

        public bool isMouseDown = false;

        [JsonMember]
        public GridPivot pivot;

        GraphNode node1;

        /** Rounds a vector's components to whole numbers if very close to them */
        public static Vector3 RoundVector3(Vector3 v)
        {
            if (Mathf.Abs(v.x - Mathf.Round(v.x)) < 0.001f) v.x = Mathf.Round(v.x);
            if (Mathf.Abs(v.y - Mathf.Round(v.y)) < 0.001f) v.y = Mathf.Round(v.y);
            if (Mathf.Abs(v.z - Mathf.Round(v.z)) < 0.001f) v.z = Mathf.Round(v.z);
            return v;
        }

        public override void OnInspectorGUI(NavGraph target)
        {
            GridGraph graph = target as GridGraph;

            GUIStyle lockStyle = AstarPathEditor.astarSkin.FindStyle("GridSizeLock");
            if (lockStyle == null)
            {
                lockStyle = new GUIStyle();
            }

            // Width / Depth with lock icon
            Rect lockRect;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            int newWidth = EditorGUILayout.IntField(
                new GUIContent("Width (nodes)", "Width of the graph in nodes"),
                graph.width
            );
            int newDepth = EditorGUILayout.IntField(
                new GUIContent("Depth (nodes)", "Depth (or height you might also call it) of the graph in nodes"),
                graph.depth
            );
            GUILayout.EndVertical();

            lockRect = GUILayoutUtility.GetRect(lockStyle.fixedWidth, lockStyle.fixedHeight);
            lockRect.y += 3;  // small visual offset
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            lockRect.width = lockStyle.fixedWidth;
            lockRect.height = lockStyle.fixedHeight;
            lockRect.x += lockStyle.margin.left;
            lockRect.y += lockStyle.margin.top;

            locked = GUI.Toggle(
                lockRect,
                locked,
                new GUIContent("",
                    "If the width and depth values are locked, changing the node size will scale the grid while keeping\n" +
                    "the number of nodes consistent instead of keeping the size the same and changing the number of nodes."),
                lockStyle
            );

            if (newWidth != graph.width || newDepth != graph.depth)
            {
                SnapSizeToNodes(newWidth, newDepth, graph);
            }

            GUI.SetNextControlName("NodeSize");
            newNodeSize = EditorGUILayout.FloatField(
                new GUIContent("Node size", "The size of a single node (side length in world units)"),
                graph.nodeSize
            );

            if (newNodeSize <= 0.01f) newNodeSize = 0.01f;

            float prevRatio = graph.aspectRatio;
            graph.aspectRatio = EditorGUILayout.FloatField(
                new GUIContent("Aspect Ratio", "Scaling of the nodes width/depth ratio. Good for isometric games."),
                graph.aspectRatio
            );

            graph.isometricAngle = EditorGUILayout.FloatField(
                new GUIContent("Isometric Angle", "For an isometric 2D game, you can use this parameter to scale the graph correctly."),
                graph.isometricAngle
            );

            if (graph.nodeSize != newNodeSize || prevRatio != graph.aspectRatio)
            {
                if (!locked)
                {
                    graph.nodeSize = newNodeSize;
                    Matrix4x4 oldMatrix = graph.matrix;
                    graph.GenerateMatrix();
                    if (graph.matrix != oldMatrix)
                    {
                        GUI.changed = true;
                    }
                }
                else
                {
                    float delta = newNodeSize / graph.nodeSize;
                    graph.nodeSize = newNodeSize;
                    graph.unclampedSize = new Vector2(newWidth * graph.nodeSize, newDepth * graph.nodeSize);
                    Vector3 newCenter = graph.matrix.MultiplyPoint3x4(
                        new Vector3((newWidth / 2f) * delta, 0, (newDepth / 2f) * delta)
                    );
                    graph.center = newCenter;
                    graph.GenerateMatrix();

                    // Keep width & depth
                    graph.width = newWidth;
                    graph.depth = newDepth;
                    AutoScan();
                }
            }

            // Pivot / center UI
            Vector3 pivotPoint;
            Vector3 diff;

            GUILayout.BeginHorizontal();
            switch (pivot)
            {
                case GridPivot.Center:
                    graph.center = RoundVector3(graph.center);
                    graph.center = EditorGUILayout.Vector3Field("Center", graph.center);
                    break;
                case GridPivot.TopLeft:
                    pivotPoint = graph.matrix.MultiplyPoint3x4(new Vector3(0, 0, graph.depth));
                    pivotPoint = RoundVector3(pivotPoint);
                    diff = pivotPoint - graph.center;
                    pivotPoint = EditorGUILayout.Vector3Field("Top-Left", pivotPoint);
                    graph.center = pivotPoint - diff;
                    break;
                case GridPivot.TopRight:
                    pivotPoint = graph.matrix.MultiplyPoint3x4(new Vector3(graph.width, 0, graph.depth));
                    pivotPoint = RoundVector3(pivotPoint);
                    diff = pivotPoint - graph.center;
                    pivotPoint = EditorGUILayout.Vector3Field("Top-Right", pivotPoint);
                    graph.center = pivotPoint - diff;
                    break;
                case GridPivot.BottomLeft:
                    pivotPoint = graph.matrix.MultiplyPoint3x4(new Vector3(0, 0, 0));
                    pivotPoint = RoundVector3(pivotPoint);
                    diff = pivotPoint - graph.center;
                    pivotPoint = EditorGUILayout.Vector3Field("Bottom-Left", pivotPoint);
                    graph.center = pivotPoint - diff;
                    break;
                case GridPivot.BottomRight:
                    pivotPoint = graph.matrix.MultiplyPoint3x4(new Vector3(graph.width, 0, 0));
                    pivotPoint = RoundVector3(pivotPoint);
                    diff = pivotPoint - graph.center;
                    pivotPoint = EditorGUILayout.Vector3Field("Bottom-Right", pivotPoint);
                    graph.center = pivotPoint - diff;
                    break;
            }

            graph.GenerateMatrix();
            pivot = PivotPointSelector(pivot);
            GUILayout.EndHorizontal();

            graph.rotation = EditorGUILayout.Vector3Field("Rotation", graph.rotation);

            if (GUILayout.Button(new GUIContent("Snap Size", "Snap the size to exactly fit nodes"),
                GUILayout.MaxWidth(100), GUILayout.MaxHeight(16)))
            {
                SnapSizeToNodes(newWidth, newDepth, graph);
            }

            Separator();

            // Main grid settings
            graph.cutCorners = EditorGUILayout.Toggle(
                new GUIContent("Cut Corners", "Enables or disables cutting corners. See docs for example."),
                graph.cutCorners
            );

            graph.neighbours = (NumNeighbours)EditorGUILayout.EnumPopup(
                new GUIContent("Connections", "How many neighbours each node should connect to."),
                graph.neighbours
            );

            graph.maxClimb = EditorGUILayout.FloatField(
                new GUIContent("Max Climb", "How high, relative to the graph, a climbable step may be. Zero means infinity."),
                graph.maxClimb
            );
            if (graph.maxClimb < 0) graph.maxClimb = 0;

            EditorGUI.indentLevel++;
            graph.maxClimbAxis = EditorGUILayout.IntPopup(
                new GUIContent("Climb Axis", "Which axis to test for Max Climb."),
                graph.maxClimbAxis,
                new GUIContent[3] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") },
                new int[3] { 0, 1, 2 }
            );
            EditorGUI.indentLevel--;

            if (graph.maxClimb > 0 &&
                Mathf.Abs((Quaternion.Euler(graph.rotation) * new Vector3(graph.nodeSize, 0, graph.nodeSize))[graph.maxClimbAxis]) > graph.maxClimb)
            {
                EditorGUILayout.HelpBox(
                    "Nodes are spaced further apart than Max Climb. You might want to increase Max Climb or change the axis.",
                    MessageType.Warning
                );
            }

            graph.maxSlope = EditorGUILayout.Slider(
                new GUIContent("Max Slope", "Maximum walkable slope (degrees). Only used if Height Testing is enabled."),
                graph.maxSlope,
                0,
                90f
            );

            graph.erodeIterations = EditorGUILayout.IntField(
                new GUIContent("Erosion iterations", "How many times the graph should be eroded (adds margins around obstacles)."),
                graph.erodeIterations
            );
            graph.erodeIterations = Mathf.Clamp(graph.erodeIterations, 0, 16);

            if (graph.erodeIterations > 0)
            {
                EditorGUI.indentLevel++;
                graph.erosionUseTags = EditorGUILayout.Toggle(
                    new GUIContent("Erosion Uses Tags",
                        "Instead of making nodes unwalkable, nodes are tagged based on their erosion level.\n" +
                        "This is a good measure of distance to the closest wall."),
                    graph.erosionUseTags
                );

                if (graph.erosionUseTags)
                {
                    EditorGUI.indentLevel++;
                    graph.erosionFirstTag = EditorGUILayoutx.SingleTagField("First Tag", graph.erosionFirstTag);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            DrawCollisionEditor(graph.collision);

            if (graph.collision.use2D)
            {
                if (Mathf.Abs(Vector3.Dot(Vector3.forward, Quaternion.Euler(graph.rotation) * Vector3.up)) < 0.9f)
                {
                    EditorGUILayout.HelpBox(
                        "When using 2D it is recommended to rotate the graph so it aligns with the 2D plane.",
                        MessageType.Warning
                    );
                }
            }

            Separator();
            GUILayout.Label(new GUIContent("Advanced"), EditorStyles.boldLabel);

            showExtra = EditorGUILayout.Foldout(showExtra, "Penalty Modifications");
            if (showExtra)
            {
                EditorGUI.indentLevel += 2;

                graph.penaltyAngle = ToggleGroup(
                    new GUIContent("Angle Penalty", "Adds a penalty based on the slope of the node."),
                    graph.penaltyAngle
                );
                if (graph.penaltyAngle)
                {
                    EditorGUI.indentLevel++;
                    graph.penaltyAngleFactor = EditorGUILayout.FloatField(
                        new GUIContent("Factor", "Scale of the penalty (avoid negative values)."),
                        graph.penaltyAngleFactor
                    );
                    graph.penaltyAnglePower = EditorGUILayout.Slider("Power", graph.penaltyAnglePower, 0.1f, 10f);
                    HelpBox("Penalty = (1 - cos(angle)^power) * factor.");
                    EditorGUI.indentLevel--;
                }

                graph.penaltyPosition = ToggleGroup("Position Penalty", graph.penaltyPosition);
                if (graph.penaltyPosition)
                {
                    EditorGUI.indentLevel++;
                    graph.penaltyPositionOffset = EditorGUILayout.FloatField("Offset", graph.penaltyPositionOffset);
                    graph.penaltyPositionFactor = EditorGUILayout.FloatField("Factor", graph.penaltyPositionFactor);
                    HelpBox(
                        "Applies penalty to nodes based on their Y coordinate.\n" +
                        "Sampled in Int3 space (multiplied by Int3.Precision: " + Int3.Precision + ").\n" +
                        "Be very careful with negative values (they underflow and become huge)."
                    );
                    EditorGUI.indentLevel--;
                }

                GUI.enabled = false;
                ToggleGroup(new GUIContent("Use Texture", AstarPathEditor.AstarProTooltip), false);
                GUI.enabled = true;

                EditorGUI.indentLevel -= 2;
            }
        }

        /** Displays an object field for objects which must be in the 'Resources' folder. */
        [System.Obsolete("Use ObjectField instead")]
        public UnityEngine.Object ResourcesField(string label, UnityEngine.Object obj, System.Type type)
        {
#if UNITY_3_3
            obj = EditorGUILayout.ObjectField(label, obj, type);
#else
            obj = EditorGUILayout.ObjectField(label, obj, type, false);
#endif

            if (obj != null)
            {
                string path = AssetDatabase.GetAssetPath(obj);

                if (!path.Contains("Resources/"))
                {
                    if (FixLabel("Object must be in the 'Resources' folder"))
                    {
                        if (!System.IO.Directory.Exists(UnityEngine.Application.dataPath + "/Resources"))
                        {
                            System.IO.Directory.CreateDirectory(UnityEngine.Application.dataPath + "/Resources");
                            AssetDatabase.Refresh();
                        }

                        string ext = System.IO.Path.GetExtension(path);
                        string error = AssetDatabase.MoveAsset(path, "Assets/Resources/" + obj.name + ext);
                        if (error != "")
                        {
                            Debug.LogError("Couldn't move asset - " + error);
                        }
                    }
                }
            }
            return obj;
        }

        public void SnapSizeToNodes(int newWidth, int newDepth, GridGraph graph)
        {
            graph.unclampedSize = new Vector2(newWidth * graph.nodeSize, newDepth * graph.nodeSize);
            Vector3 newCenter = graph.matrix.MultiplyPoint3x4(new Vector3(newWidth / 2f, 0, newDepth / 2f));
            graph.center = newCenter;
            graph.GenerateMatrix();
            AutoScan();
            GUI.changed = true;
        }

        public static GridPivot PivotPointSelector(GridPivot pivot)
        {
            GUISkin skin = AstarPathEditor.astarSkin;
            GUIStyle background = skin.FindStyle("GridPivotSelectBackground");

            Rect r = GUILayoutUtility.GetRect(19, 19, background);
            r.width = 19;
            r.height = 19;

            if (background == null) return pivot;

            if (Event.current.type == EventType.Repaint)
            {
                background.Draw(r, false, false, false, false);
            }

            if (GUI.Toggle(new Rect(r.x, r.y, 7, 7), pivot == GridPivot.TopLeft, "", skin.FindStyle("GridPivotSelectButton")))
                pivot = GridPivot.TopLeft;

            if (GUI.Toggle(new Rect(r.x + 12, r.y, 7, 7), pivot == GridPivot.TopRight, "", skin.FindStyle("GridPivotSelectButton")))
                pivot = GridPivot.TopRight;

            if (GUI.Toggle(new Rect(r.x + 12, r.y + 12, 7, 7), pivot == GridPivot.BottomRight, "", skin.FindStyle("GridPivotSelectButton")))
                pivot = GridPivot.BottomRight;

            if (GUI.Toggle(new Rect(r.x, r.y + 12, 7, 7), pivot == GridPivot.BottomLeft, "", skin.FindStyle("GridPivotSelectButton")))
                pivot = GridPivot.BottomLeft;

            if (GUI.Toggle(new Rect(r.x + 6, r.y + 6, 7, 7), pivot == GridPivot.Center, "", skin.FindStyle("GridPivotSelectButton")))
                pivot = GridPivot.Center;

            return pivot;
        }

        public override void OnSceneGUI(NavGraph target)
        {
            Event e = Event.current;
            GridGraph graph = target as GridGraph;

            Matrix4x4 matrixPre = graph.matrix;
            graph.GenerateMatrix();

            if (e.type == EventType.MouseDown)
                isMouseDown = true;
            else if (e.type == EventType.MouseUp)
                isMouseDown = false;

            if (!isMouseDown)
            {
                savedMatrix = graph.boundsMatrix;
            }

            Handles.matrix = savedMatrix;

            if ((graph.GetType() == typeof(GridGraph) && graph.nodes == null) ||
                (graph.uniformWidthDepthGrid && graph.depth * graph.width != graph.nodes.Length) ||
                graph.matrix != matrixPre)
            {
                if (AutoScan())
                {
                    GUI.changed = true;
                }
            }

            Matrix4x4 inversed = savedMatrix.inverse;

            Handles.color = AstarColor.BoundsHandles;

            Vector2 extents = graph.unclampedSize * 0.5f;
            Vector3 center = inversed.MultiplyPoint3x4(graph.center);

            if (Tools.current == Tool.Scale)
            {
                Vector3 rightPos = center + new Vector3(extents.x, 0, 0);
                Vector3 forwardPos = center + new Vector3(0, 0, extents.y);
                Vector3 leftPos = center + new Vector3(-extents.x, 0, 0);
                Vector3 backPos = center + new Vector3(0, 0, -extents.y);

                float sizeRight = 0.1f * HandleUtility.GetHandleSize(rightPos);
                float sizeForward = 0.1f * HandleUtility.GetHandleSize(forwardPos);
                float sizeLeft = 0.1f * HandleUtility.GetHandleSize(leftPos);
                float sizeBack = 0.1f * HandleUtility.GetHandleSize(backPos);
                float centerSize = 0.1f * HandleUtility.GetHandleSize(center);

                Vector3 p1 = Handles.Slider(rightPos, Vector3.right, sizeRight, Handles.CylinderHandleCap, 0);
                Vector3 p2 = Handles.Slider(forwardPos, Vector3.forward, sizeForward, Handles.CylinderHandleCap, 0);
                Vector3 p4 = Handles.Slider(leftPos, -Vector3.right, sizeLeft, Handles.CylinderHandleCap, 0);
                Vector3 p5 = Handles.Slider(backPos, -Vector3.forward, sizeBack, Handles.CylinderHandleCap, 0);
                Vector3 p6 = Handles.Slider(center, Vector3.up, centerSize, Handles.CylinderHandleCap, 0);

                Vector3 r1 = new Vector3(p1.x, p6.y, p2.z);
                Vector3 r2 = new Vector3(p4.x, p6.y, p5.z);

                graph.center = savedMatrix.MultiplyPoint3x4((r1 + r2) / 2f);
                Vector3 tmp = r1 - r2;
                graph.unclampedSize = new Vector2(tmp.x, tmp.z);
            }
            else if (Tools.current == Tool.Move)
            {
                if (Tools.pivotRotation == PivotRotation.Local)
                {
                    center = Handles.PositionHandle(center, Quaternion.identity);
                    if (Tools.viewTool != ViewTool.Orbit)
                    {
                        graph.center = savedMatrix.MultiplyPoint3x4(center);
                    }
                }
                else
                {
                    Handles.matrix = Matrix4x4.identity;
                    Vector3 worldCenter = Handles.PositionHandle(graph.center, Quaternion.identity);
                    if (Tools.viewTool != ViewTool.Orbit)
                    {
                        graph.center = worldCenter;
                    }
                }
            }
            else if (Tools.current == Tool.Rotate)
            {
                Handles.matrix = Matrix4x4.identity;
                Quaternion rot = Handles.RotationHandle(Quaternion.Euler(graph.rotation), graph.center);
                if (Tools.viewTool != ViewTool.Orbit)
                {
                    graph.rotation = rot.eulerAngles;
                }
            }

            Handles.matrix = Matrix4x4.identity;
        }

        public enum GridPivot
        {
            Center,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
    }
}