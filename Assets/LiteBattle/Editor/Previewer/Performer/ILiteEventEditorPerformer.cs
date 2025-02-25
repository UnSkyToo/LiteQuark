using LiteBattle.Runtime;

namespace LiteBattle.Editor
{
    public interface ILiteEventEditorPerformer
    {
        public void OnExecute(ILiteEvent evt);
        public void OnCancel();
        public void OnFrame(int frame);
    }
}