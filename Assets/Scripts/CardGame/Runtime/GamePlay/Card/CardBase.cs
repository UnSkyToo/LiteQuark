using System;

namespace LiteCard.GamePlay
{
    public abstract class CardBase
    {
        public CardDeckType BelongDeckType { get; set; }
        
        protected readonly CardData Data_;

        protected CardBase(CardData data)
        {
            Data_ = data;
        }

        public string GetName()
        {
            var upgradeLevel = GetUpgradeLevel();
            
            if (upgradeLevel == 0)
            {
                return Data_.Cfg.Name;
            }

            if (upgradeLevel == 1)
            {
                return $"{Data_.Cfg.Name}+";
            }

            return $"{Data_.Cfg.Name}+{upgradeLevel}";
        }

        public CardConfig GetCfg()
        {
            return Data_.Cfg;
        }

        public CardCastData GetCastData()
        {
            return GetUpgradeLevel() > 0 ? Data_.CastUpgrade : Data_.CastNormal;
        }

        public CardData GetData()
        {
            return Data_;
        }

        public int GetUpgradeLevel()
        {
            return Data_.UpgradeLevel.Value;
        }

        public int GetDecCost()
        {
            return Data_.DecCost.Value;
        }
        
        public int GetCost()
        {
            if (GetCastData().Cfg.Cost < 0)
            {
                return AgentSystem.Instance.GetPlayer().CurEnergy;
            }
            
            return Math.Clamp(GetCastData().Cfg.Cost - GetDecCost(), 0, GetCastData().Cfg.Cost);
        }

        public void ChangeDecCost(RecordScopeType scopeType, int changeValue)
        {
            Data_.DecCost.ChangeValue(scopeType, changeValue);
        }
        
        public void AddTag(RecordScopeType scopeType, CardTag tag)
        {
            Data_.Tags.Add(scopeType, tag);
        }
        
        public bool ContainTag(CardTag tag)
        {
            return Data_.Tags.Contains(tag);
        }

        public bool ContainTagAny(CardTag[] tags)
        {
            foreach (var tag in tags)
            {
                if (ContainTag(tag))
                {
                    return true;
                }
            }

            return false;
        }
        
        public void Upgrade(RecordScopeType scopeType)
        {
            Data_.UpgradeLevel.ChangeValue(scopeType, 1);
        }

        public bool Cast(Player caster, AgentBase target)
        {
            if (!OnCast(caster, target))
            {
                return false;
            }

            Data_.CastCount++;
            return true;
        }

        protected virtual bool OnCast(Player caster, AgentBase target)
        {
            return false;
        }

        public abstract CardBase Clone();
    }
}