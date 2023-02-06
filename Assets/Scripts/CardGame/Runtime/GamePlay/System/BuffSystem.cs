using System;
using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public sealed class BuffSystem : Singleton<BuffSystem>
    {
        public BuffSystem()
        {
        }

        public void TriggerBuff(BuffTriggerType triggerType, AgentBase caster, params object[] arguments)
        {
            var buffList = caster.GetBuffList(triggerType);
            foreach (var buff in buffList)
            {
                BuffTriggerHandler.Instance.Execute(caster, buff, arguments);
            }
        }
        
        public void DoBuff(AgentBase caster, BuffData buff)
        {
            var targets = AgentMatcher.Match(caster, new List<AgentBase> { caster }, buff.Cfg.MatchID);
            
            ModifierSystem.Instance.Append(caster, targets, buff);
        }

        private BuffData BuffCreate(AgentBase caster, AgentBase target, int buffID, int layer)
        {
            var buff = target.GetBuffByID(buffID);
            if (buff != null)
            {
                return buff;
            }

            buff = new BuffData(buffID, caster);
            buff.Layer = Math.Min(layer, buff.Cfg.MaxLayer);
            
            target.AddBuff(buff);
            
            target.ChangeAttr(buff.Cfg.BaseAttr, 1, false);
            target.ChangeAttr(buff.Cfg.LayerAttr, buff.Layer, false);

            return buff;
        }

        private void BuffDelete(AgentBase caster, AgentBase target, int buffID)
        {
            var buff = target.GetBuffByID(buffID);
            if (buff == null)
            {
                return;
            }
            
            target.ChangeAttr(buff.Cfg.LayerAttr, buff.Layer, true);
            target.ChangeAttr(buff.Cfg.BaseAttr, 1, true);

            target.RemoveBuff(buff);
        }

        private void BuffLayerAdd(AgentBase caster, AgentBase target, int buffID, int layer)
        {
            if (layer <= 0)
            {
                return;
            }
            
            var buff = target.GetBuffByID(buffID);
            if (buff == null)
            {
                BuffCreate(caster, target, buffID, layer);
            }
            else
            {
                var addLayer = Math.Min(buff.Cfg.MaxLayer - buff.Layer, layer);
                if (addLayer > 0)
                {
                    buff.Layer += addLayer;
                    target.ChangeAttr(buff.Cfg.LayerAttr, addLayer, false);
                }
            }
        }

        private void BuffLayerSub(AgentBase caster, AgentBase target, int buffID, int layer)
        {
            if (layer <= 0)
            {
                return;
            }
            
            var buff = target.GetBuffByID(buffID);
            if (buff == null)
            {
               return;
            }

            var subLayer = Math.Min(buff.Layer, layer);
            if (subLayer > 0)
            {
                buff.Layer -= subLayer;
                target.ChangeAttr(buff.Cfg.LayerAttr, subLayer, true);

                if (buff.Layer <= 0)
                {
                    BuffDelete(caster, target, buffID);
                }
            }
        }

        public void BuffLayerChange(AgentBase caster, AgentBase target, int buffID, int layer)
        {
            if (layer > 0)
            {
                BuffLayerAdd(caster, target, buffID, layer);
            }
            else
            {
                BuffLayerSub(caster, target, buffID, -layer);
            }
            
            LiteRuntime.Get<EventSystem>().Send(new BuffLayerChangeEvent(target, buffID));
        }

        public void AddBuff(AgentBase caster, int buffID, int layer)
        {
            BuffLayerChange(caster, caster, buffID, 1);
        }

        public void RemoveBuff(AgentBase caster, int buffID)
        {
            BuffDelete(caster, caster, buffID);
        }
    }
}