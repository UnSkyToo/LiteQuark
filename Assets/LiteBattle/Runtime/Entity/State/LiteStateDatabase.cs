using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Runtime
{
    public sealed class LiteStateDatabase : Singleton<LiteStateDatabase>
    {
        private readonly Dictionary<string, LiteGroupData> GroupDataList_ = new();
        private readonly Dictionary<string, Dictionary<string, LiteStateData>> StateDataList_ = new();

        private LiteStateDatabase()
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
            var jsonPath = PathUtils.GetRelativeAssetRootPath(LiteStateConfig.Instance.GetDatabaseJsonPath());
            var json = LiteRuntime.Asset.LoadAssetSync<TextAsset>(jsonPath).text;
            var groupDataList = LitJson.JsonMapper.ToObject<List<LiteGroupData>>(json);
            foreach (var groupData in groupDataList)
            {
                GroupDataList_.Add(groupData.Name, groupData);
                
                var group = LoadStateGroup(groupData);
                StateDataList_.Add(groupData.Name, group);
            }
        }

        private void Unload()
        {
            GroupDataList_.Clear();
            StateDataList_.Clear();
        }

        public void Reload()
        {
            Unload();
            Load();
        }

        private Dictionary<string, LiteStateData> LoadStateGroup(LiteGroupData groupData)
        {
            var group = new Dictionary<string, LiteStateData>();
            
            foreach (var file in groupData.TimelineList)
            {
                var timelineAsset = LiteRuntime.Asset.LoadAssetSync<TimelineAsset>(file);
                if (timelineAsset == null)
                {
                    LLog.Error($"can't load timeline asset : {file}");
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
                        LLog.Error($"unexpected clip type : {clip.GetType()}");
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
                        LLog.Error($"unexpected marker type : {marker.GetType()}");
                    }
                }
            }

            var state = new LiteStateData(timelineAsset.name, clipList.ToArray());
            return state;
        }

        public LiteGroupData GetGroupData(string stateGroupID)
        {
            if (string.IsNullOrWhiteSpace(stateGroupID))
            {
                return default;
            }

            if (GroupDataList_.TryGetValue(stateGroupID, out var groupData))
            {
                return groupData;
            }

            LLog.Error($"can't get state group, state group id = {stateGroupID}");
            return default;
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

            LLog.Error($"can't get state, state group id = {stateGroupID}, state id = {stateID}");
            return null;
        }
    }
}