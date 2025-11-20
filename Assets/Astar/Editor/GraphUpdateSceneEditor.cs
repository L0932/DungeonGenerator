using Pathfinding;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

[CustomEditor(typeof(GraphUpdateScene))]
/** Editor for GraphUpdateScene */
public class GraphUpdateSceneEditor : Editor
{
    int selectedPoint = -1;

    const float pointGizmosRadius = 0.09f;
    static readonly Color PointColor = new Color(1f, 0.36f, 0f, 0.6f);
    static readonly Color PointSelectedColor = new Color(1f, 0.24f, 0f, 1.0f);

    public override void OnInspectorGUI()
    {
        GraphUpdateScene script = (GraphUpdateScene)target;

        Undo.RecordObject(script, "Modify GraphUpdateScene settings");

        if (script.points == null)
            script.points = new Vector3[0];

        if (script.points == null || script.points.Length == 0)
        {
            if (script.GetComponent<Collider>() != null)
            {
                EditorGUILayout.HelpBox("No points, using collider.bounds", MessageType.Info);
            }
            else if (script.GetComponent<Renderer>() != null)
            {
                EditorGUILayout.HelpBox("No points, using renderer.bounds", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No points and no collider or renderer attached, will not affect anything.\n" +
                    "Points can be added using the transform tool and holding Shift.",
                    MessageType.Warning
                );
            }
        }

        Vector3[] prePoints = script.points;

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("points"), true);
        serializedObject.ApplyModifiedProperties();

        EditorGUI.indentLevel = 0;

        script.updatePhysics = EditorGUILayout.Toggle(
            new GUIContent(
                "Update Physics",
                "Perform similar calculations on the nodes as during scan.\n" +
                "Grid Graphs will update the position of the nodes and also check walkability using collision.\n" +
                "See online documentation for more info."
            ),
            script.updatePhysics
        );

        if (script.updatePhysics)
        {
            EditorGUI.indentLevel++;
            script.resetPenaltyOnPhysics = EditorGUILayout.Toggle(
                new GUIContent(
                    "Reset Penalty On Physics",
                    "Will reset the penalty to the default value during the update."
                ),
                script.resetPenaltyOnPhysics
            );
            EditorGUI.indentLevel--;
        }

        script.updateErosion = EditorGUILayout.Toggle(
            new GUIContent(
                "Update Erosion",
                "Recalculate erosion for grid graphs.\nSee online documentation for more info."
            ),
            script.updateErosion
        );

        if (prePoints != script.points)
        {
            script.RecalcConvex();
            HandleUtility.Repaint();
        }

        bool preConvex = script.convex;
        script.convex = EditorGUILayout.Toggle(
            new GUIContent(
                "Convex",
                "Sets if only the convex hull of the points should be used or the whole polygon"
            ),
            script.convex
        );
        if (script.convex != preConvex)
        {
            script.RecalcConvex();
            HandleUtility.Repaint();
        }

        script.minBoundsHeight = EditorGUILayout.FloatField(
            new GUIContent(
                "Min Bounds Height",
                "Defines a minimum height to be used for the bounds of the GUO.\n" +
                "Useful if you define points in 2D (which would give height 0)."
            ),
            script.minBoundsHeight
        );

        script.applyOnStart = EditorGUILayout.Toggle("Apply On Start", script.applyOnStart);
        script.applyOnScan = EditorGUILayout.Toggle("Apply On Scan", script.applyOnScan);

        script.modifyWalkability = EditorGUILayout.Toggle(
            new GUIContent(
                "Modify Walkability",
                "If true, walkability of all nodes will be modified."
            ),
            script.modifyWalkability
        );

        if (script.modifyWalkability)
        {
            EditorGUI.indentLevel++;
            script.setWalkability = EditorGUILayout.Toggle(
                new GUIContent(
                    "Walkability",
                    "Nodes' walkability will be set to this value."
                ),
                script.setWalkability
            );
            EditorGUI.indentLevel--;
        }

        script.penaltyDelta = EditorGUILayout.IntField(
            new GUIContent(
                "Penalty Delta",
                "A penalty will be added to the nodes, usually you need very large values, " +
                "at least 1000–10000.\nA higher penalty will mean that agents will try to avoid those nodes."
            ),
            script.penaltyDelta
        );

        if (script.penaltyDelta < 0)
        {
            EditorGUILayout.HelpBox(
                "Be careful when lowering the penalty. Negative penalties are not supported and will instead " +
                "underflow and get really high.\n" +
                "You can set an initial penalty on graphs (see their settings) and then lower them like this " +
                "to get regions which are easier to traverse.",
                MessageType.Warning
            );
        }

        script.modifyTag = EditorGUILayout.Toggle(
            new GUIContent(
                "Modify Tags",
                "Should the tags of the nodes be modified."
            ),
            script.modifyTag
        );

        if (script.modifyTag)
        {
            EditorGUI.indentLevel++;
            script.setTag = EditorGUILayout.Popup(
                "Set Tag",
                script.setTag,
                AstarPath.FindTagNames()
            );
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button(
            "Tags can be used to restrict which units can walk on what ground. Click here for more info",
            "HelpBox"))
        {
            UnityEngine.Application.OpenURL(AstarPathEditor.GetURL("tags"));
        }

        EditorGUILayout.Space();

        bool worldSpace = EditorGUILayout.Toggle(
            new GUIContent(
                "Use World Space",
                "Specify coordinates in world space or local space. When using local space you can move " +
                "the GameObject around and the points will follow.\n" +
                "Some operations, like calculating the convex hull, and snapping to Y will change axis " +
                "depending on how the object is rotated if world space is not used."
            ),
            script.useWorldSpace
        );

