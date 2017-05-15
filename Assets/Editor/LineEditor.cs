using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(Line))]
[Serializable]
public class LineEditor : Editor {

    static bool ShowSettings;
    static bool MaxSmoothStrength;
    static float HandleSize = 0.1f;
    static float RayLength = 0.5f;
    static int Segments = 10;
    static bool DrawSamplePoint;
    static bool DrawNormals;
    static bool DrawTangets;
    static bool DrawCornerCircle;
    static float T;

    private Line Line;
    private int SelectedPointIndex;
    private int LastPointsCount;
    private Dictionary<int,AnchorPoint> SelectedAnchorPoints = new Dictionary<int,AnchorPoint>();
    private AnchorPoint LastSelectedPoint;

    GUIStyle Bold = new GUIStyle();

    private void Init() {
        if (Line == null) {
            Line = (Line) target;
        }
        Bold.fontStyle = FontStyle.Bold;
    }

    private void OnEnable() {
//        Debug.Log(string.Format("OnEnable() - Line={0}", Line));
        Init();
    }

#region Draw callbacks

    public override void OnInspectorGUI() {
        //        DrawDefaultInspector();

        EditorGUILayout.HelpBox("Shortcuts:\n Shift + 1 -> point type None\n Shift + 2 -> point type RoundedCorner", MessageType.Info);

        DrawInspectorSettings();
        DrawInspectorPointControls();
    }

    private void OnSceneGUI() {

        // transformations not yet supported
        Line.transform.position = Vector3.zero;
        Line.transform.localScale = Vector3.one;
        Line.transform.rotation = Quaternion.identity;

        if (Line.AnchorPoints.Count < 2) return;

//        Debug.Log(string.Format("GUIUtility.hotControl={0}", GUIUtility.hotControl));
//        Debug.Log(string.Format("SelectedPointIndex={0}", SelectedPointIndex));
        if (GUIUtility.hotControl != 0 && GUIUtility.hotControl != SelectedPointIndex) {
            SelectedPointIndex = GUIUtility.hotControl;
            if (SelectedAnchorPoints.ContainsKey(SelectedPointIndex)) {
                LastSelectedPoint = SelectedAnchorPoints[SelectedPointIndex];
            }
        }

        if (Line.AnchorPoints.Count != LastPointsCount) {
            LastPointsCount = Line.AnchorPoints.Count;
            Line.ReasignNeighbourPoints();//TEMP -> on debug mode change
        }

        DrawScenePointControls();
        PointTypeShortcuts();

        {//draw segments
            for (int i = 0; i < Segments; i++) {
                float t = (1 / (float) Segments) * i;
                Ray ray = Line.GetPointOnLine(t);
                if (DrawNormals) DrawRay(ray.origin, ray.direction * RayLength, Color.cyan);
                if (DrawTangets) DrawRay(ray.origin, Utils.NormalR(ray.direction) * RayLength, Color.magenta);
            }
        }

        {// draw sample point
            if (!DrawSamplePoint) return;
            Ray ray = Line.GetPointOnLine(T);
            DrawRay(ray.origin, ray.direction * RayLength, Color.green);
            DrawRay(ray.origin, Utils.NormalR(ray.direction) * RayLength, Color.red);
            DrawCrosshair(ray.origin, 0.1f * RayLength, Color.white);
        }
    }

#endregion

#region Draw

