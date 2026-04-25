using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.Timeline;

namespace LiteBattle.Runtime
{
    public sealed class LiteNexusDataManager : Singleton<LiteNexusDataManager>, IManager
    {
        private readonly Dictionary<string, LiteUnitConfig> UnitConfigMap_ = new();
        private readonly Dictionary<string, AssetHandle<LiteUnitConfig>> UnitConfigHandleMap_ = new();
        private readonly Dictionary<string, Dictionary<string, LiteStateConfig>> StateConfigMap_ = new();

        private LiteNexusDataManager()
        {
        }
        
        public UniTask<bool> Startup()
        {
            return Load();
        }

        public void Shutdown()
        {
            Unload();
        }

        private async UniTask<bool> Load()
        {
            var jsonPath = PathUtils.GetRelativeAssetRootPath(LiteNexusConfig.Instance.GetDatabaseJsonPath());
            var jsonHandle = LiteRuntime.Asset.LoadAssetHandle<TextAsset>(jsonPath);
            LiteNexusDatabase database;
            try
            {
                var text = await jsonHandle.Task;
                var json = text.text;
                database = LitJson.JsonMapper.ToObject<LiteNexusDatabase>(json);
            }
            finally
            {
                jsonHandle.Dispose();
            }

            foreach (var unitConfigPath in database.UnitList)
            {
                var unitConfigHandle = LiteRuntime.Asset.LoadAssetHandle<LiteUnitConfig>(unitConfigPath);
                var unitConfig = await unitConfigHandle.Task;
                if (unitConfig == null)
                {
                    unitConfigHandle.Dispose();
                    continue;
                }

                UnitConfigMap_.Add(unitConfig.StateGroup, unitConfig);
                UnitConfigHandleMap_.Add(unitConfig.StateGroup, unitConfigHandle);
                
                var group = await LoadStateGroup(database.StateMap[unitConfig.StateGroup]);
                StateConfigMap_.Add(unitConfig.StateGroup, group);
            }
            return true;
        }

        private void Unload()
        {
            foreach (var chunk in UnitConfigHandleMap_)
            {
                chunk.Value.Dispose();
            }
            UnitConfigHandleMap_.Clear();
            UnitConfigMap_.Clear();
            StateConfigMap_.Clear();
        }

        public async void Reload()
        {
            Unload();
            await Load();
        }

        private async UniTask<Dictionary<string, LiteStateConfig>> LoadStateGroup(List<string> stateList)
        {
            var group = new Dictionary<string, LiteStateConfig>();
            
            foreach (var filePath in stateList)
            {
                var timelineHandle = LiteRuntime.Asset.LoadAssetHandle<TimelineAsset>(filePath);
                try
                {
                    var timelineAsset = await timelineHandle.Task;
                    if (timelineAsset == null)
                    {
                        LLog.Error($"can't load timeline asset : {filePath}");
                        continue;
                    }

                    var state = CreateStateConfigWithAsset(timelineAsset);
                    if (state != null)
                    {
                        // LiteLog.Info($"load state : {timelineAsset.name}");
                        group.Add(timelineAsset.name, state);
                    }
                }
                finally
                {
                    timelineHandle.Dispose();
                }
            }

            return group;
        }

        private LiteStateConfig CreateStateConfigWithAsset(TimelineAsset timelineAsset)
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

            var state = new LiteStateConfig(timelineAsset.name, clipList.ToArray());
            return state;
        }

        public LiteUnitConfig GetUnitConfig(string unitID)
        {
            if (string.IsNullOrWhiteSpace(unitID))
            {
                return null;
            }

            if (UnitConfigMap_.TryGetValue(unitID, out var unitConfig))
            {
                return unitConfig;
            }

            LLog.Error($"can't get unit config, unit id = {unitID}");
            return null;
        }

        public LiteStateConfig GetStateConfig(string stateGroupID, string stateID)
        {
            if (string.IsNullOrWhiteSpace(stateGroupID) || string.IsNullOrWhiteSpace(stateID))
            {
                return null;
            }
            
            if (StateConfigMap_.TryGetValue(stateGroupID, out var states))
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
