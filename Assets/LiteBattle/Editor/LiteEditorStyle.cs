using LiteBattle.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public static class LiteEditorStyle
    {
        public static GUIContent OptionIcon { get; private set; } = L10n.IconContent("_Popup", "Options");
        public static GUIContent NewContent { get; private set; } = L10n.IconContent("CreateAddNew", "Show Options.");
        public static GUIStyle DropdownOption { get; private set; } = EditorStyles.FromUSS("Icon-TrackOptions");
        public static GUIStyle ButtonNormal { get; private set; } = GUI.skin.GetStyle("button");
        public static GUIStyle ButtonSelect { get; private set; }
        
        public static GUIContent PlayContent { get; private set; } = L10n.IconContent("Animation.Play", "Play the timeline (Space)");
        public static GUIContent GotoBeginingContent { get; private set; } = L10n.IconContent("Animation.FirstKey", "Go to the beginning of the timeline (Shift+<)");
        public static GUIContent GotoEndContent { get; private set; } = L10n.IconContent("Animation.LastKey", "Go to the end of the timeline (Shift+>)");
        public static GUIContent NextFrameContent { get; private set; } = L10n.IconContent("Animation.NextKey", "Go to the next frame");
        public static GUIContent PreviousFrameContent { get; private set; } = L10n.IconContent("Animation.PrevKey", "Go to the previous frame");
        
        public static GUIStyle InFooter { get; private set; } = GUI.skin.GetStyle("IN Footer");
        public static GUIStyle FrameBox { get; private set; } = GUI.skin.GetStyle("FrameBox");

        public static void Generate()
        {
            ButtonSelect = new GUIStyle(GUI.skin.GetStyle("button"));
            ButtonSelect.normal.textColor = Color.green;
            ButtonSelect.hover.textColor = ButtonSelect.normal.textColor;

            var transferTrackContent = CreateGUIContentFromImage("Icons/LiteTransferTrackIcon.png");
            LiteTimelineHelper.SetTrackIcon<LiteTimelineStateTransferTrack>(transferTrackContent);
        }

        private static GUIContent CreateGUIContentFromImage(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture>($"Assets/LiteBattle/Editor/StyleSheets/Images/{path}");
            var content = new GUIContent(texture);
            return content;
        }
    }
}