    private void DrawInspectorSettings() {


        ShowSettings = EditorGUILayout.Foldout(ShowSettings, "Show settings");
        if (!ShowSettings) return;

        GUILayout.Label("Settings", Bold);

        EditorGUI.BeginChangeCheck();

        Line.ClosedLine = GUILayout.Toggle(Line.ClosedLine, "Closed Line");

        GUILayout.BeginHorizontal();
        DrawSamplePoint = GUILayout.Toggle(DrawSamplePoint, "Sample Point");
        DrawNormals = GUILayout.Toggle(DrawNormals, "Normals");
        DrawTangets = GUILayout.Toggle(DrawTangets, "Tangents");
        DrawCornerCircle = GUILayout.Toggle(DrawCornerCircle, "Corner Circle");
        GUILayout.EndHorizontal();
        
        T = EditorGUILayout.Slider("T", T, 0, 1);
        HandleSize = EditorGUILayout.Slider("Handle Size", HandleSize, 0.1f, 0.3f);
        RayLength = EditorGUILayout.Slider("Ray Length", RayLength, 0, 10);
        Segments = (int) EditorGUILayout.Slider("Segments", Segments, 0, 200);


        if (EditorGUI.EndChangeCheck()) {
            SceneView.RepaintAll();
        }
    }

    private void DrawInspectorPointControls() {

        GUILayout.Label("Points", Bold);

        EditorGUI.BeginChangeCheck();
        if (EditorGUI.EndChangeCheck()) {
            SceneView.RepaintAll();
        }

        for (int i = 0; i < Line.AnchorPoints.Count; i++) {
            AnchorPoint point = Line.AnchorPoints[i];

            string[] pointTypes = System.Enum.GetNames(typeof(PointType));

            GUILayout.BeginHorizontal();
            GUILayout.Label(point.Name);

            {
                EditorGUI.BeginChangeCheck();
                PointType pointType = (PointType) EditorGUILayout.Popup((int) point.PointType, pointTypes);

                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(Line, "Change Point Type");

                    point.PointType = pointType;

                    if (point.PointType == PointType.None)
                        point.ResetValues();
                
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("X", GUILayout.Width(15), GUILayout.Height(15))) {
//                RemovePoint(point);
                Line.RemovePoint(i);
                SceneView.RepaintAll();
            }
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            Vector3 position = EditorGUILayout.Vector3Field("    Position: ", point.Position);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(Line, "Move Point");
                point.Position = position;
                SceneView.RepaintAll();
            }

            if (point.PointType == PointType.RoundedCorner) {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
//                GUILayout.Label("    Smooth Strength:", GUILayout.Width(170));
                float defaultLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 147;
                float smoothStrength = EditorGUILayout.FloatField("    Smooth Strength:", point.SmoothStrength, GUILayout.Width(212));
                EditorGUIUtility.labelWidth = defaultLabelWidth;
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(Line, "SmoothStrength");
                    point.SmoothStrength = smoothStrength;
                    SceneView.RepaintAll();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        if (GUILayout.Button("+", GUILayout.Width(18))) {
            Undo.RecordObject(Line, "Point added");
            AnchorPoint lastPoint = Line.AnchorPoints[Line.AnchorPoints.Count - 1];
            Line.AddPointAtTheEnd(lastPoint.Position + Vector2.right);
            SceneView.RepaintAll();
        }
    }

    private void DrawScenePointControls() {

        int pointsCount = Line.AnchorPoints.Count;

        Line.ReasignNeighbourPoints();

        if (SelectedAnchorPoints == null || SelectedAnchorPoints.Count < Line.AnchorPoints.Count)
            SelectedAnchorPoints = new Dictionary<int, AnchorPoint>();

        for (int i = 0; i < pointsCount; i++) {
            DrawScenePointControl(Line.AnchorPoints[i]);

            if (!Line.ClosedLine && i == pointsCount - 1) continue;

            int indexNext = (i + 1) % pointsCount;
            int indexPrevious = (i - 1 < 0) ? pointsCount - 1 : i - 1;
            AnchorPoint point = Line.AnchorPoints[i];
            AnchorPoint pointNext = Line.AnchorPoints[indexNext];
            AnchorPoint pointPrevious = Line.AnchorPoints[indexPrevious];

            // draw line to the next point
            if (point.PointType != PointType.RoundedCorner && pointNext.PointType != PointType.RoundedCorner) {
            }
            if (point.PointType == PointType.RoundedCorner) {
                Handles.color = Color.grey;
                Handles.DrawLine(point.Position, point.Position + point.LeftTangent);
                Handles.DrawLine(point.Position, point.Position + point.RightTangent);
            } else {
                Handles.color = Color.white;
                if (pointNext.PointType == PointType.RoundedCorner)
                    Handles.DrawLine(point.Position, pointNext.Position + pointNext.LeftTangent);
                else
                    Handles.DrawLine(point.Position, pointNext.Position);
            }
        }

        for (int i = 0; i < pointsCount; i++) {
            int indexNext = (i + 1) % pointsCount;
            int indexPrevious = (i - 1 < 0) ? pointsCount - 1 : i - 1;
            AnchorPoint point = Line.AnchorPoints[i];
            AnchorPoint pointNext = Line.AnchorPoints[indexNext];
            AnchorPoint pointPrevious = Line.AnchorPoints[indexPrevious];

            // draw rounded corner
            if (point.PointType == PointType.RoundedCorner) {
                if (pointNext != null && pointPrevious != null) {
                    bool isSelected = SelectedAnchorPoints.ContainsKey(SelectedPointIndex) && point == SelectedAnchorPoints[SelectedPointIndex];
                    DrawScenePointCornerControl(point, isSelected);
                }
            }
        }
    }

    private void DrawScenePointControl(AnchorPoint controlPoint) {

        EditorGUI.BeginChangeCheck();

        Transform handleTransform = Line.transform;
        Vector3 p0 = handleTransform.TransformPoint(controlPoint.Position);


        float handleSize = HandleUtility.GetHandleSize(p0) * HandleSize;

        Handles.color = Color.white;
        p0 = Handles.FreeMoveHandle(p0, Quaternion.identity, handleSize, Vector3.zero, (controlID, position, rotation, size) => {
            if (!SelectedAnchorPoints.ContainsKey(controlID))
                SelectedAnchorPoints.Add(controlID, controlPoint);
            Handles.RectangleCap(controlID, position, rotation, size);
        });

//        DrawRay(controlPoint.Position, controlPoint.Normal, Color.red);

        string label = controlPoint.Name;
        Handles.Label(p0 + Vector3.right * 0.5f, label);

        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(Line, string.Format("Move Point ({0})", controlPoint.Name));
            controlPoint.Position = handleTransform.InverseTransformPoint(p0);
        }
    }

