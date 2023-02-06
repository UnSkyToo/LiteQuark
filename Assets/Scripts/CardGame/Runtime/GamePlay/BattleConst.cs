using System;

namespace LiteCard.GamePlay
{
    public static class BattleConst
    {
        public const int DrawCardCountPerRound = 5;
        public const int MaxCardCount = 10;
        public const int AttrLimit = 99999;
    }
    
    public enum RecordScopeType
    {
        Game,
        Battle,
        Round
    }
    
    public enum MatchTargetType
    {
        None,
        Player,
        Caster,
        Target,
        SelectTarget,
        Event,
        Random,
        AllMonster,
        All,
    }

    public enum MatchFilterType
    {
        None,
        Buff,
        CastCard,
    }

    public enum CompareMethod
    {
        Equal,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
    }
    
    public enum ModifierType
    {
        Damage,
        DamageThorns,
        DamageFixed,
        
        Recovery,
        
        Armour,
        ArmourFixed,
        
        Buff,
        
        CopyCard,
        MoveCard,
        DrawCard,
        AddCard,
        CastCard,
        ConsumeCard,
        UpgradeCard,
        
        CardCost,
        CardCostByKey,
        CardTag,
        
        Attr,
        ChangeEnergy,
        AgentTag,
    }

    public enum BuffSustainType
    {
        Round,
        OneRound,
        Forever,
    }

    public enum BuffTriggerType
    {
        None,
        
        BeforeAttack,
        AfterAttack,
        BeforeBeAttack,
        AfterBeAttack,
        
        BeforeChangeArmour,
        AfterChangeArmour,

        DeductArmour,
        DeductHp,

        RoundBegin,
        RoundEnd,
        
        KillMonster,

        BeforeAttrChange,
        AfterAttrChange,
        
        DrawCard,
        ConsumeCard,
        AfterCastCard,
    }

    public enum CharacterJob : byte
    {
        Normal = 10,
        Ironclad = 20,
        Silent = 30,
        Defect = 40,
        Watcher = 50,
    }
    
    public enum CardRarity : byte
    {
        Starter = 10,
        Common = 20,
        Uncommon = 30,
        Rare = 40,
    }

    [Flags]
    public enum CardType : int
    {
        None = 0,
        Attack = 1 << 1,
        Skill = 1 << 2,
        Power = 1 << 3,
        State = 1 << 4,
        Curse = 1 << 5,
    }
    
    public enum CardTag
    {
        // 普通
        Normal,
        // 打击
        Strike,
        // 消耗
        Consume,
        // 虚无
        Ethereal,
        // 不能打出
        Unplayable,
    }

    public enum CardDeckType
    {
        // 暂无归属
        None,
        // 牌库
        Pool,
        // 手牌
        Hand,
        // 弃牌
        Used,
        // 备用
        Back,
    }

    public enum CardUpgradeType
    {
        None,
        AddBuff,
    }

    public enum CardCastTargetType
    {
        None,
        SelectCardByUI,
        SelectCardByIndex,
        SelectCardByType,
        SelectCardByRandom,
        SelectCardByRandomJob,
    }

    public enum CardCastCheckType
    {
        None,
        // 禁止打出
        Forbid,
        // 满足手牌是特定类型
        HandCardType,
    }
    
    public enum AgentAttrType
    {
        CurHp,
        MaxHp,
        Strength,
        Dexterity,
        Armour,
    }

    public enum AgentTag
    {
        ForbidDrawCard,
    }
}