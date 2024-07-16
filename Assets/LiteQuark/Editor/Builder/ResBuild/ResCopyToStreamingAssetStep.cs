namespace LiteQuark.Editor
{
    /// <summary>
    /// Copy asset bundle to streaming assets path
    /// </summary>
    internal sealed class ResCopyToStreamingAssetStep : IBuildStep
    {
        public string Name => "CopyTo StreamingAssets Step";

        public void Execute(ProjectBuilder builder)
        {
            ProjectBuilderUtils.CopyToStreamingAssets(builder.GetResOutputPath(), builder.ResConfig.CleanStreamingAssetsBeforeCopy);
        }
    }
}