    private void DrawScenePointCornerControl(AnchorPoint point, bool isSelected) {
        point.RecalculateCornerValues();

        AnchorPoint pointPrevious = point.PreviousPoint;
        AnchorPoint pointNext = point.NextPoint;

        Vector2 A = pointPrevious.Position;
        Vector2 B = point.Position;
        Vector2 C = pointNext.Position;
        Vector2 AC = C - A;
        Vector2 BA = A - B;
        Vector2 BC = C - B;

        // draw circle
        Assert.IsNotNull(point);
        Assert.IsNotNull(pointNext);
        Assert.IsNotNull(pointPrevious);

        Vector2 arcCenter = B + point.Vh;

//        if (isSelected)
        if (DrawCornerCircle) {
            Handles.color = Color.gray;
            Handles.DrawWireDisc(arcCenter, Vector3.forward, point.Radius);
        }

        {// draw arc
            Vector2 ACnl = Utils.NormalL(AC);
            bool isConvex = Vector2.Dot(ACnl, point.Vh) < 0;

//        DrawRay(B, ACnl, Color.red);
//        DrawRay(B, point.Vh, Color.grey);

            Vector2 startAngleVector = isConvex ? Utils.NormalL(BC) : Utils.NormalL(BA);
//        DrawRay(arcCenter, startAngleVector, Color.magenta);

            Handles.color = Color.white;
            Handles.DrawWireArc(arcCenter, Vector3.forward, startAngleVector, point.ArcAngle, point.Radius);
        }

        {// draw sides
//        Handles.color = Color.red;
            if (pointNext != null && pointNext.PointType == PointType.RoundedCorner) {
                Vector2 start = B + point.RightTangent;
                Vector2 end = pointNext.Position + pointNext.LeftTangent;
                Handles.DrawLine(start, (start + end) * 0.5f);
            } else {
                Handles.DrawLine(B + point.RightTangent, C);
            }

//        Handles.color = Color.blue;
            if (pointPrevious != null && pointPrevious.PointType == PointType.RoundedCorner) {
                Vector2 start = B + point.LeftTangent;
                Vector2 end = pointPrevious.Position + pointPrevious.RightTangent;
                Handles.DrawLine(start, (start + end) * 0.5f);
            } else {
                Handles.DrawLine(A, B + point.LeftTangent);
            }
        }

//        Handles.color = Color.grey;
//        Handles.DrawLine(B + point.LeftTangent, B + point.Vh);
//        Handles.DrawLine(B + point.RightTangent, B + point.Vh);

//        if (isSelected)
        if (DrawCornerCircle) {
            float size = 0.2f;
            Vector2 leftNormal = Utils.NormalL(BA) * size * 0.5f;
            Vector2 rightNormal = Utils.NormalR(BC) * size * 0.5f;
            DrawRay(B + point.LeftTangent - leftNormal, leftNormal * 2, Color.gray);
            DrawRay(B + point.RightTangent - rightNormal, rightNormal * 2, Color.gray);
//            DrawRay(B + point.Vh, point.StartVector, Color.yellow);
        }


//        if (isSelected) {
        DrawRay(B + point.Vh - point.VhN * point.Radius, point.VhN * point.Radius, Color.grey);

        EditorGUI.BeginChangeCheck();
        {
            Vector2 smoothVector = B + point.Vh;

            float handleSize = HandleUtility.GetHandleSize(point.Position) * HandleSize;

            Vector2 sliderPosition = Handles.Slider(smoothVector, point.VhN, handleSize, (controlID, position, rotation, size) => {
                if (!SelectedAnchorPoints.ContainsKey(controlID))
                    SelectedAnchorPoints.Add(controlID, point);
                Handles.SphereCap(controlID, position, Quaternion.Euler(0, 0, 0), handleSize);
            }, 1f);

            Vector2 newSmoothVector = sliderPosition - B;

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(Line, "Change Corner Smooth Strength");
                point.SmoothStrength = Mathf.Max(0, newSmoothVector.magnitude);
            }
        }
//        }
    }


