using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// Model监控标签页 - 监控和修改Model层数据
    /// </summary>
    public class ModelMonitorTab
    {
        #region Fields

        // 过滤选项
        private string _searchFilter = "";
        private bool _showInitializedOnly = false;

        // UI状态
        private Vector2 _scrollPosition;
        private Dictionary<Type, bool> _modelFoldoutStates = new Dictionary<Type, bool>();
        private Dictionary<string, bool> _fieldFoldoutStates = new Dictionary<string, bool>();

        // 编辑状态
        private Dictionary<string, object> _editValues = new Dictionary<string, object>();
        private Dictionary<string, bool> _editModeStates = new Dictionary<string, bool>();

        // 选中的Model
        private Type _selectedModelType = null;

        // 样式
        private GUIStyle _modelHeaderStyle;
        private GUIStyle _fieldNameStyle;
        private GUIStyle _fieldTypeStyle;
        private GUIStyle _fieldValueStyle;
        private GUIStyle _readonlyStyle;

        #endregion

        #region Public Methods

        public void OnGUI(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            EnsureStylesInitialized();

            // 绘制头部
            DrawHeader(window, provider);

            EditorGUILayout.Space(5);

            // 绘制过滤选项
            DrawFilterOptions(window);

            EditorGUILayout.Space(5);

            // 绘制Model列表
            DrawModelList(window, provider);
        }

        #endregion

        #region Drawing Methods

        private void DrawHeader(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var modelInfos = provider.ModelInfos;
            var initializedCount = modelInfos.Count(m => m.IsInitialized);
            var totalCount = modelInfos.Count;

            var rect = EditorGUILayout.GetControlRect(false, 50);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.8f, 0.4f, 0.2f));

            var headerRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 40);
            EditorGUI.LabelField(headerRect, "Model数据监控", window._headerStyle);

            var statsRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 20);
            var statsText = $"总计: {totalCount} | 已初始化: {initializedCount}";
            EditorGUI.LabelField(statsRect, statsText, window._infoStyle);
        }

        private void DrawFilterOptions(VomitMonitorWindow window)
        {
            var rect = EditorGUILayout.GetControlRect(false, 25);
            EditorGUI.DrawRect(rect, new Color(0.9f, 0.9f, 0.9f, 0.2f));

            var filterRect = new Rect(rect.x + 5, rect.y + 2, rect.width - 10, 20);

            // 搜索框
            var searchRect = new Rect(filterRect.x, filterRect.y, 200, 20);
            _searchFilter = EditorGUI.TextField(searchRect, "搜索:", _searchFilter);

            // 过滤选项
            var toggleRect = new Rect(searchRect.xMax + 20, filterRect.y, 150, 20);
            _showInitializedOnly = EditorGUI.Toggle(toggleRect, "仅显示已初始化", _showInitializedOnly);
        }

        private void DrawModelList(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var modelInfos = provider.ModelInfos;

            // 应用过滤
            var filteredModels = modelInfos.Where(m =>
            {
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !(m.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    !(m.FullTypeName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return false;
                }

                if (_showInitializedOnly && !m.IsInitialized) return false;

                return true;
            }).OrderBy(m => m.Name).ToList();

            if (filteredModels.Count == 0)
            {
                var rect = EditorGUILayout.GetControlRect(false, 60);
                EditorGUI.DrawRect(rect, new Color(0.95f, 0.95f, 0.95f, 1f));
                var messageRect = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, 40);
                EditorGUI.LabelField(messageRect, "没有找到匹配的Model\n调整过滤条件或搜索关键词", window._infoStyle);
                return;
            }

            // 分割视图
            EditorGUILayout.BeginHorizontal();

            // 左侧：Model列表
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            DrawModelListPanel(window, filteredModels);
            EditorGUILayout.EndVertical();

            // 分隔线
            var separatorRect = EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(2));
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            // 右侧：字段详情
            EditorGUILayout.BeginVertical();
            if (_selectedModelType != null)
            {
                DrawModelFields(window, provider, _selectedModelType);
            }
            else
            {
                DrawSelectModelPrompt(window);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawModelListPanel(VomitMonitorWindow window, List<ModelInfo> filteredModels)
        {
            var listRect = EditorGUILayout.GetControlRect(false, 300);
            var listScrollPos = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));

            foreach (var modelInfo in filteredModels)
            {
                DrawModelListItem(window, modelInfo);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawModelListItem(VomitMonitorWindow window, ModelInfo modelInfo)
        {
            if (!_modelFoldoutStates.ContainsKey(modelInfo.Type))
            {
                _modelFoldoutStates[modelInfo.Type] = false;
            }

            var isSelected = _selectedModelType == modelInfo.Type;
            var bgColor = isSelected ? new Color(0.3f, 0.6f, 1f, 0.3f) : new Color(0.9f, 0.9f, 0.9f, 0.1f);

            var rect = EditorGUILayout.GetControlRect(false, 22);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            // 选择按钮区域
            var selectRect = new Rect(rect.x, rect.y, rect.width, rect.height);

            if (GUI.Button(selectRect, "", GUIStyle.none))
            {
                _selectedModelType = modelInfo.Type;
            }

            // Model名称
            var nameRect = new Rect(rect.x + 5, rect.y + 2, 180, 18);
            var labelStyle = isSelected ? _modelHeaderStyle : window._infoStyle;
            EditorGUI.LabelField(nameRect, modelInfo.Name, labelStyle);

            // 初始化状态
            var statusRect = new Rect(nameRect.xMax + 10, rect.y + 2, 50, 18);
            var statusStyle = modelInfo.IsInitialized ? window._successStyle : window._warningStyle;
            var statusText = modelInfo.IsInitialized ? "✓" : "○";
            EditorGUI.LabelField(statusRect, statusText, statusStyle);
        }

        private void DrawSelectModelPrompt(VomitMonitorWindow window)
        {
            EditorGUILayout.BeginVertical(window._boxStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("选择一个Model查看详细信息", window._infoStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawModelFields(VomitMonitorWindow window, MonitorDataProvider provider, Type modelType)
        {
            var modelInfo = provider.ModelInfos.FirstOrDefault(m => m.Type == modelType);
            if (modelInfo == null) return;

            var scrollPos = EditorGUILayout.BeginScrollView(Vector2.zero);

            EditorGUILayout.BeginVertical(window._boxStyle);
            EditorGUILayout.LabelField($"Model: {modelInfo.Name}", window._headerStyle);
            EditorGUILayout.LabelField($"类型: {modelInfo.FullTypeName}", window._infoStyle);
            EditorGUILayout.LabelField($"状态: {(modelInfo.IsInitialized ? "已初始化" : "未初始化")}",
                modelInfo.IsInitialized ? window._successStyle : window._warningStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 获取字段列表
            var fields = GetModelFields(modelType);

            if (fields.Count == 0)
            {
                EditorGUILayout.HelpBox("该Model没有可显示的字段", MessageType.Info);
            }
            else
            {
                // 字段列表头部
                DrawFieldHeader();

                foreach (var fieldInfo in fields)
                {
                    DrawFieldItem(window, provider, modelType, fieldInfo);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFieldHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("字段名", _fieldNameStyle, GUILayout.Width(150));
            GUILayout.Label("类型", _fieldTypeStyle, GUILayout.Width(120));
            GUILayout.Label("值", _fieldValueStyle);
            GUILayout.Label("操作", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFieldItem(VomitMonitorWindow window, MonitorDataProvider provider, Type modelType, FieldPropertyInfo fieldInfo)
        {
            var fieldKey = $"{modelType.Name}.{fieldInfo.Name}";

            // 背景色
            var bgColor = fieldInfo.IsProperty ? new Color(0.95f, 0.9f, 1f, 0.3f) : new Color(0.95f, 0.95f, 0.95f, 0.3f);
            var rect = EditorGUILayout.GetControlRect(false, 22);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            EditorGUILayout.BeginHorizontal();

            // 字段名
            GUILayout.Label(fieldInfo.Name, _fieldNameStyle, GUILayout.Width(150));

            // 类型
            var typeLabel = GetSimpleTypeName(fieldInfo.Type);
            GUILayout.Label(typeLabel, _fieldTypeStyle, GUILayout.Width(120));

            // 值
            var currentValue = provider.GetModelFieldValue(modelType, fieldInfo.Name);
            var valueText = currentValue?.ToString() ?? "null";

            if (fieldInfo.CanWrite)
            {
                // 可编辑
                var newValue = EditorGUILayout.TextField(valueText, GUILayout.Width(150));
                if (newValue != valueText && GUILayout.Button("应用", EditorStyles.miniButton, GUILayout.Width(40)))
                {
                    ApplyFieldValue(provider, modelType, fieldInfo, newValue);
                }
            }
            else
            {
                // 只读
                GUILayout.Label(valueText, _readonlyStyle);
                GUILayout.Space(90);
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Helper Methods

        private List<FieldPropertyInfo> GetModelFields(Type modelType)
        {
            var fields = new List<FieldPropertyInfo>();

            // 获取字段
            var fieldInfos = modelType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fieldInfos)
            {
                if (field.IsStatic || field.Name.StartsWith("<") || field.Name.StartsWith("m_")) continue;

                fields.Add(new FieldPropertyInfo
                {
                    Name = field.Name,
                    Type = field.FieldType,
                    IsProperty = false,
                    CanRead = true,
                    CanWrite = !field.IsInitOnly,
                    IsPublic = field.IsPublic
                });
            }

            // 获取属性
            var propertyInfos = modelType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in propertyInfos)
            {
                if (property.GetIndexParameters().Length > 0) continue;
                if (property.Name == "Initialized") continue; // 跳过框架属性

                fields.Add(new FieldPropertyInfo
                {
                    Name = property.Name,
                    Type = property.PropertyType,
                    IsProperty = true,
                    CanRead = property.CanRead,
                    CanWrite = property.CanWrite,
                    IsPublic = property.GetGetMethod()?.IsPublic == true || property.GetSetMethod()?.IsPublic == true
                });
            }

            return fields.OrderBy(f => f.IsProperty).ThenBy(f => f.Name).ToList();
        }

        private void ApplyFieldValue(MonitorDataProvider provider, Type modelType, FieldPropertyInfo fieldInfo, string newValue)
        {
            try
            {
                object convertedValue = null;

                if (fieldInfo.Type == typeof(string))
                {
                    convertedValue = newValue;
                }
                else if (fieldInfo.Type == typeof(int))
                {
                    convertedValue = int.Parse(newValue);
                }
                else if (fieldInfo.Type == typeof(float))
                {
                    convertedValue = float.Parse(newValue);
                }
                else if (fieldInfo.Type == typeof(double))
                {
                    convertedValue = double.Parse(newValue);
                }
                else if (fieldInfo.Type == typeof(bool))
                {
                    convertedValue = bool.Parse(newValue);
                }
                else
                {
                    Debug.LogWarning($"[ModelMonitorTab] 不支持的类型转换: {fieldInfo.Type.Name}");
                    return;
                }

                provider.SetModelFieldValue(modelType, fieldInfo.Name, convertedValue);
                Debug.Log($"[ModelMonitorTab] 成功设置 {modelType.Name}.{fieldInfo.Name} = {convertedValue}");
            }
            catch (FormatException)
            {
                Debug.LogError($"[ModelMonitorTab] 格式错误: 无法将 '{newValue}' 转换为 {fieldInfo.Type.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelMonitorTab] 设置字段值失败: {e.Message}");
            }
        }

        private string GetSimpleTypeName(Type type)
        {
            if (type == null) return "null";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type.IsGenericType) return type.Name.Split('`')[0] + "<>";
            return type.Name;
        }

        private void EnsureStylesInitialized()
        {
            if (_modelHeaderStyle != null) return;

            // 统一的颜色变量
            var lightTextColor = new Color(0.95f, 0.95f, 0.95f);
            var mediumTextColor = new Color(0.75f, 0.75f, 0.75f);
            var successColor = new Color(0.4f, 1f, 0.6f);

            _modelHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = lightTextColor },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _fieldNameStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = lightTextColor },
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(5, 5, 2, 2)
            };

            _fieldTypeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.6f, 0.7f, 1f) },
                padding = new RectOffset(5, 5, 2, 2)
            };

            _fieldValueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = successColor },
                wordWrap = false,
                padding = new RectOffset(5, 5, 2, 2)
            };

            _readonlyStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = mediumTextColor },
                fontStyle = FontStyle.Italic,
                wordWrap = false,
                padding = new RectOffset(5, 5, 2, 2)
            };
        }

        #endregion
    }
}
