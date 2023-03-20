using UnityEngine;

namespace InfiniteGame
{
    public static class Const
    {
        // public static readonly Rect Boundary = new Rect(-Screen.width * 0.5f * 0.01f, -Screen.height * 0.5f * 0.01f, Screen.width * 0.01f, Screen.height * 0.01f);
        public static readonly Rect BoundaryMax = new Rect(-Screen.width * 0.01f, -Screen.height * 0.01f, Screen.width * 2 * 0.01f, Screen.height * 2 * 0.01f);

        public static class Tag
        {
            public const string Enemy = "Enemy";
            public const string EnemyBullet = "EnemyBulelt";
            public const string Bullet = "Bullet";
        }

        public static class Mask
        {
            public static readonly int Collision = LayerMask.GetMask("Collision");
        }

        // public static class Collision
        // {
        //     public static readonly ContactFilter2D BulletFilter = new ContactFilter2D
        //     {
        //         useTriggers = true,
        //         useLayerMask = true,
        //         layerMask = LayerMask.GetMask("Collision"),
        //         useDepth = false,
        //         useOutsideDepth = false,
        //         minDepth = float.NegativeInfinity,
        //         maxDepth = float.PositiveInfinity,
        //         useNormalAngle = false,
        //         useOutsideNormalAngle = false,
        //         minNormalAngle = 0.0f,
        //         maxNormalAngle = 359.9999f,
        //     };
        //     // public static readonly ContactFilter2D BulletFilter = new ContactFilter2D().NoFilter();
        // }
    }
}