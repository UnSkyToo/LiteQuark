using System.Collections.Generic;
using System.Threading.Tasks;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public sealed class LiteEntityManager : Singleton<LiteEntityManager>, IManager
    {
        private readonly List<LiteEntity> EntityList_ = new List<LiteEntity>();
        private readonly List<LiteEntity> AddList_ = new List<LiteEntity>();
        private readonly List<LiteEntity> RemoveList_ = new List<LiteEntity>();

        private LiteEntityManager()
        {
        }

        public Task<bool> Startup()
        {
            EntityList_.Clear();
            AddList_.Clear();
            RemoveList_.Clear();
            
            return Task.FromResult(true);
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

        public void AddUnit(string unitID, System.Action<LiteUnit> callback)
        {
            var config = LiteNexusDataManager.Instance.GetUnitConfig(unitID);
            if (config == null)
            {
                callback?.Invoke(null);
                return;
            }

            var unit = new LiteUnit(config);
            AddList_.Add(unit);
            unit.Initialize(() =>
            {
                callback?.Invoke(unit);
            });
        }
    }
}