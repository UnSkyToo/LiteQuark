namespace LiteQuark.Runtime
{
    public class MotionDestroy : MotionBase
    {
        public override void Enter()
        {
            if (Master != null && Master.gameObject != null)
            {
                LiteRuntime.Get<AssetSystem>().UnloadAsset(Master.gameObject);
            }
        }
    }
}