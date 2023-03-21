using UnityEngine;

namespace InfiniteGame
{
    public class BattlePreviewer : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            var player = BattleManager.Instance.GetPlayer();
            if (player != null)
            {
                DrawCircle(player.GetCircle(), Color.yellow);
            }

            foreach (var entity in BattleManager.Instance.GetEnemies())
            {
                DrawCircle(entity.GetCircle(), Color.red);
            }

            foreach (var entity in BattleManager.Instance.GetBullets())
            {
                DrawCircle(entity.GetCircle(), Color.blue);
            }

            foreach (var entity in BattleManager.Instance.GetExps())
            {
                DrawCircle(entity.GetCircle(), Color.green);
            }
        }

        private void DrawCircle(CircleArea circle, Color color)
        {
            var lastColor = Gizmos.color;
            Gizmos.color = color;

            var beginPoint = Vector3.zero;
            var firstPoint = Vector3.zero;
            for (float theta = 0; theta < 2 * Mathf.PI; theta += 0.1f)
            {
                var x = circle.Radius * Mathf.Cos(theta);
                var y = circle.Radius * Mathf.Sin(theta);
                var endPoint = new Vector3(circle.X + x, circle.Y + y, 0);
                if (theta == 0)
                {
                    firstPoint = endPoint;
                }
                else
                {
                    Gizmos.DrawLine(beginPoint, endPoint);
                }
                beginPoint = endPoint;
            }
            
            Gizmos.DrawLine(firstPoint, beginPoint);

            Gizmos.color = lastColor;
        }
    }
}