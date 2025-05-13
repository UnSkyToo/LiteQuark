using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class AssetViewerWindow : EditorWindow
    {
        private TreeViewState _assetTreeState;
        private AssetViewerTreeView _assetTreeView;
        private SearchField _searchField;
        private AssetViewerTreeItem _selectItem = null;
        private Vector2 _detailScrollPos = Vector2.zero;

        private bool _combineMode = true;
        
        [MenuItem("Lite/Asset Viewer")]
        private static void ShowWin()
        {
            var win = GetWindow<AssetViewerWindow>();
            win.titleContent = new GUIContent("Asset Viewer");
            win.Show();
        }

        private void OnEnable()
        {
            if (_assetTreeState == null)
            {
                _assetTreeState = new TreeViewState();
            }
            
            _assetTreeView = new AssetViewerTreeView(_assetTreeState);
            _searchField = new SearchField();

            _assetTreeView.OnItemSelectionChanged += (item) =>
            {
                _selectItem = item;
                _detailScrollPos = Vector2.zero;
            };
        }

        private void OnDisable()
        {
            _assetTreeState = null;
        }

        private void OnGUI()
        {
            if (!_assetTreeView.IsLoaded)
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
                _combineMode = EditorGUILayout.Toggle(new GUIContent("Combine Mode", "Displaying bundles that merge sub paths"), _combineMode);
                if (EditorGUI.EndChangeCheck())
                {
                    _assetTreeView.CombineMode = _combineMode;
                    _assetTreeView.Reload();
                }

                using (new ColorScope(Color.red))
                {
                    EditorGUILayout.LabelField("Exclude folders whose names contain '~' or '#', as well as assets within the Packages directory.");
                }
            }
            
            var searchRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            _assetTreeView.searchString = _searchField.OnGUI(searchRect, _assetTreeView.searchString);

            var treeRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            treeRect.height -= 150;
            _assetTreeView.OnGUI(treeRect);

            var detailRect = new Rect(treeRect.x, treeRect.yMax + 5, treeRect.width, 140);
            _detailScrollPos = GUI.BeginScrollView(detailRect, _detailScrollPos, new Rect(0, 0, detailRect.width, detailRect.height), true, false);
            if (_selectItem != null && _selectItem.DependencyList.Length > 0)
            {
                var y = 0f;
                foreach (var item in _selectItem.DependencyList)
                {
                    EditorGUI.LabelField(new Rect(0, y, detailRect.width - 10, EditorGUIUtility.singleLineHeight), item);
                    y += EditorGUIUtility.singleLineHeight;
                }
            }

            GUI.EndScrollView();
        }
    }
}