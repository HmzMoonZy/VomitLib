using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VomitLib.Editor.SpriteAtlas;

namespace Twenty2.VomitLib.Config
{
    /// <summary>
    /// SpriteAtlas配置
    /// </summary>
    [Serializable]
    public class SpriteAtlasInfo
    {
        [SerializeField] public string folderPath;
        [SerializeField] public string atlasPath;
        [SerializeField] public string atlasName;
        
        public SpriteAtlasInfo(string folder, string atlas, string name)
        {
            folderPath = folder;
            atlasPath = atlas;
            atlasName = name;
        }
    }

    [Serializable]
    public class SpriteAtlasConfig
    {
        [SerializeField, Tooltip("SpriteAtlas生成目录")]
        private string _atlasGeneratePath = "Assets/GameMain/Art/Atlas";
        
        [SerializeField, HideInInspector]
        private List<SpriteAtlasInfo> _collectedAtlases = new List<SpriteAtlasInfo>();

        public string AtlasGeneratePath 
        { 
            get => _atlasGeneratePath; 
            set => _atlasGeneratePath = value; 
        }
        
        public List<SpriteAtlasInfo> CollectedAtlases 
        { 
            get => _collectedAtlases; 
        }
        
        public void AddAtlas(string folderPath, string atlasPath, string atlasName)
        {
            // 如果已存在则更新，否则添加
            var existing = _collectedAtlases.Find(x => x.folderPath == folderPath);
            if (existing != null)
            {
                existing.atlasPath = atlasPath;
                existing.atlasName = atlasName;
            }
            else
            {
                _collectedAtlases.Add(new SpriteAtlasInfo(folderPath, atlasPath, atlasName));
            }
        }
        
        public void RemoveAtlas(string folderPath)
        {
            _collectedAtlases.RemoveAll(x => x.folderPath == folderPath);
        }
        
        public bool HasAtlas(string folderPath)
        {
            return _collectedAtlases.Exists(x => x.folderPath == folderPath);
        }
        
        public SpriteAtlasInfo GetAtlasInfo(string folderPath)
        {
            return _collectedAtlases.Find(x => x.folderPath == folderPath);
        }
    }
    
    /// <summary>
    /// SpriteAtlasConfig独立Inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(SpriteAtlasConfig))]
    public class SpriteAtlasConfigInspector : PropertyDrawer
    {
        private bool _showSpriteAtlasConfig = true;
        private bool _showSpriteAtlasList = true;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var config = GetPropertyValue<SpriteAtlasConfig>(property);
            if (config == null)
            {
                EditorGUI.LabelField(position, label.text, "SpriteAtlasConfig is null");
                EditorGUI.EndProperty();
                return;
            }

            var rect = position;
            rect.height = EditorGUIUtility.singleLineHeight;

            _showSpriteAtlasConfig = EditorGUI.Foldout(rect, _showSpriteAtlasConfig, "SpriteAtlas 配置", true, EditorStyles.foldoutHeader);
            
            if (_showSpriteAtlasConfig)
            {
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel++;
                
                // 显示基本配置
                var pathProp = property.FindPropertyRelative("_atlasGeneratePath");
                EditorGUI.PropertyField(rect, pathProp, new GUIContent("图集生成目录"));
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
                // 显示管理部分
                _showSpriteAtlasList = EditorGUI.Foldout(rect, _showSpriteAtlasList, "图集管理", true);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
                if (_showSpriteAtlasList)
                {
                    EditorGUI.indentLevel++;
                    rect = DrawSpriteAtlasList(rect, config);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_showSpriteAtlasConfig)
                return EditorGUIUtility.singleLineHeight;

            var config = GetPropertyValue<SpriteAtlasConfig>(property);
            if (config == null)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight * 3; // Header + path + atlas management header
            
            if (_showSpriteAtlasList)
            {
                var atlases = config.CollectedAtlases;
                height += EditorGUIUtility.singleLineHeight; // Count label
                
                if (atlases.Count == 0)
                {
                    height += EditorGUIUtility.singleLineHeight; // No atlas message
                }
                else
                {
                    height += atlases.Count * (EditorGUIUtility.singleLineHeight * 4 + 8); // Each atlas info
                }
                
                height += EditorGUIUtility.singleLineHeight + 4; // Cleanup button
            }
            
            return height + EditorGUIUtility.standardVerticalSpacing * 6;
        }

