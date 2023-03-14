using System;
using Platform;
using UnityEngine;

public class Test : MonoBehaviour
{
    public GameObject CharacterGo;
    
    private PhysicsWorld World_;
    private Character Character_;

    void Start()
    {
        CharacterGo.SetActive(true);
        
        World_ = new PhysicsWorld();
        Character_ = new Character(World_, CharacterGo, new AABB(0, 0, Const.CharacterHalfX, Const.CharacterHalfY, 0, Const.CharacterHalfY));
        World_.AddObject(Character_);
    }

    private void OnDestroy()
    {
        World_.Dispose();
    }

    void Update()
    {
        InputMgr.Instance.Update();

        if (Input.GetMouseButtonDown(0))
        {
            var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var x = World_.GetMap().GetMapTileXAtPoint(worldPos.x);
            var y = World_.GetMap().GetMapTileYAtPoint(worldPos.y);

            if (Input.GetKey(KeyCode.LeftControl))
            {
                World_.GetMap().SetTile(x, y, World_.GetMap().GetTile(x, y) == TileType.Empty ? TileType.OneWay : TileType.Empty);
            }
            else
            {
                World_.GetMap().SetTile(x, y, World_.GetMap().GetTile(x, y) == TileType.Empty ? TileType.Block : TileType.Empty);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var x = World_.GetMap().GetMapTileXAtPoint(worldPos.x);
            var y = World_.GetMap().GetMapTileYAtPoint(worldPos.y);
            var tilePosX = World_.GetMap().GetMapTilePositionX(x);
            var tilePosY = World_.GetMap().GetMapTilePositionY(y);
            Character_.SetPosition(new Vector2((float)tilePosX, (float)tilePosY));
        }
    }

    private int a = 0;
    private void FixedUpdate()
    {
        a++;
        if (a % 3 == 0)
        {
            World_.Update(Time.fixedDeltaTime * 3);
        }
    }

    private void OnGUI()
    {
    }
}