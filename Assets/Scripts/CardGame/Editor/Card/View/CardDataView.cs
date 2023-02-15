using System;
using System.Collections.Generic;
using LiteCard.GamePlay;
using LiteQuark.Editor;
using UnityEditor;
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

        protected override void OnDrawDataItem(Rect rect, CardConfig data, bool isSelected)
        {
            void DrawTextureEx(string resPath, float x, float y, float width, float height)
            {
                DrawTexture(resPath, rect.x + ((ItemWidth - width) * 0.5f - x) * ItemScale, rect.y + ((ItemHeight - height) * 0.5f - y) * ItemScale, width, height);
            }

            void DrawStringEx(string title, float x, float y)
            {
                var width = EditorStyles.label.fontSize * title.Length;
                var height = 20;
                DrawString(title, rect.x + ((ItemWidth - width) * 0.5f - x) * ItemScale, rect.y + ((ItemHeight - height) * 0.5f - y) * ItemScale, width, height);
            }
            
            DrawTexture(GameConst.Card.BackgroundResPathList[data.Job][data.Type], rect.x, rect.y, ItemWidth, ItemHeight);
            DrawTextureEx(data.IconRes, 0, 70, 248, 186);
            DrawTextureEx(GameConst.Card.TypeResPathList[data.Type][data.Rarity], 0, 50, 262, 185);
            DrawTextureEx(GameConst.Card.NameResPathList[data.Rarity], 0, 160, 324, 77);
            DrawStringEx(data.Name, 0, 177);
        }

        private void DrawTexture(string resPath, float x, float y, float width, float height)
        {
            var texture = GetTexture(resPath);
            if (texture != null)
            {
                var rect = new Rect(x, y, width * ItemScale, height * ItemScale);
                GUI.DrawTexture(rect, texture);
            }
        }

        private void DrawString(string title, float x, float y, float width, float height)
        {
            using (new ColorScope(Color.red))
            {
                var rect = new Rect(x, y, width * ItemScale, height * ItemScale);
                EditorGUI.LabelField(rect, title);
            }
        }

        private readonly Dictionary<string, Texture> TextureCache_ = new Dictionary<string, Texture>();
        private Texture GetTexture(string resPath)
        {
            if (TextureCache_.TryGetValue(resPath, out var texture))
            {
                return texture;
            }

            texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/StandaloneAssets/{resPath}");
            TextureCache_.Add(resPath, texture);
            return texture;
        }

        private const float ItemScale = 0.5f;
        private const float ItemWidth = 302f;
        private const float ItemHeight = 419f;

        protected override float GetItemWidth()
        {
            return ItemWidth * ItemScale;
        }

        protected override float GetItemHeight()
        {
            return ItemHeight * ItemScale;
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