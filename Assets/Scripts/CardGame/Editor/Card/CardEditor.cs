using System.Collections.Generic;
using LiteCard.GamePlay;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteCard.Editor
{
    public sealed class CardEditor : EditorWindow
    {
        [MenuItem("Lite/Card Editor &K")]
        private static void ShowWin()
        {
            var win = GetWindow<CardEditor>("Card Editor");
            win.Show();
        }

        private readonly List<IEditorView> Views_ = new List<IEditorView>();
        private readonly List<GUIContent> Toolbars_ = new List<GUIContent>();
        private int ViewIndex_ = 0;

        private static CardEditor Instance_;

        private void OnEnable()
        {
            Instance_ = this;
            
            TypeUtils.AddAssembly(typeof(CardGameLogic).Assembly, 0);
            
            Views_.Clear();
            Views_.Add(new CardDataView("Card", "Assets/StandaloneAssets/CardGame/Json/card.json"));
            Views_.Add(new BuffDataView("Buff", "Assets/StandaloneAssets/CardGame/Json/buff.json"));
            Views_.Add(new DataView<ModifierConfig>("Modifier", "Assets/StandaloneAssets/CardGame/Json/modifier.json"));
            Views_.Add(new DataView<MatchConfig>("Match", "Assets/StandaloneAssets/CardGame/Json/match.json"));
            Views_.Add(new HelpView());
            
            foreach (var view in Views_)
            {
                Toolbars_.Add(new GUIContent(view.Name));
                view.Load();
            }

            ViewIndex_ = 0;
        }

        private void OnDisable()
        {
            foreach (var view in Views_)
            {
                view.Save();
            }
            
            LiteEditor.LiteReorderableListWrap.Clear();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (Views_.Count == 0)
            {
                return;
            }
            
            ViewIndex_ = GUI.Toolbar(new Rect(0, 0, position.width, 20), ViewIndex_, Toolbars_.ToArray());
            Views_[ViewIndex_].Draw(new Rect(0, 20, position.width, position.height));
        }

        public static T GetView<T>(EditorDataPopupType type)
        {
            return (T)Instance_.Views_[(int)type];
        }
    }
}