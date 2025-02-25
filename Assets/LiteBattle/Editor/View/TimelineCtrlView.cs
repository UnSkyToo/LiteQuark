using LiteBattle.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Editor
{
    public sealed class LiteTimelineCtrlView : LiteViewBase
    {
        private string CurrentTimelineName_ = string.Empty;
        private int MaxFrame_ = 0;
        private int FrameIndex_ = 0;
        
        public LiteTimelineCtrlView(LiteStateEditor stateEditor)
            : base(stateEditor)
        {
            ResetContext();
        }

        public override void Dispose()
        {
            ResetContext();
        }

        public override bool IsVisible()
        {
            return LiteEditorBinder.Instance.GetTimeline() != null;
        }

        public override void Draw()
        {
            DrawTitle($"Timeline Ctrl View : {LiteEditorBinder.Instance.GetCurrentStateGroup()}");

            var timelineAsset = LiteEditorBinder.Instance.GetTimeline();
            if (timelineAsset == null)
            {
                if (!string.IsNullOrWhiteSpace(CurrentTimelineName_))
                {
                    ResetContext();
                }
            }
            else if (timelineAsset.name != CurrentTimelineName_)
            {
                OnTimelineChanged(timelineAsset);
            }
            
            using (new EditorGUI.DisabledGroupScope(timelineAsset == null))
            {
                DrawTimelineController(timelineAsset);
            }

            using (new EditorGUI.DisabledGroupScope(!EditorApplication.isPlaying))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawDataToolbar();
                }
            }
        }

        private void ResetContext()
        {
            LiteEditorPreviewer.Instance.SetTimeline(null);
            CurrentTimelineName_ = string.Empty;
            MaxFrame_ = 0;
            FrameIndex_ = 0;
        }
        
        private void OnTimelineChanged(TimelineAsset timelineAsset)
        {
            CurrentTimelineName_ = timelineAsset.name;
            MaxFrame_ = LiteTimelineHelper.TimeToFrame(timelineAsset.duration);
            LiteEditorPreviewer.Instance.SetTimeline(timelineAsset);
            SetFrameIndex(0);
        }

        public void SetFrameIndex(int frameIndex)
        {
            FrameIndex_ = frameIndex;
            LiteEditorPreviewer.Instance.ChangeFrame(FrameIndex_);
            
            var time = LiteTimelineHelper.FrameToTime(FrameIndex_);
            LiteTimelineHelper.SetTimelineTrackTime((float) time);
            LiteTimelineHelper.RepaintTimeline();
        }

        private void DrawTimelineController(TimelineAsset timelineAsset)
        {
            EditorGUI.BeginChangeCheck();
            var newFrame = EditorGUILayout.IntSlider($"Frame {FrameIndex_}/{MaxFrame_}", FrameIndex_, 0, MaxFrame_);
            if (EditorGUI.EndChangeCheck())
            {
                SetFrameIndex(newFrame);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTransportToolbar();
                
                // if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                // {
                //     LiteTimelineHelper.ClearTimeline();
                // }
            }
        }
        
        private void DrawTransportToolbar()
        {
            GotoBeginingSequenceGUI();
            PreviousEventButtonGUI();
            PlayButtonGUI();
            NextEventButtonGUI();
            GotoEndSequenceGUI();
        }
        
        private void GotoBeginingSequenceGUI()
        {
            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button(LiteEditorStyle.GotoBeginingContent, EditorStyles.toolbarButton))
                {
                    SetFrameIndex(0);
                }
            }
        }
        
        private void PreviousEventButtonGUI()
        {
            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button(LiteEditorStyle.PreviousFrameContent, EditorStyles.toolbarButton))
                {
                    if (FrameIndex_ > 0)
                    {
                        SetFrameIndex(FrameIndex_ - 1);
                    }
                }
            }
        }
        
        private void PlayButtonGUI()
        {
            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                var isPlaying = GUILayout.Toggle(EditorApplication.isPlaying, LiteEditorStyle.PlayContent, EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorApplication.EnterPlaymode();
                }
            }
        }

        private void NextEventButtonGUI()
        {
            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying && !EditorApplication.isPaused))
            {
                if (GUILayout.Button(LiteEditorStyle.NextFrameContent, EditorStyles.toolbarButton))
                {
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.Step();
                    }
                    else
                    {
                        if (FrameIndex_ < MaxFrame_)
                        {
                            SetFrameIndex(FrameIndex_ + 1);
                        }
                    }
                }
            }
        }
        
        private void GotoEndSequenceGUI()
        {
            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button(LiteEditorStyle.GotoEndContent, EditorStyles.toolbarButton))
                {
                    SetFrameIndex(MaxFrame_);
                }
            }
        }

        private void DrawDataToolbar()
        {
            if (GUILayout.Button("Reload"))
            {
                LiteTimelineHelper.SaveTimelineChanges();
                LiteStateDataset.Instance.Reload();
            }
        }
    }
}