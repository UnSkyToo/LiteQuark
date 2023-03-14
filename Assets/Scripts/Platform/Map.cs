using System;
using LiteQuark.Runtime;
using UnityEngine;

namespace Platform
{
    public enum TileType : int
    {
        Empty,
        Block,
        OneWay,
    }

    public class Map : IDisposable
    {
        public int Width { get; }
        public int Height { get; }

        public Vector3 Position => Position_;

        private Vector3 Position_;
        private readonly TileType[,] Tiles_;
        private readonly GameObject[,] Sprites_;
        
        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            
            Tiles_ = new TileType[width, height];
            Sprites_ = new GameObject[width, height];
            
            InitMaps();
        }

        public void Dispose()
        {
            for (var x = 0; x < Width; ++x)
            {
                for (var y = 0; y < Height; ++y)
                {
                    GameObject.DestroyImmediate(Sprites_[x, y]);
                }
            }
        }
        
        private bool IsValid(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsObstacle(int x, int y)
        {
            if (!IsValid(x, y))
            {
                return true;
            }

            return Tiles_[x, y] == TileType.Block;
        }

        public bool IsGround(int x, int y)
        {
            if (!IsValid(x, y))
            {
                return false;
            }

            return Tiles_[x, y] == TileType.OneWay || Tiles_[x, y] == TileType.Block;
        }

        public bool IsOneWay(int x, int y)
        {
            if (!IsValid(x, y))
            {
                return false;
            }

            return Tiles_[x, y] == TileType.OneWay;
        }

        public bool IsEmpty(int x, int y)
        {
            if (!IsValid(x, y))
            {
                return false;
            }

            return Tiles_[x, y] == TileType.Empty;
        }

        public Vector2Int GetMapTileAtPoint(double x, double y)
        {
            return new Vector2Int(GetMapTileXAtPoint(x), GetMapTileYAtPoint(y));
        }

        public int GetMapTileXAtPoint(double x)
        {
            return (int)Math.Round((x - Position_.x) / (double)Const.TileSizeX);
        }

        public int GetMapTileYAtPoint(double y)
        {
            return (int)Math.Round((y - Position_.y) / (double)Const.TileSizeY);
        }

        public double GetMapTilePositionX(int tileIndexX)
        {
           return (tileIndexX * (double)Const.TileSizeX) + Position_.x;
        }

        public double GetMapTilePositionY(int tileIndexY)
        {
            return (tileIndexY * (double)Const.TileSizeY) + Position_.y;
        }

        // public Vector2 GetMapTilePosition(int tileIndexX, int tileIndexY)
        // {
        //     return new Vector2((float)GetMapTilePositionX(tileIndexX), (float)GetMapTilePositionY(tileIndexY));
        // }

        private TileType GetTileType(int x, int y)
        {
            if (!IsValid(x, y))
            {
                return TileType.Block;
            }

            return Tiles_[x, y];
        }

        private void InitMaps()
        {
            Position_ = new Vector3(-Screen.width * 0.5f + Const.TileSizeX * 0.5f, -Screen.height * 0.5f + Const.TileSizeY * 0.5f);
            
            InitTiles();
            InitSprites();
        }

        private void InitTiles()
        {
            for (var x = 0; x < Width; ++x)
            {
                for (var y = 0; y < 4; ++y)
                {
                    Tiles_[x, y] = TileType.Block;
                }

                for (var y = Height - 4; y < Height; ++y)
                {
                    Tiles_[x, y] = TileType.Empty;
                }
            }

            Tiles_[0, 0] = TileType.Block;
            Tiles_[0, Height - 1] = TileType.Block;
            Tiles_[Width - 1, 0] = TileType.Block;
            Tiles_[Width - 1, Height - 1] = TileType.Block;
        }

        private void InitSprites()
        {
            var map = new GameObject($"Map({Width}x{Height})");
            map.transform.localPosition = Position_;
            
            for (var x = 0; x < Width; ++x)
            {
                for (var y = 0; y < Height; ++y)
                {
                    Sprites_[x, y] = new GameObject($"Tile_{x}_{y}");
                    Sprites_[x, y].transform.SetParent(map.transform, false);
                    Sprites_[x, y].transform.localPosition = new Vector3(x * Const.TileSizeX, y * Const.TileSizeY, 0);
                    RefreshTileSprite(x, y);
                }
            }
        }

        private void RefreshTileSprite(int x, int y)
        {
            var renderer = Sprites_[x, y].GetOrAddComponent<SpriteRenderer>();
            if (Tiles_[x, y] == TileType.Block)
            {
                var sprite = LiteRuntime.Get<AssetSystem>().LoadAssetSync<Sprite>("Test/block.png");
                renderer.sprite = sprite;
                renderer.color = ((x % 2) + (y %  2)) % 2 == 0 ? Color.white : Color.blue;
            }
            else if (Tiles_[x, y] == TileType.OneWay)
            {
                var sprite = LiteRuntime.Get<AssetSystem>().LoadAssetSync<Sprite>("Test/oneway.png");
                renderer.sprite = sprite;
                renderer.color = ((x % 2) + (y %  2)) % 2 == 0 ? Color.white : Color.blue;
            }
        }

        public TileType GetTile(int x, int y)
        {
            if (!IsValid(x, y))
            {
                return TileType.Block;
            }

            return Tiles_[x, y];
        }

        public void SetTile(int x, int y, TileType tileType)
        {
            if (!IsValid(x, y))
            {
                return;
            }

            Tiles_[x, y] = tileType;
            RefreshTileSprite(x, y);
        }
    }
}