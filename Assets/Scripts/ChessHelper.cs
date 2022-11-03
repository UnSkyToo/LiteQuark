using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay
{
    public static class ChessHelper
    {
        public static Vector2Int ScreenToBoardPos(Vector2 mousePosition, int width, int height)
        {
            var worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0));
            // Debug.Log(worldPosition);
            var x = Mathf.RoundToInt(worldPosition.x * PixelPerUnit / XStep + (float)(width - 1) * 0.5f);
            var y = Mathf.RoundToInt(-worldPosition.y * PixelPerUnit / YStep + (float)(height - 1) * 0.5f);
            // Debug.Log(x + "," + y);
            return new Vector2Int(x, y);
        }

        public static Vector2 BoardToWorldPos(int x, int y, int width, int height)
        {
            var baseX = x - ((float)width - 1) * 0.5f;
            var baseY = y - ((float)height - 1) * 0.5f;
            var worldX = baseX * XStep / PixelPerUnit;
            var worldY = -baseY * YStep / PixelPerUnit;
            // Debug.Log(worldX + "," + worldY);
            return new Vector2(worldX, worldY);
        }

        public static void GenerateChessBoard(Transform board, int width, int height, string linePrefab = "chessboard/prefab/line.prefab")
        {
            AssetManager.Instance.LoadAsset<GameObject>(linePrefab, (go) =>
            {
                CreateBoard(board, go, width, height);
            });
        }

        public static void GenerateChess(Transform board, ChessKind kind, int x, int y, int width, int height)
        {
            var chessPrefab = string.Empty;
            switch (kind)
            {
                case ChessKind.White:
                    chessPrefab = "chessboard/prefab/chess_white.prefab";
                    break;
                case ChessKind.Black:
                    chessPrefab = "chessboard/prefab/chess_black.prefab";
                    break;
                case ChessKind.None:
                    return;
            }

            AssetManager.Instance.LoadAsset<GameObject>(chessPrefab, (go) =>
            {
                CreateChess(board, go, x, y, width, height);
            });
        }

        private const float PixelPerUnit = 100f;
        private const float OneLineWidth = 256f;
        private const float OneLineHeight = 256f;
        private const int XStep = 40;
        private const int YStep = 40;

        private const int OrderNormalLine = 10;
        private const int OrderBorderLine = 20;
        private const int OrderChess = 30;

        private static void CreateChess(Transform board, GameObject chessPrefab, int x, int y, int width, int height)
        {
            var container = board.Find("Chess");

            var go = Object.Instantiate(chessPrefab, container, false);
            go.transform.localPosition = BoardToWorldPos(x, y, width, height);
            go.GetComponent<SpriteRenderer>().sortingOrder = OrderChess;
        }

        private static void CreateBoard(Transform board, GameObject linePrefab, int xCount, int yCount)
        {
            CreateHorizontal(board, linePrefab, xCount);
            CreateVertical(board, linePrefab, yCount);

            var container = new GameObject("Chess");
            container.transform.SetParent(board, false);
            container.transform.localPosition = Vector3.zero;
        }
        
        private static void CreateHorizontal(Transform parent, GameObject linePrefab, int count)
        {
            var totalHeight = (count - 1) * YStep;
            var beginY = totalHeight / 2;

            var container = new GameObject("Horizontal");
            container.transform.SetParent(parent, false);
            container.transform.localPosition = Vector3.zero;
            
            for (var i = 0; i < count; ++i)
            {
                CreateLine(container.transform, linePrefab, $"L{i + 1}", 0, beginY - i * YStep, totalHeight, 1,OrderNormalLine);
            }
        }

        private static void CreateVertical(Transform parent, GameObject linePrefab, int count)
        {
            var totalWidth = (count - 1) * XStep;
            var beginX = -totalWidth / 2;

            var container = new GameObject("Vertical");
            container.transform.SetParent(parent, false);
            container.transform.localPosition = Vector3.zero;
            
            for (var i = 0; i < count; ++i)
            {
                CreateLine(container.transform, linePrefab, $"L{i + 1}", beginX + i * XStep, 0, 1, totalWidth, OrderNormalLine);
            }
        }

        private static void CreateLine(Transform parent, GameObject linePrefab, string name, int x, int y, int pixelWidth, int pixelHeight, int order, bool isBold = false)
        {
            var fx = (float)x / PixelPerUnit;
            var fy = (float)y / PixelPerUnit;
            var fw = (float)pixelWidth * (isBold ? 3 : 1) / OneLineWidth;
            var fh = (float)pixelHeight * (isBold ? 3 : 1) / OneLineHeight;

            var go = Object.Instantiate(linePrefab, parent, false);
            go.name = name;
            go.transform.localPosition = new Vector3(fx, fy, 0);
            go.transform.localScale = new Vector3(fw, fh, 1);
            go.transform.localRotation = Quaternion.identity;

            go.GetComponent<SpriteRenderer>().sortingOrder = order;
        }
    }
}