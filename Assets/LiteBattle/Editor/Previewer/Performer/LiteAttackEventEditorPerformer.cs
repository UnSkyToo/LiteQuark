using LiteBattle.Runtime;

namespace LiteBattle.Editor
{
    [LiteEventEditorPerformer(typeof(LiteAttackEvent))]
    public class LiteAttackEventEditorPerformer : ILiteEventEditorPerformer
    {
        public void OnExecute(ILiteEvent evt)
        {
            if (evt is LiteAttackEvent attackEvent)
            {
                LiteEditorBinder.Instance.BindAttackRange(attackEvent.Range);
            }
        }

        public void OnCancel()
        {
            LiteEditorBinder.Instance.UnBindAttackRange();
        }

        public void OnFrame(int frame)
        {
        }
    }
}