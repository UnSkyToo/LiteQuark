using System;
using System.Reflection;
using LiteBattle.Runtime;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public static class LiteTimelineHelper
    {
        public static void SetTimeline(TimelineAsset asset)
        {
            var win = TimelineEditor.GetWindow();
            if (win != null)
            {
                win.SetTimeline(asset);
            }
        }
        
        public static void ClearTimeline()
        {
            var win = TimelineEditor.GetWindow();
            if (win != null)
            {
                win.ClearTimeline();
            }
        }

        public static void SaveTimelineChanges()
        {
            var win = TimelineEditor.GetWindow();
            if (win != null)
            {
                win.SaveChanges();
            }
        }
        
        public static void RepaintTimeline()
        {
            var win = TimelineEditor.GetWindow();
            if (win != null)
            {
                win.Repaint();
            }
        }
        
        public static void SetVisibleTimeRange(Vector2 range)
        {
            var editorType = typeof(TimelineEditor);
            var visibleTimeRangeProperty = editorType.GetProperty("visibleTimeRange", BindingFlags.Static | BindingFlags.NonPublic);
            if (visibleTimeRangeProperty == null)
            {
                return;
            }
            
            visibleTimeRangeProperty.SetValue(null, range);
        }

        public static double GetFrameRate()
        {
            return TimelineEditor.inspectedAsset != null
                ? TimelineEditor.inspectedAsset.editorSettings.frameRate
                : 60d; // : TimelineAsset.EditorSettings.kDefaultFrameRate;
        }

        public static int TimeToFrame(double time)
        {
            var typeUtilType = typeof(TimelineClip).Assembly.GetType("UnityEngine.Timeline.TimeUtility");
            if (typeUtilType == null)
            {
                throw new Exception("can't find TimeUtility type");
            }

            var toFrameMethod = typeUtilType.GetMethod("ToFrames", BindingFlags.Static | BindingFlags.Public);
            if (toFrameMethod == null)
            {
                throw new Exception("can't find ToFrames in TimeUtility Type");
            }

            var frameRate = GetFrameRate();
            var frame = toFrameMethod.Invoke(null, new object[] { time, frameRate });
            return (int) frame;
        }

        public static double FrameToTime(int frame)
        {
            return frame / GetFrameRate();
        }

        public static void SetTrackIcon<T>(GUIContent content) where T : TrackAsset
        {
            var resourceCacheType = typeof(TimelineEditor).Assembly.GetType("UnityEditor.Timeline.TrackResourceCache");
            if (resourceCacheType == null)
            {
                throw new Exception("can't find TrackResourceCache type");
            }
            
            var setIconMethod = resourceCacheType.GetMethod("SetTrackIcon", BindingFlags.Static | BindingFlags.Public);
            if (setIconMethod == null)
            {
                throw new Exception("can't find SetTrackIcon in TrackResourceCache Type");
            }
            var finalMethod = setIconMethod.MakeGenericMethod(typeof(T));
            finalMethod.Invoke(null, new object[] { content });
        }

        public static void SetTimelineTrackTime(float time)
        {
            var timelineWindowType = typeof(TimelineEditor).Assembly.GetType("UnityEditor.Timeline.TimelineWindow");
            if (timelineWindowType == null)
            {
                throw new Exception("can't find TimelineWindow type");
            }

            var instance = LiteReflectionHelper.GetPropertyValue(timelineWindowType, "instance");
            var state = LiteReflectionHelper.GetPropertyValue(instance, "state");
            var editSequence = LiteReflectionHelper.GetPropertyValue(state, "editSequence");
            LiteReflectionHelper.SetPropertyValue(editSequence, "time", time);
        }

        public static double GetTimelineTrackTime()
        {
            var timelineWindowType = typeof(TimelineEditor).Assembly.GetType("UnityEditor.Timeline.TimelineWindow");
            if (timelineWindowType == null)
            {
                throw new Exception("can't find TimelineWindow type");
            }

            var instance = LiteReflectionHelper.GetPropertyValue(timelineWindowType, "instance");
            var state = LiteReflectionHelper.GetPropertyValue(instance, "state");
            var editSequence = LiteReflectionHelper.GetPropertyValue(state, "editSequence");
            return (double)LiteReflectionHelper.GetPropertyValue(editSequence, "time");
        }

        public static void SetTimelineFrameChange(Action<int> onFrame)
        {
            var timelineWindowType = typeof(TimelineEditor).Assembly.GetType("UnityEditor.Timeline.TimelineWindow");
            if (timelineWindowType == null)
            {
                throw new Exception("can't find TimelineWindow type");
            }

            var instance = LiteReflectionHelper.GetPropertyValue(timelineWindowType, "instance");
            var state = LiteReflectionHelper.GetPropertyValue(instance, "state");
            var editSequence = LiteReflectionHelper.GetPropertyValue(state, "editSequence");

            var winStateInfo = LiteReflectionHelper.GetFieldInfo(editSequence, "m_WindowState");
            if (winStateInfo != null)
            {
                var winState = winStateInfo.GetValue(editSequence);
                var action = winState.GetType().GetEvent("OnTimeChange");
                action.RemoveEventHandler(winState, (Action)OnTimeChange);
                action.AddEventHandler(winState, (Action)OnTimeChange);
            }

            void OnTimeChange()
            {
                var time = GetTimelineTrackTime();
                onFrame?.Invoke(TimeToFrame(time));
            }
        }
    }
}