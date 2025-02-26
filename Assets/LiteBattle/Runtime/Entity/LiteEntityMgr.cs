using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityMgr : Singleton<LiteEntityMgr>
    {
        private readonly List<LiteEntity> EntityList_ = new List<LiteEntity>();
        private readonly List<LiteEntity> AddList_ = new List<LiteEntity>();
        private readonly List<LiteEntity> RemoveList_ = new List<LiteEntity>();

        private LiteEntityMgr()
        {
        }

        public void Startup()
        {
            LiteStateDatabase.Instance.Startup();
            
            EntityList_.Clear();
            AddList_.Clear();
            RemoveList_.Clear();
        }

        public void Shutdown()
        {
            foreach (var entity in AddList_)
            {
                entity.Dispose();
            }
            AddList_.Clear();

            foreach (var entity in EntityList_)
            {
                entity.Dispose();
            }
            EntityList_.Clear();
            
            LiteStateDatabase.Instance.Shutdown();
        }

        public void Tick(float deltaTime)
        {
            if (AddList_.Count > 0)
            {
                foreach (var entity in AddList_)
                {
                    EntityList_.Add(entity);
                }
                AddList_.Clear();
            }

            foreach (var entity in EntityList_)
            {
                entity.Tick(deltaTime);

                if (!entity.IsAlive)
                {
                    RemoveList_.Add(entity);
                }
            }

            if (RemoveList_.Count > 0)
            {
                foreach (var entity in RemoveList_)
                {
                    EntityList_.Remove(entity);
                    entity.Dispose();
                }
                RemoveList_.Clear();
            }
        }

        public void PostTick(float deltaTime)
        {
            foreach (var entity in EntityList_)
            {
                entity.PostTick(deltaTime);
            }
        }

        public List<LiteEntity> GetEntityList()
        {
            return EntityList_;
        }

        public LiteEntity GetEntity(ulong uniqueID)
        {
            foreach (var entity in EntityList_)
            {
                if (entity.UniqueID == uniqueID)
                {
                    return entity;
                }
            }

            return null;
        }

        public LiteAgent AddAgent(string stateGroupID)
        {
            var data = LiteStateDatabase.Instance.GetGroupData(stateGroupID);
            var asset = LiteRuntime.Asset.LoadAssetSync<LiteAgentConfig>(data.AgentPath);
            if (asset == null)
            {
                LLog.Error($"can't load {nameof(LiteAgentConfig)} : {stateGroupID}");
                return null;
            }

            var agent = new LiteAgent(asset);
            AddList_.Add(agent);
            return agent;
        }
    }
}