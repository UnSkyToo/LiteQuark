using System;
using System.Collections.Generic;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public class LiteEditorPreviewer : Singleton<LiteEditorPreviewer>
    {
        private TimelineAsset Timeline_;
        private readonly Dictionary<int, ILiteEventEditorPerformer> Performers_ = new Dictionary<int, ILiteEventEditorPerformer>();
        private int FrameIndex_;
        
        private LiteEditorPreviewer()
        {
        }

        public void Startup()
        {
            Timeline_ = null;
            Performers_.Clear();
            FrameIndex_ = -1;

            LiteTimelineStateBehavior.OnTimeChange += OnTimeChange;
        }

        public void Shutdown()
        {
            LiteTimelineStateBehavior.OnTimeChange -= OnTimeChange;
            
            CancelAllPerformer();
        }

        private void OnTimeChange(double time)
        {
            var frameIndex = LiteTimelineHelper.TimeToFrame(time);
            ChangeFrame(frameIndex);
        }

        private void CancelAllPerformer()
        {
            foreach (var performer in Performers_)
            {
                performer.Value.OnCancel();
            }
            Performers_.Clear();
        }

        public void SetTimeline(TimelineAsset timelineAsset)
        {
            CancelAllPerformer();
            Timeline_ = timelineAsset;
        }

        public void ChangeFrame(int frameIndex)
        {
            if (Timeline_ == null)
            {
                return;
            }

            if (FrameIndex_ == frameIndex)
            {
                return;
            }

            FrameIndex_ = frameIndex;
            Process(frameIndex);

            foreach (var performer in Performers_)
            {
                performer.Value.OnFrame(frameIndex);
            }
        }

        private void Process(int frameIndex)
        {
            var tracks = Timeline_.GetOutputTracks();
            foreach (var track in tracks)
            {
                var clips = track.GetClips();
                foreach (var clip in clips)
                {
                    if (clip.asset is LiteTimelineStateClip stateClip)
                    {
                        var startFrame = LiteTimelineHelper.TimeToFrame(clip.start);
                        var endFrame = LiteTimelineHelper.TimeToFrame(clip.start + clip.duration);

                        if (startFrame <= frameIndex && frameIndex <= endFrame)
                        {
                            Execute(stateClip.GetInstanceID(), stateClip.Event);
                        }
                        else
                        {
                            Cancel(stateClip.GetInstanceID(), stateClip.Event);
                        }
                    }
                }

                var markers = track.GetMarkers();
                foreach (var marker in markers)
                {
                    if (marker is LiteTimelineStateMarker stateMarker)
                    {
                        var triggerFrame = LiteTimelineHelper.TimeToFrame(marker.time);
                        if (triggerFrame == frameIndex)
                        {
                            Execute(stateMarker.GetInstanceID(), stateMarker.Event);
                        }
                        else
                        {
                            Cancel(stateMarker.GetInstanceID(), stateMarker.Event);
                        }
                    }
                }
            }
        }

        private void Execute(int instanceID, ILiteEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (Performers_.ContainsKey(instanceID))
            {
                return;
            }

            var performerType = GetPerformerType(evt);
            if (performerType == null)
            {
                return;
            }

            if (Activator.CreateInstance(performerType) is ILiteEventEditorPerformer performer)
            {
                Performers_.Add(instanceID, performer);
                Performers_[instanceID].OnExecute(evt);
            }
        }

        private void Cancel(int instanceID, ILiteEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (!Performers_.ContainsKey(instanceID))
            {
                return;
            }
            
            Performers_[instanceID].OnCancel();
            Performers_.Remove(instanceID);
        }

        private Type GetPerformerType(ILiteEvent evt)
        {
            var performerTypes = LiteEditorUtils.GetTypeListWithBaseType(typeof(ILiteEventEditorPerformer));
            
            foreach (var performerType in performerTypes)
            {
                var performerAttr = TypeUtils.GetAttribute<LiteEventEditorPerformerAttribute>(performerType, null);
                if (performerAttr != null && performerAttr.BindType == evt.GetType())
                {
                    return performerType;
                }
            }

            return null;
        }
    }
}