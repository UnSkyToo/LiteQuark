using System;
using System.Collections.Generic;
using LiteCard.GamePlay;
using UnityEngine;

namespace LiteCard.Editor
{
    public sealed class BuffDataView : ClassifyDataView<BuffConfig>
    {
        private readonly Dictionary<CharacterJob, List<BuffConfig>> ClassifyList_ = new ();
        private readonly string[] JobList_ = typeof(CharacterJob).GetEnumNames();
        private int JobIndex_ = 0;

        public BuffDataView(string name, string jsonFile)
            : base(name, jsonFile)
        {
        }

        protected override float DrawClassifyToolbar()
        {
            JobIndex_ = GUILayout.Toolbar(JobIndex_, JobList_);

            return 20;
        }

        protected override BuffConfig[] GetSelectList()
        {
            var job = Enum.Parse<CharacterJob>(JobList_[JobIndex_]);

            if (ClassifyList_.TryGetValue(job, out var jobList))
            {
                return jobList.ToArray();
            }

            return Array.Empty<BuffConfig>();
        }
        
        protected override void RebuildClassifyList()
        {
            ClassifyList_.Clear();

            foreach (var data in GetData())
            {
                if (!ClassifyList_.TryGetValue(data.Job, out var jobList))
                {
                    jobList = new List<BuffConfig>();
                    ClassifyList_.Add(data.Job, jobList);
                }

                jobList.Add(data);
            }
        }
    }
}