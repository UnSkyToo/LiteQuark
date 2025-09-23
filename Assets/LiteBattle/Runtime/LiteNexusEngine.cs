using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public class LiteNexusEngine : Singleton<LiteNexusEngine>, IManager
    {
        public LiteContext GlobalContext { get; }

        private LitePlayerController PlayerController_;

        protected LiteNexusEngine()
        {
            GlobalContext = new LiteContext(null);
        }

        public async UniTask<bool> Startup()
        {
            await LiteNexusDataManager.Instance.Startup();
            await LiteInputManager.Instance.Startup();
            await LiteEntityManager.Instance.Startup();
            await LiteCameraManager.Instance.Startup();

            var unit = LiteEntityManager.Instance.AddUnit("player");
            LiteCameraManager.Instance.Bind(UnityEngine.Camera.main, unit);
            unit.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.MaxHp, 100);
            unit.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.CurHp, 100);
            unit.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.Atk, 10);
            unit.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.Def, 5);
            unit.Camp = LiteEntityCamp.Light;
            unit.Tag = "Player";
            PlayerController_ = new LitePlayerController(unit);

            var monster = LiteEntityManager.Instance.AddUnit("player_test");
            monster.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.MaxHp, 100);
            monster.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.CurHp, 100);
            monster.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.Atk, 10);
            monster.GetModule<LiteEntityDataModule>().AddChange(LiteEntityDataType.Def, 5);
            monster.Position = new Vector3(3, 0, 3);
            monster.Camp = LiteEntityCamp.Dark;
            monster.Tag = "Monster Test";
            
            return true;
        }

        public void Shutdown()
        {
            PlayerController_?.Dispose();
            
            LiteCameraManager.Instance.Shutdown();
            LiteEntityManager.Instance.Shutdown();
            LiteInputManager.Instance.Shutdown();
            LiteNexusDataManager.Instance.Shutdown();
        }

        public void Tick(float deltaTime)
        {
            PreTick(deltaTime);
            LiteInputManager.Instance.Tick(deltaTime);
            LiteEntityManager.Instance.Tick(deltaTime);
            LiteCameraManager.Instance.Tick(deltaTime);
            PostTick(deltaTime);
            
            // if (Input.GetKeyDown(KeyCode.Z))
            // {
            //     var entity = LiteEntityManager.Instance.AddAttackEntity();
            //     entity.Tag = "Bullet";
            //     entity.Camp = PlayerController_.Unit.Camp;
            //
            //     var dir = (PlayerController_.Unit.Rotation * Vector3.forward).normalized;
            //     entity.Position = PlayerController_.Unit.Position + dir * 1;
            //     var targetPos = entity.Position + dir * 5f;
            //     entity.GetModule<LiteEntityMovementModule>().MoveToPos(targetPos);
            // }
        }

        private void PreTick(float deltaTime)
        {
        }

        private void PostTick(float deltaTime)
        {
            LiteEntityManager.Instance.PostTick(deltaTime);
            GlobalContext.Tick();
        }
    }
}