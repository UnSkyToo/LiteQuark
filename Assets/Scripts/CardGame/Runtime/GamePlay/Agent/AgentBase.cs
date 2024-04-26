using System;
using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public abstract class AgentBase : BaseObject, IRoundLifeCycle
    {
        public string Name { get; }

        public int CurHp => GetAttr(AgentAttrType.CurHp);
        public int MaxHp => GetAttr(AgentAttrType.MaxHp);
        public int Strength => GetAttr(AgentAttrType.Strength);
        public int Dexterity => GetAttr(AgentAttrType.Dexterity);
        public int Armour => GetAttr(AgentAttrType.Armour);

        private readonly Dictionary<AgentAttrType, int> AttrMap_;
        private readonly List<BuffData> BuffList_;

        private readonly RecordList<AgentTag> Tags_;

        protected AgentBase(string name, int curHp, int maxHp)
        {
            Name = name;

            AttrMap_ = new Dictionary<AgentAttrType, int>();
            SetAttr(AgentAttrType.CurHp, curHp);
            SetAttr(AgentAttrType.MaxHp, maxHp);
            SetAttr(AgentAttrType.Strength, 0);
            SetAttr(AgentAttrType.Dexterity, 0);
            SetAttr(AgentAttrType.Armour, 0);

            BuffList_ = new List<BuffData>();
            Tags_ = new RecordList<AgentTag>();
        }

        public bool IsAlive()
        {
            return CurHp > 0;
        }

        public void ResetRecord(RecordScopeType scopeType)
        {
            Tags_.Reset(scopeType);
        }

        public virtual void RoundBegin()
        {
        }
        
        public virtual void RoundEnd()
        {
            var buffList = new List<BuffData>(BuffList_.ToArray());
            foreach (var buff in buffList)
            {
                if (buff.Cfg.SustainType == BuffSustainType.OneRound)
                {
                    BuffSystem.Instance.RemoveBuff(this, buff.Cfg.ID);
                }
                else if (buff.Cfg.SustainType == BuffSustainType.Round)
                {
                    BuffSystem.Instance.BuffLayerChange(this, this, buff.Cfg.ID, -1);
                }
            }
        }

        public BuffData[] GetBuffList()
        {
            return BuffList_.ToArray();
        }

        public BuffData[] GetBuffList(BuffTriggerType triggerType)
        {
            var result = new List<BuffData>();
            
            foreach (var buff in BuffList_)
            {
                if (buff.Cfg.TriggerType == triggerType)
                {
                    result.Add(buff);
                }
            }

            return result.ToArray();
        }

        public BuffData GetBuffByID(int buffID)
        {
            return BuffList_.Find((buff) => buff.Cfg.ID == buffID);
        }

        public void AddTag(RecordScopeType scopeType, AgentTag tag)
        {
            Tags_.Add(scopeType, tag);
        }

        public bool ContainTag(AgentTag tag)
        {
            return Tags_.Contains(tag);
        }

        public void AddBuff(BuffData buff)
        {
            BuffList_.Add(buff);
        }

        public void RemoveBuff(BuffData buff)
        {
            BuffList_.Remove(buff);
        }

        public int GetAttr(AgentAttrType attrType)
        {
            if (AttrMap_.ContainsKey(attrType))
            {
                return AttrMap_[attrType];
            }
            
            return 0;
        }

        public void SetAttr(AgentAttrType attrType, int value)
        {
            if (!AttrMap_.ContainsKey(attrType))
            {
                AttrMap_.Add(attrType, value);
            }
            else
            {
                AttrMap_[attrType] = value;
            }
        }

        public void ChangeAttr(AgentAttrType attrType, int changeValue, float changePercent)
        {
            var oldValue = GetAttr(attrType);
            var value = GameUtils.CalculateFloorValue(GetAttr(attrType), 1.0f + changePercent) + changeValue;

            var limit = GetAttrLimit(attrType);
            value = Math.Clamp(value, 0, limit);

            BattleContext.Current[BattleContextKey.ChangeAttrType] = attrType;
            BattleContext.Current[BattleContextKey.ChangeAttrValue] = value;
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.BeforeAttrChange, this, attrType);

            AttrMap_[attrType] = (int)BattleContext.Current[BattleContextKey.ChangeAttrValue];
            
            BattleContext.Current[BattleContextKey.ChangeAttrType] = attrType;
            BattleContext.Current[BattleContextKey.ChangeAttrValue] = value;
            BuffSystem.Instance.TriggerBuff(BuffTriggerType.AfterAttrChange, this, attrType);
            
            LiteRuntime.Get<EventSystem>().Send(new AgentAttrChangeEvent(this, attrType, value - oldValue));
        }

        public void ChangeAttr(ChangeAttrConfig[] changeAttrs, int count, bool inverse)
        {
            for (var index = 0; index < count; ++index)
            {
                foreach (var attr in changeAttrs)
                {
                    ChangeAttr(attr.AttrType, inverse ? -attr.Value : attr.Value, inverse ? -attr.Percent : attr.Percent);
                }
            }
        }

        private int GetAttrLimit(AgentAttrType attrType)
        {
            switch (attrType)
            {
                case AgentAttrType.CurHp:
                    return MaxHp;
            }

            return BattleConst.AttrLimit;
        }
    }
}