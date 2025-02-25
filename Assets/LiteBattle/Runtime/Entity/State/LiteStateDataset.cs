using System.Collections.Generic;
using System.IO;
using LiteQuark.Runtime;
using UnityEngine.Timeline;

namespace LiteBattle.Runtime
{
    public sealed class LiteStateDataset : Singleton<LiteStateDataset>
    {
        private readonly Dictionary<string, Dictionary<string, LiteStateData>> StateDataList_ = new ();

        private LiteStateDataset()
        {
        }
        
        public void Startup()
        {
            Load();
        }

        public void Shutdown()
        {
            Unload();
        }

        private void Load()
        {
            var stateDirectoryList = LiteAssetMgr.Instance.GetDirectoryList("StateData/Timeline/");
            foreach (var stateDirectory in stateDirectoryList)
            {
                var name = Path.GetFileName(stateDirectory);
                var group = LoadStateGroup(stateDirectory);
                StateDataList_.Add(name, group);
            }
        }

        private void Unload()
        {
            StateDataList_.Clear();
        }

        public void Reload()
        {
            Unload();
            Load();
        }

        private Dictionary<string, LiteStateData> LoadStateGroup(string stateGroupPath)
        {
            var group = new Dictionary<string, LiteStateData>();
            
            var fileList = LiteAssetMgr.Instance.GetFileList(stateGroupPath, ".playable");
            foreach (var file in fileList)
            {
                var timelineAsset = LiteAssetMgr.Instance.LoadAsset<TimelineAsset>(file);
                if (timelineAsset == null)
                {
                    LiteLog.Error($"can't load timeline asset : {file}");
                    continue;
                }

                var state = CreateStateDataWithAsset(timelineAsset);
                if (state != null)
                {
                    // LiteLog.Info($"load state : {timelineAsset.name}");
                    group.Add(timelineAsset.name, state);
                }
            }

            return group;
        }

        private LiteStateData CreateStateDataWithAsset(TimelineAsset timelineAsset)
        {
            if (timelineAsset == null)
            {
                return null;
            }

            var clipList = new List<LiteClip>();

            var tracks = timelineAsset.GetOutputTracks();
            foreach (var track in tracks)
            {
                if (track.muted)
                {
                    continue;
                }
                
                var clips = track.GetClips();
                foreach (var clip in clips)
                {
                    if (clip.asset is LiteTimelineStateClip stateClip)
                    {
                        var liteClip = new LiteClip(LiteClipKind.Duration, (float)clip.start, (float)clip.duration, stateClip.Event);
                        clipList.Add(liteClip);
                    }
                    else
                    {
                        LiteLog.Error($"unexpect clip type : {clip.GetType()}");
                    }
                }

                var markers = track.GetMarkers();
                foreach (var marker in markers)
                {
                    if (marker is LiteTimelineStateMarker stateMarker)
                    {
                        var liteClip = new LiteClip(LiteClipKind.Signal, (float)stateMarker.time, 0, stateMarker.Event);
                        clipList.Add(liteClip);
                    }
                    else
                    {
                        LiteLog.Error($"unexpect clip type : {marker.GetType()}");
                    }
                }
            }

            var state = new LiteStateData(timelineAsset.name, clipList.ToArray());
            return state;
        }

        public LiteStateData GetStateData(string stateGroupID, string stateID)
        {
            if (string.IsNullOrWhiteSpace(stateGroupID) || string.IsNullOrWhiteSpace(stateID))
            {
                return null;
            }
            
            if (StateDataList_.TryGetValue(stateGroupID, out var states))
            {
                if (states.TryGetValue(stateID, out var state))
                {
                    return state;
                }
            }

            LiteLog.Error($"can't get state, state group id = {stateGroupID}, state id = {stateID}");
            return null;
        }
    }
}