        if (worldSpace != script.useWorldSpace)
        {
            Undo.RecordObject(script, "Switch Use World Space");
            script.ToggleUseWorldSpace();
        }

        script.lockToY = EditorGUILayout.Toggle("Lock to Y", script.lockToY);

        if (script.lockToY)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            float newY = EditorGUILayout.FloatField("Lock to Y value", script.lockToYValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(script, "Change Y locking");
                script.lockToYValue = newY;
                script.LockToY();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear all points"))
        {
            Undo.RecordObject(script, "Remove All Points");
            script.points = new Vector3[0];
            script.RecalcConvex();
            EditorUtility.SetDirty(script);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(script);
        }
    }

    public void OnSceneGUI()
    {
        GraphUpdateScene script = (GraphUpdateScene)target;

        if (script.points == null)
            script.points = new Vector3[0];

        List<Vector3> points = Pathfinding.Util.ListPool<Vector3>.Claim();
        points.AddRange(script.points);

        Matrix4x4 invMatrix = script.useWorldSpace ? Matrix4x4.identity : script.transform.worldToLocalMatrix;

        if (!script.useWorldSpace)
        {
            Matrix4x4 matrix = script.transform.localToWorldMatrix;
            for (int i = 0; i < points.Count; i++)
                points[i] = matrix.MultiplyPoint3x4(points[i]);
        }

        if (Tools.current != Tool.View && Event.current.type == EventType.Layout)
        {
            for (int i = 0; i < script.points.Length; i++)
            {
                HandleUtility.AddControl(-i - 1, HandleUtility.DistanceToLine(points[i], points[i]));
            }
        }

        if (Tools.current != Tool.View)
            HandleUtility.AddDefaultControl(0);

        for (int i = 0; i < points.Count; i++)
        {
            if (i == selectedPoint && Tools.current == Tool.Move)
            {
                Handles.color = PointSelectedColor;
                Undo.RecordObject(script, "Move Point");

                float size = HandleUtility.GetHandleSize(points[i]) * pointGizmosRadius * 2f;
                Handles.SphereHandleCap(
                    -i - 1,
                    points[i],
                    Quaternion.identity,
                    size,
                    EventType.Repaint
                );

                Vector3 pre = points[i];
                Vector3 post = Handles.PositionHandle(points[i], Quaternion.identity);
                if (pre != post)
                {
                    script.points[i] = invMatrix.MultiplyPoint3x4(post);
                }
            }
            else
            {
                Handles.color = PointColor;
                float size = HandleUtility.GetHandleSize(points[i]) * pointGizmosRadius;
                Handles.SphereHandleCap(
                    -i - 1,
                    points[i],
                    Quaternion.identity,
                    size,
                    EventType.Repaint
                );
            }
        }

        if (Event.current.type == EventType.MouseDown)
        {
            int pre = selectedPoint;
            selectedPoint = -(HandleUtility.nearestControl + 1);
            if (pre != selectedPoint)
                GUI.changed = true;
        }

        // Shift + Click behaviors
        if (Event.current.type == EventType.MouseDown && Event.current.shift && Tools.current == Tool.Move)
        {
            // Alt + Shift: remove point
            if ((Event.current.modifiers & EventModifiers.Alt) != 0)
            {
                if (selectedPoint >= 0 && selectedPoint < points.Count)
                {
                    Undo.RecordObject(script, "Remove Point");
                    List<Vector3> arr = new List<Vector3>(script.points);
                    arr.RemoveAt(selectedPoint);
                    points.RemoveAt(selectedPoint);
                    script.points = arr.ToArray();
                    script.RecalcConvex();
                    GUI.changed = true;
                }
            }
            // Ctrl + Shift: insert point on nearest segment
            else if ((Event.current.modifiers & EventModifiers.Control) != 0 && points.Count > 1)
            {
                int minSeg = 0;
                float minDist = float.PositiveInfinity;
                for (int i = 0; i < points.Count; i++)
                {
                    float dist = HandleUtility.DistanceToLine(points[i], points[(i + 1) % points.Count]);
                    if (dist < minDist)
                    {
                        minSeg = i;
                        minDist = dist;
                    }
                }

                object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition));
                if (hit is RaycastHit rayhit)
                {
                    Undo.RecordObject(script, "Add Point");

                    List<Vector3> arr = Pathfinding.Util.ListPool<Vector3>.Claim();
                    arr.AddRange(script.points);

                    points.Insert(minSeg + 1, rayhit.point);
                    Vector3 localPoint = script.useWorldSpace ? rayhit.point : invMatrix.MultiplyPoint3x4(rayhit.point);

                    arr.Insert(minSeg + 1, localPoint);
                    script.points = arr.ToArray();
                    script.RecalcConvex();
                    Pathfinding.Util.ListPool<Vector3>.Release(arr);
                    GUI.changed = true;
                }
            }
            // Shift only: add new point at ray hit
            else
            {
                object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition));
                if (hit is RaycastHit rayhit)
                {
                    Undo.RecordObject(script, "Add Point");

                    Vector3[] arr = new Vector3[script.points.Length + 1];
                    for (int i = 0; i < script.points.Length; i++)
                        arr[i] = script.points[i];

                    points.Add(rayhit.point);
                    Vector3 localPoint = script.useWorldSpace ? rayhit.point : invMatrix.MultiplyPoint3x4(rayhit.point);

                    arr[script.points.Length] = localPoint;
                    script.points = arr;
                    script.RecalcConvex();
                    GUI.changed = true;
                }
            }

            Event.current.Use();
        }

        Pathfinding.Util.ListPool<Vector3>.Release(points);

        if (GUI.changed)
        {
            HandleUtility.Repaint();
            EditorUtility.SetDirty(script);
        }
    }
}
