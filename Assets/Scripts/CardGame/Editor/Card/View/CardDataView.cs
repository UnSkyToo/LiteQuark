using System;
using System.Collections.Generic;
using LiteCard.GamePlay;
using UnityEngine;

namespace LiteCard.Editor
{
    public sealed class CardDataView : ClassifyDataView<CardConfig>
    {
        private readonly Dictionary<CharacterJob, Dictionary<CardRarity, Dictionary<CardType, List<CardConfig>>>> ClassifyList_ = new ();
        private readonly string[] JobList_ = typeof(CharacterJob).GetEnumNames();
        private int JobIndex_ = 0;
        private readonly string[] RarityList_ = typeof(CardRarity).GetEnumNames();
        private int RarityIndex_ = 0;
        private readonly string[] TypeList_ = new string[] { CardType.Attack.ToString(), CardType.Skill.ToString(), CardType.Power.ToString(), CardType.State.ToString(), CardType.Curse.ToString() };
        private int TypeIndex_ = 0;

        public CardDataView(string name, string jsonFile)
            : base(name, jsonFile)
        {
        }

        protected override float DrawClassifyToolbar()
        {
            JobIndex_ = GUILayout.Toolbar(JobIndex_, JobList_);
            RarityIndex_ = GUILayout.Toolbar(RarityIndex_, RarityList_);
            TypeIndex_ = GUILayout.Toolbar(TypeIndex_, TypeList_);

            return 60;
        }

        protected override CardConfig[] GetSelectList()
        {
            var job = Enum.Parse<CharacterJob>(JobList_[JobIndex_]);
            var rarity = Enum.Parse<CardRarity>(RarityList_[RarityIndex_]);
            var type = Enum.Parse<CardType>(TypeList_[TypeIndex_]);

            if (ClassifyList_.TryGetValue(job, out var jobList))
            {
                if (jobList.TryGetValue(rarity, out var rarityList))
                {
                    if (rarityList.TryGetValue(type, out var typeList))
                    {
                        return typeList.ToArray();
                    }
                }
            }

            return Array.Empty<CardConfig>();
        }

        protected override void RebuildClassifyList()
        {
            ClassifyList_.Clear();

            foreach (var data in GetData())
            {
                if (!ClassifyList_.TryGetValue(data.Job, out var jobList))
                {
                    jobList = new Dictionary<CardRarity, Dictionary<CardType, List<CardConfig>>>();
                    ClassifyList_.Add(data.Job, jobList);
                }

                if (!jobList.TryGetValue(data.Rarity, out var rarityList))
                {
                    rarityList = new Dictionary<CardType, List<CardConfig>>();
                    jobList.Add(data.Rarity, rarityList);
                }

                if (!rarityList.TryGetValue(data.Type, out var typeList))
                {
                    typeList = new List<CardConfig>();
                    rarityList.Add(data.Type, typeList);
                }
                
                typeList.Add(data);
            }
        }
    }
}