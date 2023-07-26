using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MipmapViewer
{
    private static readonly Dictionary<SceneView, SceneView.CameraMode> PreviousCameraMode = new ();

    
    [InitializeOnLoadMethod]
    private static void HookIntoSceneView()
    {
        EditorApplication.delayCall += () =>
        {
            SceneView.ClearUserDefinedCameraModes();

            SceneView.AddCameraMode("Mipmap Mode", "Lite Debug");
        };

        EditorApplication.update += () =>
        {
            foreach (SceneView view in SceneView.sceneViews)
            {
                if (!PreviousCameraMode.ContainsKey(view) || PreviousCameraMode[view] != view.cameraMode)
                {
                    view.SetSceneViewShaderReplace(GetDrawModeShader(view.cameraMode), "");
                }

                PreviousCameraMode[view] = view.cameraMode;
            }
        };
    }
    
    private static Shader GetDrawModeShader(SceneView.CameraMode mode)
    {
        if (mode.name == "Mipmap Mode")
        {
            return Shader.Find("Lite/MipmapViewer");
        }

        return null;
    }
}
