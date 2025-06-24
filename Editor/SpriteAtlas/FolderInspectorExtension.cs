using System.IO;
using UnityEditor;
using UnityEngine;

namespace VomitLib.Editor.SpriteAtlas
{
    /// <summary>
    /// 文件夹Inspector扩展，显示SpriteAtlas checkbox
    /// </summary>
    [CustomEditor(typeof(DefaultAsset), true)]
    public class FolderInspectorExtension : UnityEditor.Editor
    {
        private bool _isFolder;
        private string _folderPath;
        private bool _generateSpriteAtlas = false;

        private void OnEnable()
        {
            _folderPath = AssetDatabase.GetAssetPath(target);
            _isFolder = Directory.Exists(_folderPath);
            
            // 初始化checkbox状态
            if (_isFolder)
            {
                _generateSpriteAtlas = SpriteAtlasManager.HasSpriteAtlas(_folderPath);
            }
        }

        public override void OnInspectorGUI()
        {
            // 显示默认Inspector内容
            DrawDefaultInspector();

            GUI.enabled = true;
            
            // 只对文件夹显示SpriteAtlas选项
            if (!_isFolder)
                return;

            // 检查文件夹是否包含图片资源
            if (!HasSpriteAssets(_folderPath))
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SpriteAtlas 设置", EditorStyles.boldLabel);

            // 更新checkbox状态
            bool currentState = SpriteAtlasManager.HasSpriteAtlas(_folderPath);
            
            EditorGUI.BeginChangeCheck();
            bool newState = EditorGUILayout.Toggle("生成 SpriteAtlas", currentState);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (newState)
                {
                    // 创建SpriteAtlas
                    SpriteAtlasManager.CreateSpriteAtlas(_folderPath);
                }
                else
                {
                    // 删除SpriteAtlas
                    SpriteAtlasManager.RemoveSpriteAtlas(_folderPath);
                }
                
                _generateSpriteAtlas = newState;
            }
            
            // 如果已有图集，显示信息
            if (currentState)
            {
                var atlasInfo = SpriteAtlasManager.GetAtlasInfo(_folderPath);
                if (atlasInfo != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("图集名称:", atlasInfo.atlasName);
                    EditorGUILayout.LabelField("图集路径:", atlasInfo.atlasPath);
                }
            }
        }

        /// <summary>
        /// 检查文件夹是否包含Sprite资源
        /// </summary>
        private bool HasSpriteAssets(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                
                if (importer != null && importer.textureType == TextureImporterType.Sprite)
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}