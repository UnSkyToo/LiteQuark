using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public static class HandleUtils
    {
        private static readonly Color OutlineColor_ = new Color(1f, 1f, 1f, Handles.color.a);
        private static readonly Stack<Color> ColorStack_ = new Stack<Color>();

        [DebuggerStepThrough]
        public static void DrawBox(Vector3 position, Vector3 size, Color color)
        {
            DrawBox(Matrix4x4.Translate(position), size, color);
        }

        [DebuggerStepThrough]
        public static void DrawBox(Matrix4x4 matrix, Vector3 size, Color color)
        {
            ApplyColor(color);
            
            var points = GetBoxVertices(matrix, size);
            var indices = GetBoxSurfaceIndex();
            for (var i = 0; i < 6; ++i)
            {
                var vertices = new Vector3[]
                {
                    points[indices[i * 4]],
                    points[indices[i * 4 + 1]],
                    points[indices[i * 4 + 2]],
                    points[indices[i * 4 + 3]]
                };
                DrawPolygon(vertices, true);
                
                ApplyColor(OutlineColor_);
                DrawPolygon(vertices, false);
                RevertColor();
            }
            
            RevertColor();
        }

        [DebuggerStepThrough]
        public static void DrawSphere(Vector3 position, float radius, Color color)
        {
            DrawSphere(Matrix4x4.Translate(position), radius, color);
        }

        [DebuggerStepThrough]
        public static void DrawSphere(Matrix4x4 matrix, float radius, Color color)
        {
            ApplyColor(color);

            var sceneView = SceneView.currentDrawingSceneView;
            if (sceneView != null)
            {
                var rotation = Quaternion.LookRotation(sceneView.camera.transform.position - matrix.MultiplyPoint(Vector3.zero));
                var lookMatrix = Matrix4x4.TRS(matrix.MultiplyPoint(Vector3.zero), rotation, matrix.lossyScale);
                DrawCircle(lookMatrix, radius, true);
            }

            ApplyColor(OutlineColor_);
            DrawCircle(matrix, radius, false);
            DrawCircle(matrix * Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)), radius, false);
            DrawCircle(matrix * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)), radius, false);
            RevertColor();
            
            RevertColor();
        }

        [DebuggerStepThrough]
        private static void DrawCircle(Matrix4x4 matrix, float radius, bool fill)
        {
            var vertices = GetSphereVertices(matrix, radius);
            DrawPolygon(vertices, fill);
        }

        [DebuggerStepThrough]
        private static void DrawPolygon(Vector3[] vertices, bool fill)
        {
            if (fill)
            {
                Handles.DrawAAConvexPolygon(vertices);
            }
            else
            {
                for (int i = vertices.Length - 1, j = 0; j < vertices.Length; i = j, ++j)
                {
                    DrawLine(vertices[i], vertices[j]);
                }
            }
        }

        [DebuggerStepThrough]
        private static void DrawLine(Vector3 start, Vector3 end)
        {
            Handles.DrawLine(start, end);
        }

        [DebuggerStepThrough]
        private static void ApplyColor(Color color)
        {
            ColorStack_.Push(Handles.color);
            Handles.color = color;
        }

        [DebuggerStepThrough]
        private static void RevertColor()
        {
            if (ColorStack_.Count > 0)
            {
                Handles.color = ColorStack_.Pop();
            }
        }
        
        private static Vector3[] GetSphereVertices(Matrix4x4 matrix, float radius, int step = 30)
        {
            const float TwoPI = Mathf.PI * 2f;
            var deltaDeg = TwoPI / step;
            var vertices = new Vector3[step];
            
            for (var i = 0; i < step; ++i)
            {
                var degree = TwoPI - deltaDeg * i;
                var pos = new Vector3(radius * Mathf.Cos(degree), radius * Mathf.Sin(degree), 0f);
                vertices[i] = matrix.MultiplyPoint(pos);
            }

            return vertices;
        }
        
        private static Vector3[] GetBoxVertices(Matrix4x4 matrix, Vector3 size)
        {
            var halfSize = size / 2f;

            var points = new Vector3[8];

            //  3 → 0
            //  ↑   ↓
            //  2 ← 1
            points[0] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
            points[1] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            points[2] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            points[3] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            
            //  5 ← 4
            //  ↓   ↑
            //  6 → 7
            points[4] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            points[5] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            points[6] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            points[7] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);

            for (var i = 0; i < points.Length; ++i)
            {
                points[i] = matrix.MultiplyPoint(points[i]);
            }

            return points;
        }
        
        private static int[] GetBoxSurfaceIndex()
        {
            return new int[]
            {
                0, 1, 2, 3, //上
                4, 5, 6, 7, //下
                2, 6, 5, 3, //左
                0, 4, 7, 1, //右
                1, 7, 6, 2, //前
                0, 3, 5, 4 //后
            };
        }
    }
}