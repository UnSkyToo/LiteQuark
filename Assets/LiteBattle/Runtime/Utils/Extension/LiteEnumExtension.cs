namespace LiteBattle.Runtime
{
    public static class LiteEnumExtension
    {
        public static string ToContextKey(this LiteInputKeyType type)
        {
            return LiteConst.ContextKey.FromInputKey(type);
        }

        public static LiteEntityCamp Reverse(this LiteEntityCamp camp)
        {
            switch (camp)
            {
                case LiteEntityCamp.Dark:
                    return LiteEntityCamp.Light;
                case LiteEntityCamp.Light:
                    return LiteEntityCamp.Dark;
                default:
                    break;
            }

            return camp;
        }
    }
}