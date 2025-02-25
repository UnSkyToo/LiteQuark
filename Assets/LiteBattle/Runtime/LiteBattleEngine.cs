using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteBattleEngine : Singleton<LiteBattleEngine>
    {
        public LiteContext GlobalContext { get; }

        private LitePlayerController PlayerController_;

        protected LiteBattleEngine()
        {
            GlobalContext = new LiteContext(null);
        }

        public void Startup(string playerAssetName)
        {
            LiteObjectPoolMgr.Instance.Startup();
            
            LiteInputMgr.Instance.Startup();
            LiteEntityMgr.Instance.Startup();
            LiteCameraMgr.Instance.Startup();

            var agent = LiteEntityMgr.Instance.AddAgent($"StateData/Agent/{playerAssetName}.asset");
            LiteCameraMgr.Instance.Bind(UnityEngine.Camera.main, agent);
            agent.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.MaxHp, 100);
            agent.Camp = LiteEntityCamp.Light;
            agent.Tag = "Player";

            PlayerController_ = new LitePlayerController(agent);

            var monster = LiteEntityMgr.Instance.AddAgent("StateData/Agent/player_test.asset");
            monster.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.MaxHp, 100);
            monster.Position = new Vector3(3, 0, 3);
            monster.Camp = LiteEntityCamp.Dark;
            monster.Tag = "Monster Test";
        }

        public void Shutdown()
        {
            PlayerController_.Dispose();
            
            LiteCameraMgr.Instance.Shutdown();
            LiteEntityMgr.Instance.Shutdown();
            LiteInputMgr.Instance.Shutdown();
            
            LiteObjectPoolMgr.Instance.Shutdown();
        }

        public void Tick(float deltaTime)
        {
            PreTick(deltaTime);
            LiteInputMgr.Instance.Tick(deltaTime);
            LiteEntityMgr.Instance.Tick(deltaTime);
            LiteCameraMgr.Instance.Tick(deltaTime);
            PostTick(deltaTime);
            
            // if (Input.GetKeyDown(KeyCode.Z))
            // {
            //     var entity = LiteEntityMgr.Instance.AddAttackEntity();
            //     entity.Tag = "Bullet";
            //     entity.Camp = PlayerController_.Agent.Camp;
            //
            //     var dir = (PlayerController_.Agent.Rotation * Vector3.forward).normalized;
            //     entity.Position = PlayerController_.Agent.Position + dir * 1;
            //     var targetPos = entity.Position + dir * 5f;
            //     entity.GetModule<LiteEntityMovementModule>().MoveToPos(targetPos);
            // }
        }

        private void PreTick(float deltaTime)
        {
        }

        private void PostTick(float deltaTime)
        {
            LiteEntityMgr.Instance.PostTick(deltaTime);
            GlobalContext.Tick();
        }
    }
}