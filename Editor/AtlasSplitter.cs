using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public sealed class AtlasSplitter
    {
        [MenuItem("Assets/Atlas Splitter")]
        private static void Entry()
        {
            var selectObj = Selection.activeObject;
            if (selectObj != null)
            {
                Process(selectObj);
            }
        }

        private static void Process(UnityEngine.Object obj)
        {
            var selectionPath = AssetDatabase.GetAssetPath(obj);
            if (!selectionPath.EndsWith(".atlas"))
            {
                EditorUtility.DisplayDialog("Error", "Please select a atlas file!", "OK");
                return;
            }

            var rootPath = Path.GetDirectoryName(selectionPath);
            var lines = new List<string>(File.ReadAllLines(selectionPath));
            
            TextureImporter mainImporter = null;
            Vector2 mainTextureSize = Vector2.zero;
            List<SpriteMetaData> MetaList_ = null;

            void ApplyImporter()
            {
                if (mainImporter == null)
                {
                    return;
                }

                mainImporter.spritesheet = MetaList_.ToArray();
                mainImporter.textureType = TextureImporterType.Sprite;
                mainImporter.spriteImportMode = SpriteImportMode.Multiple;
                
                AssetDatabase.ImportAsset(mainImporter.assetPath, ImportAssetOptions.ForceUpdate);
            }

            var index = 0;
            while (index < lines.Count)
            {
                var line = lines[index++];
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.EndsWith(".png"))
                {
                    ApplyImporter();
                    
                    mainImporter = AssetImporter.GetAtPath($"{rootPath}/{line}") as TextureImporter;
                    MetaList_ = new List<SpriteMetaData>();
                    
                    var size = lines[index++].Trim();
                    var format = lines[index++].Trim();
                    var filter = lines[index++].Trim();
                    var repeat = lines[index++].Trim();
                    
                    var w = int.Parse(size.Substring(5).Split(',')[0]);
                    var h = int.Parse(size.Substring(5).Split(',')[1]);
                    mainTextureSize = new Vector2(w, h);
                    
                    Debug.Log($"process {line}");
                }
                else
                {
                    var path = line.Trim();
                    var rotate = lines[index++].Trim();
                    var xy = lines[index++].Trim();
                    var size = lines[index++].Trim();
                    var orig = lines[index++].Trim();
                    var offset = lines[index++].Trim();
                    var unknown_index = lines[index++].Trim();

                    var x = int.Parse(xy.Substring(3).Split(',')[0]);
                    var y = int.Parse(xy.Substring(3).Split(',')[1]);
                    var w = int.Parse(size.Substring(5).Split(',')[0]);
                    var h = int.Parse(size.Substring(5).Split(',')[1]);

                    var meta = new SpriteMetaData();
                    meta.alignment = 0;
                    meta.border = new Vector4(0, 0, 0, 0);
                    meta.name = line.Replace('/', '_');
                    meta.pivot = new Vector2(0.5f, 0.5f);
                    meta.rect = new Rect(
                        x,
                        mainTextureSize.y - y - h,
                        w,
                        h);
                    MetaList_.Add(meta);
                }
            }
            
            ApplyImporter();
            Debug.Log("process success");
        }
    }
}