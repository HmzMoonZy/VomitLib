using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using Twenty2.VomitLib.Config;
using Twenty2.VomitLib.Editor;
using UnityEditor.U2D;

namespace VomitLib.Editor.SpriteAtlas
{
    /// <summary>
    /// SpriteAtlas管理器
    /// </summary>
    public static class SpriteAtlasManager
    {
        /// <summary>
        /// 获取配置
        /// </summary>
        private static SpriteAtlasConfig GetConfig()
        {
            var vomitConfig = VomitEditor.Config;
            if (vomitConfig?.SpriteAtlasConfig == null)
            {
                Debug.LogError("SpriteAtlasConfig未配置，请在VomitLibConfig中设置");
                return null;
            }
            return vomitConfig.SpriteAtlasConfig;
        }

        /// <summary>
        /// 检查文件夹是否有图集
        /// </summary>
        public static bool HasSpriteAtlas(string folderPath)
        {
            var config = GetConfig();
            return config?.HasAtlas(folderPath) ?? false;
        }

        /// <summary>
        /// 创建SpriteAtlas
        /// </summary>
        public static void CreateSpriteAtlas(string folderPath)
        {
            var config = GetConfig();
            if (config == null) return;

            // 确保输出目录存在
            string outputDir = config.AtlasGeneratePath;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                AssetDatabase.Refresh();
            }

            // 生成图集名称和路径
            string folderName = Path.GetFileName(folderPath);
            string atlasName = $"{folderName}_Atlas";
            string atlasPath = Path.Combine(outputDir, $"{atlasName}.spriteatlas").Replace('\\', '/');

            // 如果已存在，先删除旧的
            if (File.Exists(atlasPath))
            {
                AssetDatabase.DeleteAsset(atlasPath);
            }

            // 创建新的SpriteAtlas
            var spriteAtlas = new UnityEngine.U2D.SpriteAtlas();
            
            // 添加文件夹到图集
            var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
            if (folderAsset != null)
            {
                spriteAtlas.Add(new Object[] { folderAsset });
            }

            // 保存图集
            AssetDatabase.CreateAsset(spriteAtlas, atlasPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 更新配置
            config.AddAtlas(folderPath, atlasPath, atlasName);
            EditorUtility.SetDirty(VomitEditor.Config);
            AssetDatabase.SaveAssets();

            Debug.Log($"已创建SpriteAtlas: {atlasPath}");
        }

        /// <summary>
        /// 删除SpriteAtlas
        /// </summary>
        public static void RemoveSpriteAtlas(string folderPath)
        {
            var config = GetConfig();
            if (config == null) return;

            var atlasInfo = config.GetAtlasInfo(folderPath);
            if (atlasInfo != null)
            {
                // 删除图集文件
                if (File.Exists(atlasInfo.atlasPath))
                {
                    AssetDatabase.DeleteAsset(atlasInfo.atlasPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                // 从配置中移除
                config.RemoveAtlas(folderPath);
                EditorUtility.SetDirty(VomitEditor.Config);
                AssetDatabase.SaveAssets();

                Debug.Log($"已删除SpriteAtlas: {atlasInfo.atlasPath}");
            }
        }

        /// <summary>
        /// 根据配置路径删除图集
        /// </summary>
        public static void RemoveSpriteAtlasByConfigIndex(int index)
        {
            var config = GetConfig();
            if (config == null || index < 0 || index >= config.CollectedAtlases.Count) 
                return;

            var atlasInfo = config.CollectedAtlases[index];
            RemoveSpriteAtlas(atlasInfo.folderPath);
        }

        /// <summary>
        /// 获取图集信息
        /// </summary>
        public static SpriteAtlasInfo GetAtlasInfo(string folderPath)
        {
            var config = GetConfig();
            return config?.GetAtlasInfo(folderPath);
        }
    }
}