        /// <summary>
        /// 绘制SpriteAtlas列表
        /// </summary>
        private Rect DrawSpriteAtlasList(Rect startRect, SpriteAtlasConfig atlasConfig)
        {
            var rect = startRect;
            var atlases = atlasConfig.CollectedAtlases;
            
            EditorGUI.LabelField(rect, $"已收集的图集数量: {atlases.Count}", EditorStyles.helpBox);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            
            if (atlases.Count == 0)
            {
                EditorGUI.LabelField(rect, "暂无已收集的图集", EditorStyles.centeredGreyMiniLabel);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                return rect;
            }

            // 显示每个图集
            for (int i = atlases.Count - 1; i >= 0; i--)
            {
                var atlas = atlases[i];
                
                // Atlas name and delete button
                var nameRect = new Rect(rect.x, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight);
                var deleteRect = new Rect(rect.x + rect.width - 55, rect.y, 50, EditorGUIUtility.singleLineHeight);
                
                EditorGUI.LabelField(nameRect, $"{i + 1}. {atlas.atlasName}", EditorStyles.boldLabel);
                
                if (GUI.Button(deleteRect, "删除"))
                {
                    if (EditorUtility.DisplayDialog("确认删除", 
                        $"确定要删除图集 '{atlas.atlasName}' 吗？\n这将删除图集文件并取消文件夹的关联。", 
                        "删除", "取消"))
                    {
                        SpriteAtlasTool.RemoveSpriteAtlasByConfigIndex(i);
                        break; // 跳出循环，因为列表已经改变
                    }
                }
                rect.y += EditorGUIUtility.singleLineHeight + 2;
                
                // 图集信息
                EditorGUI.indentLevel++;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "文件夹路径:", atlas.folderPath);
                rect.y += EditorGUIUtility.singleLineHeight;
                
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), "图集路径:", atlas.atlasPath);
                rect.y += EditorGUIUtility.singleLineHeight;
                
                // 状态检查
                bool folderExists = System.IO.Directory.Exists(atlas.folderPath);
                bool atlasExists = System.IO.File.Exists(atlas.atlasPath);
                
                if (!folderExists || !atlasExists)
                {
                    var helpRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.HelpBox(helpRect, $"状态异常: 文件夹存在={folderExists}, 图集存在={atlasExists}", MessageType.Warning);
                }
                rect.y += EditorGUIUtility.singleLineHeight + 4;
                
                EditorGUI.indentLevel--;
            }
            
            // 清理按钮
            var cleanupRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(cleanupRect, "清理无效项"))
            {
                int cleanedCount = CleanupInvalidAtlases(atlasConfig);
                if (cleanedCount > 0)
                {
                    EditorUtility.DisplayDialog("清理完成", $"已清理 {cleanedCount} 个无效项", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("清理完成", "没有发现无效项", "确定");
                }
            }
            rect.y += EditorGUIUtility.singleLineHeight + 4;
            
            return rect;
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

        /// <summary>
        /// 获取属性值
        /// </summary>
        private T GetPropertyValue<T>(SerializedProperty property)
        {
            object obj = property.serializedObject.targetObject;
            string[] path = property.propertyPath.Split('.');
            
            for (int i = 0; i < path.Length; i++)
            {
                var field = obj.GetType().GetField(path[i], 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    obj = field.GetValue(obj);
                }
                else
                {
                    return default(T);
                }
            }
            
            return (T)obj;
        }
    }
}