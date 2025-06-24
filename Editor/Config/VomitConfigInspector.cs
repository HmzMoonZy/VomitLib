using UnityEditor;
using UnityEngine;
using Twenty2.VomitLib.Config;
using VomitLib.Editor.SpriteAtlas;

namespace Twenty2.VomitLib.Editor.Config
{
    /// <summary>
    /// VomitConfig自定义Inspector
    /// </summary>
    [CustomEditor(typeof(VomitConfig))]
    public class VomitConfigInspector : UnityEditor.Editor
    {
        private bool _showSpriteAtlasList = true;

        public override void OnInspectorGUI()
        {
            // 显示默认Inspector
            DrawDefaultInspector();

            var config = target as VomitConfig;
            if (config?.SpriteAtlasConfig == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // 显示SpriteAtlas管理部分
            _showSpriteAtlasList = EditorGUILayout.Foldout(_showSpriteAtlasList, "SpriteAtlas 管理", true, EditorStyles.foldoutHeader);
            
            if (_showSpriteAtlasList)
            {
                EditorGUI.indentLevel++;
                DrawSpriteAtlasList(config.SpriteAtlasConfig);
                EditorGUI.indentLevel--;
            }

            // 如果有修改，保存
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawSpriteAtlasList(SpriteAtlasConfig atlasConfig)
        {
            var atlases = atlasConfig.CollectedAtlases;
            
            EditorGUILayout.LabelField($"已收集的图集数量: {atlases.Count}", EditorStyles.helpBox);
            
            if (atlases.Count == 0)
            {
                EditorGUILayout.LabelField("暂无已收集的图集", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.Space();

            // 显示每个图集
            for (int i = atlases.Count - 1; i >= 0; i--)
            {
                var atlas = atlases[i];
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}. {atlas.atlasName}", EditorStyles.boldLabel);
                
                // 删除按钮
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", 
                        $"确定要删除图集 '{atlas.atlasName}' 吗？\n这将删除图集文件并取消文件夹的关联。", 
                        "删除", "取消"))
                    {
                        SpriteAtlasManager.RemoveSpriteAtlasByConfigIndex(i);
                        break; // 跳出循环，因为列表已经改变
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // 图集信息
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("文件夹路径:", atlas.folderPath);
                EditorGUILayout.LabelField("图集路径:", atlas.atlasPath);
                
                // 状态检查
                bool folderExists = System.IO.Directory.Exists(atlas.folderPath);
                bool atlasExists = System.IO.File.Exists(atlas.atlasPath);
                
                if (!folderExists || !atlasExists)
                {
                    EditorGUILayout.HelpBox(
                        $"状态异常: 文件夹存在={folderExists}, 图集存在={atlasExists}", 
                        MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.Space();
            
            // 清理按钮
            if (GUILayout.Button("清理无效项"))
            {
                int cleanedCount = CleanupInvalidAtlases(atlasConfig);
                if (cleanedCount > 0)
                {
                    EditorUtility.DisplayDialog("清理完成", $"已清理 {cleanedCount} 个无效项", "确定");
                    EditorUtility.SetDirty(target);
                }
                else
                {
                    EditorUtility.DisplayDialog("清理完成", "没有发现无效项", "确定");
                }
            }
        }

        /// <summary>
        /// 清理无效的图集项
        /// </summary>
        private int CleanupInvalidAtlases(SpriteAtlasConfig config)
        {
            var atlases = config.CollectedAtlases;
            int cleanedCount = 0;
            
            for (int i = atlases.Count - 1; i >= 0; i--)
            {
                var atlas = atlases[i];
                bool folderExists = System.IO.Directory.Exists(atlas.folderPath);
                bool atlasExists = System.IO.File.Exists(atlas.atlasPath);
                
                if (!folderExists || !atlasExists)
                {
                    atlases.RemoveAt(i);
                    cleanedCount++;
                }
            }
            
            return cleanedCount;
        }
    }
}