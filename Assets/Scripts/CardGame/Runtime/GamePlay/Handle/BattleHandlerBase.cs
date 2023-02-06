using LiteQuark.Runtime;

namespace LiteCard.GamePlay
{
    public abstract class BattleHandlerBase<T> : Singleton<T> where T :  class
    {
        protected BattleHandlerBase()
        {
        }

        public abstract EditorObjectArrayResult GetObjectArrayResult(object binder);

        protected bool CheckParam(object binder, object[] paramList)
        {
            var result = GetObjectArrayResult(binder);
            if (!GameUtils.CheckParam(result.Value, paramList))
            {
                Log.Error($"{GetType().Name} {binder} params error");
                return false;
            }

            return true;
        }
    }
}