using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay.Chess
{
    public sealed class ChessPlayer : ITick
    {
        public ChessKind Kind { get; }
        
        private readonly ChessBoard Board_;
        private readonly bool EnableAI_;

        private bool Ready_;
        private float AIWaitTime_ = 0;
        
        public ChessPlayer(ChessBoard board, ChessKind kind, bool enableAI)
        {
            Board_ = board;
            Kind = kind;
            EnableAI_ = enableAI;

            Ready_ = false;
        }

        public void Tick(float deltaTime)
        {
            if (!Ready_)
            {
                return;
            }

            AIWaitTime_ -= deltaTime;

            if (EnableAI_)
            {
                if (AIWaitTime_ < 0)
                {
                    var bestCoordList = AI.Valuation.TupleUtils.GetBestValuePoints(Board_, Kind);
                    if (bestCoordList.Length > 0)
                    {
                        var coord = bestCoordList[Random.Range(0, bestCoordList.Length)];
                        DoChess(coord.X, coord.Y);
                    }
                    else
                    {
                        Debug.LogError("ai can not calculate point");
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var pos = ChessUtils.ScreenToBoardPos(Input.mousePosition, Board_.Width, Board_.Height);
                    DoChess(pos.x, pos.y);
                }
            }
        }

        public void NotifyReady()
        {
            Ready_ = true;
            AIWaitTime_ = 0.35f;
        }

        private void DoChess(int x, int y)
        {
            if (Board_.DoChess(this, x, y))
            {
                LiteRuntime.Get<AudioSystem>().PlaySound("chess/audio/down.ogg");
                Ready_ = false;
            }
        }
    }
}