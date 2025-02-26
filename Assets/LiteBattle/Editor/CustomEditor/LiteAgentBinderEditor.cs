// using LiteBattle.Runtime;
// using UnityEditor;
// using UnityEngine;
//
// namespace LiteBattle.Editor
// {
//     [CustomEditor(typeof(LiteAgentBinder))]
//     public class LiteAgentBinderEditor : UnityEditor.Editor
//     {
//         public override void OnInspectorGUI()
//         {
//             base.OnInspectorGUI();
//
//             var binder = serializedObject.targetObject as LiteAgentBinder;
//             
//             EditorGUI.BeginChangeCheck();
//             var newIndex = EditorGUILayout.Popup(new GUIContent("Animation"), binder.GetPreviewAnimatorStateIndex(), binder.GetAnimatorStateNameList().ToArray());
//             if (EditorGUI.EndChangeCheck())
//             {
//                 binder.SetPreviewAnimatorStateIndex(newIndex);
//             }
//
//             var frame = LiteTimelineHelper.TimeToFrame(binder.GetPreviewAnimatorStateTime());
//             var maxFrame = LiteTimelineHelper.TimeToFrame(binder.GetAnimatorStateLength(newIndex));
//             
//             EditorGUI.BeginChangeCheck();
//             var newFrame = EditorGUILayout.IntSlider("Frame", frame, 0, maxFrame);
//             if (EditorGUI.EndChangeCheck())
//             {
//                 var time = (float) LiteTimelineHelper.FrameToTime(newFrame);
//                 binder.SetPreviewAnimatorStateTime(time);
//             }
//         }
//     }
// }