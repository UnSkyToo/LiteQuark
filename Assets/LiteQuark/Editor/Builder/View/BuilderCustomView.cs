namespace LiteQuark.Editor
{
    internal sealed class BuilderCustomView : BuilderStepView
    {
        public override bool Enabled
        {
            get => _config.Enable;
            protected set => _config.Enable = value;
        }

        private readonly CustomBuildConfig _config;
        private readonly ICustomBuildView _view;
        
        public BuilderCustomView(ProjectBuilderWindow window, string title, CustomBuildConfig config, ICustomBuildView view)
            : base(window, title)
        {
            _config = config;
            _view = view;
        }

        protected override void DrawContent()
        {
            _view?.DrawContent(_config);
        }
    }
}