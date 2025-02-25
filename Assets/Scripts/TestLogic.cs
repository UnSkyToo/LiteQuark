using System;
using System.Collections.Generic;
using LiteBattle.Runtime;
using UnityEditor.Animations;
using UnityEngine;

namespace LiteGame
{
    public class TestLogic : MonoBehaviour
    {
        [SerializeField]
        public string PlayerAssetName;

        private List<string> animationNames = new List<string>();

        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            // var a = GameObject.Find("MaleCharacter");
            // var b = a.GetComponentsInChildren<Animator>();
            // for (int j = 0; j < b.Length; j++)
            // {
            //     var ac = b[j].runtimeAnimatorController as AnimatorController;
            //     if (ac == null)
            //     {
            //         continue;
            //     }
            //     ChildAnimatorState[] stList = ac.layers[0].stateMachine.states;
            //
            //     for (var i = 0; i < stList.Length; ++i)
            //     {
            //         var c = stList[i].state.name;
            //         animationNames.Add(c);
            //     }
            // }
            
            LiteAssetMgr.Instance.SetHandler(new ResourceHandler());
            
            LiteBattleEngine.Instance.Startup(PlayerAssetName);
        }

        private void OnDestroy()
        {
            LiteBattleEngine.Instance.Shutdown();
        }

        private void Update()
        {
            LiteBattleEngine.Instance.Tick(0.0166666667f);
        }

        private Vector2 pos_ = Vector2.zero;
        private void OnGUI()
        {
            // pos_ = GUILayout.BeginScrollView(pos_);
            // foreach (var name in animationNames)
            // {
            //     if (GUILayout.Button(name))
            //     {
            //         var a = GameObject.Find("MaleCharacter");
            //         var b = a.GetComponentsInChildren<Animator>();
            //         b[0].Play(name, 0, 0);
            //     }
            // }
            // GUILayout.EndScrollView();
        }
    }
}