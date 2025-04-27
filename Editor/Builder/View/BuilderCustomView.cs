namespace LiteQuark.Editor
{
    internal sealed class BuilderCustomView : BuilderStepView
    {
        public override bool Enabled
        {
            get => Config_.Enable;
            protected set => Config_.Enable = value;
        }

        private readonly CustomBuildConfig Config_;
        private readonly ICustomBuildView View_;
        
        public BuilderCustomView(ProjectBuilderWindow window, string title, CustomBuildConfig config, ICustomBuildView view)
            : base(window, title)
        {
            Config_ = config;
            View_ = view;
        }

        protected override void DrawContent()
        {
            View_?.DrawContent(Config_);
        }
    }
}