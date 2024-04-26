namespace LiteQuark.Runtime
{
    public class MotionDestroy : MotionBase
    {
        public override void Enter()
        {
            if (Master != null && Master.gameObject != null)
            {
                LiteRuntime.Asset.UnloadAsset(Master.gameObject);
            }
        }
    }
}