#endregion

#region Shortcuts

    private void PointTypeShortcuts() {
        if (LastSelectedPoint != null) {
            if (Event.current.shift) {
                if (Event.current.type == EventType.keyDown) {
                    if (Event.current.keyCode == KeyCode.Alpha2) {
                        Undo.RecordObject(Line, "Change node type");
                        LastSelectedPoint.PointType = PointType.RoundedCorner;
                        LastSelectedPoint.RecalculateCornerValues();
                        SceneView.RepaintAll();
                    }
                    if (Event.current.keyCode == KeyCode.Alpha1) {
                        Undo.RecordObject(Line, "Change node type");
                        LastSelectedPoint.PointType = PointType.None;
                        LastSelectedPoint.RecalculateCornerValues();
                        LastSelectedPoint.ResetValues();
                        SceneView.RepaintAll();
                    }
                }
            }
        }
    }

#endregion

#region Helpers

    private void DrawRay(Vector2 origin, Vector2 direction, Color color) {
        Handles.color = color;
        Handles.DrawLine(origin, origin + direction);
    }

    private void DrawRay(Ray ray, Color color) {
        Handles.color = color;
        Handles.DrawLine(ray.origin, ray.origin + ray.direction);
    }

    private void DrawCrosshair(Vector2 center, float size, Color color) {
        Handles.color = color;
        Handles.DrawWireDisc(center, Vector3.forward, size * 0.5f);
        Handles.DrawLine(center + Vector2.up * size, center + Vector2.down * size);
        Handles.DrawLine(center + Vector2.left * size, center + Vector2.right * size);
    }

#endregion
}
