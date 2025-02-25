using System;
using LiteBattle.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LiteBattle.Editor
{
    public class LiteStateSceneView : IDisposable
    {
        public LiteStateEditor StateEditor { get; }

        private readonly Color RangeColor_ = new Color(1f, 0f, 0f, 0.5f);
        private readonly BoxBoundsHandle BoxHandle_ = new BoxBoundsHandle();
        private readonly SphereBoundsHandle SphereHandle_ = new SphereBoundsHandle();
        
        public LiteStateSceneView(LiteStateEditor stateEditor)
        {
            StateEditor = stateEditor;
        }

        public void Dispose()
        {
        }

        public void Draw(SceneView sceneView)
        {
            DrawAttackRange();
        }

        private void DrawAttackRange()
        {
            // var selectObj = Selection.activeObject;
            //
            // if (selectObj is LiteTimelineStateClip { Event: LiteAttackEvent clipAttackEvent })
            // {
            //     DrawAttackRange(clipAttackEvent.Range);
            // }
            // else if (selectObj is LiteTimelineStateMarker { Event: LiteAttackEvent markerAttackEvent })
            // {
            //     DrawAttackRange(markerAttackEvent.Range);
            // }

            if (LiteEditorBinder.Instance.IsBindAttackRange())
            {
                var range = LiteEditorBinder.Instance.GetAttackRange();
                DrawAttackRange(range);
                ControlAttackRange(range);
            }
        }

        private void DrawAttackRange(ILiteRange range)
        {
            var agentBinder = LiteEditorBinder.Instance.GetAgentBinder();
            if (agentBinder == null)
            {
                return;
            }
            
            if (range is LiteBoxRange boxRange)
            {
                LiteHandles.DrawBox(agentBinder.transform.position + boxRange.Offset, boxRange.Size, RangeColor_);
            }
            else if (range is LiteSphereRange sphereRange)
            {
                LiteHandles.DrawSphere(agentBinder.transform.position + sphereRange.Offset, sphereRange.Radius, RangeColor_);
            }
        }

        private void ControlAttackRange(ILiteRange range)
        {
            var offset = Vector3.zero;
            var size = Vector3.one;

            switch (range)
            {
                case LiteBoxRange boxRange:
                    offset = boxRange.Offset;
                    size = boxRange.Size;
                    break;
                case LiteSphereRange sphereRange:
                    offset = sphereRange.Offset;
                    size = new Vector3(sphereRange.Radius, sphereRange.Radius, sphereRange.Radius);
                    break;
                default:
                    return;
            }
            
            switch (Tools.current)
            {
                case Tool.Move:
                    offset = Handles.DoPositionHandle(offset, Quaternion.identity);
                    break;
                case Tool.Scale:
                    size = Handles.DoScaleHandle(size, offset, Quaternion.identity, HandleUtility.GetHandleSize(offset));
                    break;
                case Tool.Rect:
                    switch (range)
                    {
                        case LiteBoxRange:
                            BoxHandle_.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y | PrimitiveBoundsHandle.Axes.Z;
                            BoxHandle_.center = offset;
                            BoxHandle_.size = size;
                            BoxHandle_.DrawHandle();
                            offset = BoxHandle_.center;
                            size = BoxHandle_.size;
                            break;
                        case LiteSphereRange:
                            SphereHandle_.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y | PrimitiveBoundsHandle.Axes.Z;
                            SphereHandle_.center = offset;
                            SphereHandle_.radius = size.x;
                            offset = SphereHandle_.center;
                            size.x = SphereHandle_.radius;
                            break;
                    }
                    break;
                case Tool.Transform:
                    Handles.TransformHandle(ref offset, Quaternion.identity, ref size);
                    break;
            }

            switch (range)
            {
                case LiteBoxRange boxRange:
                    boxRange.Offset = offset.Round(3);
                    boxRange.Size = size.Round(3);
                    break;
                case LiteSphereRange sphereRange:
                    sphereRange.Offset = offset.Round(3);
                    sphereRange.Radius = (float) Math.Round(size.x, 3);
                    break;
            }
        }
    }
}