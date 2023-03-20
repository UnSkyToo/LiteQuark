using System;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace InfiniteGame.Editor
{
    [CustomEditor(typeof(CurveDataBinder))]
    public sealed class CurveEditor : UnityEditor.Editor
    {
        // private bool CustomDrawer;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            // EditorGUI.BeginChangeCheck();
            // CustomDrawer = EditorGUILayout.Toggle("Custom Drawer", CustomDrawer);
            // if (EditorGUI.EndChangeCheck())
            // {
            //     SceneView.lastActiveSceneView.Repaint();
            // }

            var curve = (target as CurveDataBinder)?.Curve;
            if (curve != null)
            {
                curve.Loop = EditorGUILayout.Toggle("Loop", curve.Loop);
                EditorGUILayout.Space(2);
                for (var index = 0; index < curve.Points.Length / 4; ++index)
                {
                    EditorGUILayout.LabelField($"Curve {index + 1}", EditorStyles.boldLabel);
                    curve.Points[index * 4 + 0] = EditorGUILayout.Vector2Field("Begin", curve.Points[index * 4 + 0]);
                    curve.Points[index * 4 + 1] = EditorGUILayout.Vector2Field("Ctrl1", curve.Points[index * 4 + 1]);
                    curve.Points[index * 4 + 2] = EditorGUILayout.Vector2Field("Ctrl2", curve.Points[index * 4 + 2]);
                    curve.Points[index * 4 + 3] = EditorGUILayout.Vector2Field("End", curve.Points[index * 4 + 3]);
                    EditorGUILayout.Space(2);
                }

                if (GUILayout.Button("Add Curve"))
                {
                    Array.Resize(ref curve.Points, curve.Points.Length + 4);
                }
            }
        }

        private void OnSceneGUI()
        {
            var curve = (target as CurveDataBinder)?.Curve;
            if (curve == null)
            {
                return;
            }
            
            EditorGUI.BeginChangeCheck();

            for (var index = 0; index < curve.Points.Length / 4; ++index)
            {
                var p0 = DrawPoint(curve, index * 4 + 0);
                var p1 = DrawPoint(curve, index * 4 + 1);
                var p2 = DrawPoint(curve, index * 4 + 2);
                var p3 = DrawPoint(curve, index * 4 + 3);
                DrawBezier(p0, p3, p1, p2, Color.white, 2f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
        }

        private Vector3 DrawPoint(CurveData curve, int index)
        {
            var point = curve.Points[index];
            Handles.DrawSolidRectangleWithOutline(new Rect(point.x - 0.25f, point.y - 0.25f, 0.5f, 0.5f), Color.yellow, Color.red);
            EditorGUI.BeginChangeCheck();
            point = Handles.DoPositionHandle(point, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(curve, "Move Point");
                EditorUtility.SetDirty(curve);
                curve.Points[index] = point;
            }
            return point;
        }

        private void DrawBezier(Vector2 start, Vector2 end, Vector2 ctrl1, Vector2 ctrl2, Color color, float width)
        {
            // if (CustomDrawer)
            // {
            //     var curve = BezierCurveFactory.CreateBezierCurve(start, ctrl1, ctrl2, end);
            //     var max = 100;
            //     for (var index = 0; index < max; ++index)
            //     {
            //         var col = Handles.color;
            //         Handles.color = Color.red;
            //         Handles.DrawLine(curve.Lerp(index / (float)max), curve.Lerp((index + 1) / (float)max));
            //         Handles.color = col;
            //     }
            // }
            // else
            {
                Handles.DrawBezier(start, end, ctrl1, ctrl2, color, null, width);
            }
        }
    }
}