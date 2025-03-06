using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class AssetViewerWindow : EditorWindow
    {
        private TreeViewState AssetTreeState_;
        private AssetViewerTreeView AssetTreeView_;
        private SearchField SearchField_;
        private AssetViewerTreeItem SelectItem_ = null;
        private Vector2 DetailScrollPos_ = Vector2.zero;

        private bool CombineMode_ = true;
        
        [MenuItem("Lite/Asset Viewer")]
        private static void ShowWin()
        {
            var win = GetWindow<AssetViewerWindow>();
            win.titleContent = new GUIContent("Asset Viewer");
            win.Show();
        }

        private void OnEnable()
        {
            if (AssetTreeState_ == null)
            {
                AssetTreeState_ = new TreeViewState();
            }
            
            AssetTreeView_ = new AssetViewerTreeView(AssetTreeState_);
            SearchField_ = new SearchField();

            AssetTreeView_.OnItemSelectionChanged += (item) =>
            {
                SelectItem_ = item;
                DetailScrollPos_ = Vector2.zero;
            };
        }

        private void OnDisable()
        {
            AssetTreeState_ = null;
        }

        private void OnGUI()
        {
            if (!AssetTreeView_.IsLoaded)
            {
                var centerRect = new Rect(0, 0, position.width, position.height);
                EditorGUI.DrawRect(centerRect, new Color(0, 0, 0, 0.5f));
                
                var style = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    normal = { textColor = Color.white }
                };
                
                GUI.Label(centerRect, "Loading...", style);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                CombineMode_ = EditorGUILayout.Toggle(new GUIContent("Combine Mode", "Displaying bundles that merge sub paths"), CombineMode_);
                if (EditorGUI.EndChangeCheck())
                {
                    AssetTreeView_.CombineMode = CombineMode_;
                    AssetTreeView_.Reload();
                }

                using (new ColorScope(Color.red))
                {
                    EditorGUILayout.LabelField("Exclude folders whose names contain '~' or '#', as well as assets within the Packages directory.");
                }
            }
            
            var searchRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            AssetTreeView_.searchString = SearchField_.OnGUI(searchRect, AssetTreeView_.searchString);

            var treeRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            treeRect.height -= 150;
            AssetTreeView_.OnGUI(treeRect);

            var detailRect = new Rect(treeRect.x, treeRect.yMax + 5, treeRect.width, 140);
            DetailScrollPos_ = GUI.BeginScrollView(detailRect, DetailScrollPos_, new Rect(0, 0, detailRect.width, detailRect.height), true, false);
            if (SelectItem_ != null && SelectItem_.DependencyList.Length > 0)
            {
                var y = 0f;
                foreach (var item in SelectItem_.DependencyList)
                {
                    EditorGUI.LabelField(new Rect(0, y, detailRect.width - 10, EditorGUIUtility.singleLineHeight), item);
                    y += EditorGUIUtility.singleLineHeight;
                }
            }

            GUI.EndScrollView();
        }
    }
}