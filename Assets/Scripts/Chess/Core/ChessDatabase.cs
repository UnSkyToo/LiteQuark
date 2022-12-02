using System;
using System.Collections.Generic;
using System.IO;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay.Chess
{
    public class ChessDatabase : Singleton<ChessDatabase>
    {
        private readonly List<ChessManual> ManualList_ = new List<ChessManual>();
        
        public ChessDatabase()
        {
        }

        public void AddManual(ChessManual manual)
        {
            ManualList_.Add(manual);
        }

        public bool LoadFromFile(string filePath)
        {
            try
            {
                var stream = File.OpenRead(filePath);
                var br = new BinaryReader(stream);
                Decode(br);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }
        }

        public bool SaveToFile(string filePath)
        {
            try
            {
                var stream = File.OpenWrite(filePath);
                var bw = new BinaryWriter(stream);
                Encode(bw);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                return false;
            }
        }

        private void Encode(BinaryWriter bw)
        {
            bw.Write(ManualList_.Count);
            foreach (var manual in ManualList_)
            {
                manual.Encode(bw);
            }
        }

        private void Decode(BinaryReader br)
        {
            ManualList_.Clear();
            var length = br.ReadInt32();
            for (var index = 0; index < length; ++index)
            {
                var manual = new ChessManual();
                manual.Decode(br);
                ManualList_.Add(manual);
            }
        }
    }
}