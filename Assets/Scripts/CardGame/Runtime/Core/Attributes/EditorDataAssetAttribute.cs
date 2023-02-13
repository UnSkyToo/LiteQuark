using System;

namespace LiteCard
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EditorDataAssetAttribute : Attribute
    {
        public Type AssetType { get; }
        
        public EditorDataAssetAttribute(Type assetType)
        {
            AssetType = assetType;
        }
    }
}