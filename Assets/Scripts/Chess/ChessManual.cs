using System.Collections.Generic;
using System.IO;

namespace LiteGamePlay
{
    public sealed class ChessManual
    {
        private ChessKind WinKind_;
        private List<ChessCoord> StepList_;
        
        public ChessManual()
        {
            WinKind_ = ChessKind.Invalid;
            StepList_ = new List<ChessCoord>();
        }

        public void Win(ChessKind winKind)
        {
            WinKind_ = winKind;
        }

        public void AddStep(int x, int y)
        {
            StepList_.Add(new ChessCoord(x, y));
        }

        public void Encode(BinaryWriter codec)
        {
            codec.Write((byte)WinKind_);
            codec.Write(StepList_.Count);
            foreach (var coord in StepList_)
            {
                codec.Write((byte)coord.X);
                codec.Write((byte)coord.Y);
            }
        }

        public void Decode(BinaryReader codec)
        {
            WinKind_ = (ChessKind)codec.ReadByte();
            var length = codec.ReadInt32();
            StepList_.Clear();
            for (var index = 0; index < length; ++index)
            {
                var x = codec.ReadByte();
                var y = codec.ReadByte();
                StepList_[index] = new ChessCoord(x, y);
            }
        }
    }
}