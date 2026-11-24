using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal abstract partial class BundleValidation
    {
        internal static void BuildFixtures(string suite, Func<AssetBundleBuild[]> media = null)
        {
            SessionState.SetString(PendingKey + ".Suite", suite);
            if (AssetDatabase.IsValidFolder(Fixture))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(Fixture);
            }
            Directory.CreateDirectory(Fixture);
            Directory.CreateDirectory(BundleFolder);
            File.WriteAllText(Fixture + "/payload.txt", "LiteQuark session validation");
            File.WriteAllText(Fixture + "/second.txt", "Second asset in the shared text bundle");
            AssetDatabase.Refresh();
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader == null) throw new Exception("No fixture shader available.");
            var material = new Material(shader) { color = Color.cyan };
            AssetDatabase.CreateAsset(material, Fixture + "/shared.mat");
            foreach (var name in new[] { "a", "c" })
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "validation_" + name;
                go.GetComponent<Renderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(go, Fixture + "/" + name + ".prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }
            var builds = new List<AssetBundleBuild>
            {
                Build("text.ab", "payload.txt", "second.txt"), Build("b.ab", "shared.mat"),
                Build("a.ab", "a.prefab"), Build("c.ab", "c.prefab")
            };
            if (suite == "lifecycle")
            {
                Directory.CreateDirectory(EditorSceneFolder);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "SessionValidationSceneMarker";
                marker.GetComponent<Renderer>().sharedMaterial = material;
                EditorSceneManager.SaveScene(scene, Fixture + "/validation_scene.unity");
                EditorSceneManager.SaveScene(scene, EditorScenePath, true);
                var host = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(host, EditorSceneFolder + "/host.unity");
                builds.Add(Build("scene.ab", "validation_scene.unity"));
            }
            if (media != null) builds.AddRange(media());
            AssetDatabase.SaveAssets();
            var manifest = BuildPipeline.BuildAssetBundles(BundleFolder, builds.ToArray(),
                BuildAssetBundleOptions.UncompressedAssetBundle, ValidationEnvironment.BundleTarget);
            if (manifest == null) throw new Exception("Fixture bundle build failed.");
            if (!manifest.GetAllDependencies("a.ab").Contains("b.ab") ||
                !manifest.GetAllDependencies("c.ab").Contains("b.ab") ||
                (suite == "lifecycle" && !manifest.GetAllDependencies("scene.ab").Contains("b.ab")))
                throw new Exception("Fixtures did not produce the required real shared dependency.");
            File.WriteAllText(Path.Combine(ValidationEnvironment.Reports(suite), "fixture-dependencies.txt"),
                string.Join("\n", manifest.GetAllAssetBundles().Select(b => b + " -> " + string.Join(",", manifest.GetAllDependencies(b)))));
        }

        protected static AssetBundleBuild Build(string bundle, params string[] assets) =>
            new() { assetBundleName = bundle, assetNames = assets.Select(asset => Fixture + "/" + asset).ToArray() };
    }
}
