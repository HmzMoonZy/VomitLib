using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;
using System;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// ModelMonitor的自定义Inspector - Model层数据监控面板
    /// 提供Model数据的实时监控和编辑功能
    /// </summary>
    [CustomEditor(typeof(ModelMonitor))]
    public class ModelMonitorInspector : UnityEditor.Editor
    {
        #region Fields

        private ModelMonitor _monitor;
        private double _lastRefreshTime;

        // 界面状态
        private bool _showSettings = false;
        private bool _showModelList = true;
        private Dictionary<Type, bool> _modelFoldoutStates = new Dictionary<Type, bool>();
        private Dictionary<string, object> _editingValues = new Dictionary<string, object>();
        private Dictionary<string, string> _editingStringValues = new Dictionary<string, string>();

        // 过滤和搜索
        private string _modelSearchFilter = "";
        private string _fieldSearchFilter = "";
        
        // Model选择
        private string[] _availableModelNames = new string[0];
        private int _selectedModelIndex = 0;

        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _fieldStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _modelHeaderStyle;

        // 颜色
        private readonly Color _modelColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);
        private readonly Color _fieldColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
        private readonly Color _editableColor = new Color(0.8f, 1f, 0.8f, 0.3f);
        private readonly Color _readOnlyColor = new Color(1f, 0.8f, 0.8f, 0.3f);

        // GUI操作相关
        private bool _needsRefresh;
        private Vector2 _modelScrollPosition;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            _monitor = (ModelMonitor)target;
            EditorApplication.delayCall += InitializeStyles;

            if (Application.isPlaying && _monitor != null && _monitor.EnableMonitoring)
            {
                EditorApplication.delayCall += () => RefreshMonitorData();
            }
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= InitializeStyles;
        }

        public override void OnInspectorGUI()
        {
            if (_headerStyle == null)
            {
                InitializeStyles();
                if (_headerStyle == null) return;
            }

            ProcessPendingOperations();

            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(5);

            DrawSettings();
            EditorGUILayout.Space(5);

            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                DrawModelMonitoring();
            }
            else
            {
                DrawPlayModeInfo();
            }

            serializedObject.ApplyModifiedProperties();

            CheckAutoRefresh();
        }

        #endregion

        #region GUI Drawing

        /// <summary>
        /// 绘制头部信息
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 Model 数据监控", _headerStyle);
            
            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                var models = _monitor.GetModelInfos();
                
                if (_monitor.MonitorAllModels)
                {
                    EditorGUILayout.LabelField($"📋 监控所有Model: {models.Count}个", _subHeaderStyle);
                }
                else if (!string.IsNullOrEmpty(_monitor.SpecificModelTypeName))
                {
                    var filteredCount = models.Count(m => m.Name.Contains(_monitor.SpecificModelTypeName));
                    EditorGUILayout.LabelField($"🎯 监控特定Model: {_monitor.SpecificModelTypeName} ({filteredCount}个)", _subHeaderStyle);
                }
                else
                {
                    EditorGUILayout.LabelField($"监控中的Model: {models.Count}个", _subHeaderStyle);
                }

                // 显示快速选择按钮
                if (models.Count > 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("快速选择:", GUILayout.Width(60));
                    
                    foreach (var model in models.Take(3)) // 只显示前3个
                    {
                        if (GUILayout.Button(model.Name, _buttonStyle, GUILayout.MaxWidth(80)))
                        {
                            _monitor.MonitorAllModels = false;
                            _monitor.SetSpecificModel(model.Name);
                            // 同步UI状态
                            var index = Array.IndexOf(_availableModelNames, model.Name);
                            if (index > 0) _selectedModelIndex = index;
                            _needsRefresh = true;
                        }
                    }
                    
                    if (models.Count > 3)
                    {
                        EditorGUILayout.LabelField($"...({models.Count - 3}更多)", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("请在运行时启用监控查看Model数据", _subHeaderStyle);
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制设置面板
        /// </summary>
        private void DrawSettings()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚙️ 监控设置", _subHeaderStyle);
            _showSettings = EditorGUILayout.Foldout(_showSettings, "", true);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("🔄 刷新", _buttonStyle, GUILayout.Width(60)))
            {
                _needsRefresh = true;
            }
            
            if (GUILayout.Button("🗑️ 清空缓存", _buttonStyle, GUILayout.Width(80)))
            {
                _monitor.ClearCache();
                _needsRefresh = true;
            }
            
            EditorGUILayout.EndHorizontal();

            if (_showSettings)
            {
                DrawDetailedSettings();
            }
        }

        /// <summary>
        /// 绘制详细设置
        /// </summary>
        private void DrawDetailedSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 基本设置
            var enableProp = serializedObject.FindProperty("_enableMonitoring");
            var autoRefreshProp = serializedObject.FindProperty("_autoRefresh");
            var refreshIntervalProp = serializedObject.FindProperty("_refreshInterval");

            EditorGUILayout.PropertyField(enableProp, new GUIContent("启用监控"));
            EditorGUILayout.PropertyField(autoRefreshProp, new GUIContent("自动刷新"));
            
            if (autoRefreshProp.boolValue)
            {
                EditorGUILayout.PropertyField(refreshIntervalProp, new GUIContent("刷新间隔(秒)"));
            }

            EditorGUILayout.Space(3);

            // 性能设置
            EditorGUILayout.LabelField("性能优化", EditorStyles.boldLabel);
            
            var monitorAllProp = serializedObject.FindProperty("_monitorAllModels");
            var specificModelProp = serializedObject.FindProperty("_specificModelTypeName");

            EditorGUILayout.PropertyField(monitorAllProp, new GUIContent("监控所有Model"));
            
            if (!monitorAllProp.boolValue)
            {
                DrawModelSelector(specificModelProp);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制Model选择器
        /// </summary>
        private void DrawModelSelector(SerializedProperty specificModelProp)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎯 Model选择器", EditorStyles.boldLabel);

            // 更新可用Model列表
            UpdateAvailableModelNames();

            if (_availableModelNames.Length == 0)
            {
                EditorGUILayout.HelpBox("没有检测到Model，请确保游戏运行中且架构已初始化", MessageType.Info);
                EditorGUILayout.PropertyField(specificModelProp, new GUIContent("手动输入Model类型名"));
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("选择Model:", GUILayout.Width(80));

                // 下拉选择
                var newIndex = EditorGUILayout.Popup(_selectedModelIndex, _availableModelNames);
                if (newIndex != _selectedModelIndex)
                {
                    _selectedModelIndex = newIndex;
                    if (_selectedModelIndex > 0 && _selectedModelIndex <= _availableModelNames.Length)
                    {
                        var selectedModelName = _availableModelNames[_selectedModelIndex];
                        specificModelProp.stringValue = selectedModelName;
                        _monitor.SetSpecificModel(selectedModelName);
                        serializedObject.ApplyModifiedProperties();
                        _needsRefresh = true;
                    }
                }

                EditorGUILayout.EndHorizontal();

                // 快速操作按钮
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("📋 全部显示", _buttonStyle))
                {
                    _selectedModelIndex = 0;
                    specificModelProp.stringValue = "";
                    _monitor.MonitorAllModels = true;
                    serializedObject.ApplyModifiedProperties();
                    _needsRefresh = true;
                }

                if (_selectedModelIndex > 0 && GUILayout.Button("🎯 只看这个", _buttonStyle))
                {
                    var selectedModelName = _availableModelNames[_selectedModelIndex];
                    specificModelProp.stringValue = selectedModelName;
                    _monitor.MonitorAllModels = false;
                    _monitor.SetSpecificModel(selectedModelName);
                    serializedObject.ApplyModifiedProperties();
                    _needsRefresh = true;
                }

                EditorGUILayout.EndHorizontal();

                // 显示当前选择的详细信息
                if (_selectedModelIndex > 0)
                {
                    var selectedModelName = _availableModelNames[_selectedModelIndex];
                    var modelInfo = GetModelInfoByName(selectedModelName);
                    if (modelInfo != null)
                    {
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField($"类型: {modelInfo.FullTypeName}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"字段数: {modelInfo.FieldCount}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"状态: {(modelInfo.IsInitialized ? "✅ 已初始化" : "❌ 未初始化")}", EditorStyles.miniLabel);
                    }
                }

                // 手动输入选项
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("或手动输入:", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(specificModelProp, GUIContent.none);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制Model监控面板
        /// </summary>
        private void DrawModelMonitoring()
        {
            // 搜索过滤
            DrawSearchFilters();

            // Model列表
            _showModelList = EditorGUILayout.Foldout(_showModelList, $"📦 Model列表", true);
            if (_showModelList)
            {
                DrawModelList();
            }
        }

        /// <summary>
        /// 绘制搜索过滤器
        /// </summary>
        private void DrawSearchFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("🔍 搜索过滤", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Model:", GUILayout.Width(50));
            _modelSearchFilter = EditorGUILayout.TextField(_modelSearchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("字段:", GUILayout.Width(50));
            _fieldSearchFilter = EditorGUILayout.TextField(_fieldSearchFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制Model列表
        /// </summary>
        private void DrawModelList()
        {
            var models = _monitor.GetModelInfos();
            
            if (models.Count == 0)
            {
                EditorGUILayout.HelpBox("没有检测到Model，请确保架构已初始化", MessageType.Info);
                return;
            }

            // 过滤Models
            var filteredModels = string.IsNullOrEmpty(_modelSearchFilter)
                ? models
                : models.Where(m => m.Name.ToLower().Contains(_modelSearchFilter.ToLower())).ToList();

            _modelScrollPosition = EditorGUILayout.BeginScrollView(_modelScrollPosition);

            foreach (var model in filteredModels)
            {
                DrawModelInfo(model);
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单个Model信息
        /// </summary>
        private void DrawModelInfo(ModelInfo model)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = _modelColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // Model头部
            EditorGUILayout.BeginHorizontal();
            
            if (!_modelFoldoutStates.ContainsKey(model.Type))
                _modelFoldoutStates[model.Type] = false;
                
            _modelFoldoutStates[model.Type] = EditorGUILayout.Foldout(_modelFoldoutStates[model.Type], 
                $"📦 {model.Name}", true, _modelHeaderStyle);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(model.IsInitialized ? "✅" : "❌", GUILayout.Width(20));
            EditorGUILayout.LabelField($"({model.FieldCount})", GUILayout.Width(30));
            
            EditorGUILayout.EndHorizontal();

            // Model详情
            if (_modelFoldoutStates[model.Type])
            {
                EditorGUILayout.LabelField($"类型: {model.FullTypeName}", _fieldStyle);
                EditorGUILayout.Space(3);

                DrawModelFields(model);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制Model字段
        /// </summary>
        private void DrawModelFields(ModelInfo model)
        {
            var fields = _monitor.GetModelFields(model.Type);
            
            // 过滤字段
            var filteredFields = string.IsNullOrEmpty(_fieldSearchFilter)
                ? fields
                : fields.Where(f => f.Name.ToLower().Contains(_fieldSearchFilter.ToLower())).ToList();

            foreach (var field in filteredFields)
            {
                DrawFieldEditor(model, field);
            }
        }

        /// <summary>
        /// 绘制字段编辑器
        /// </summary>
        private void DrawFieldEditor(ModelInfo model, FieldPropertyInfo field)
        {
            var fieldKey = $"{model.Type.FullName}.{field.Name}";
            var currentValue = _monitor.GetModelFieldValue(model.Type, field.Name);

            var bgColor = field.CanWrite ? _editableColor : _readOnlyColor;
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            EditorGUILayout.BeginHorizontal();

            // 字段名和类型
            var fieldIcon = field.IsProperty ? "🔧" : "📋";
            var accessIcon = field.CanWrite ? "✏️" : "👁️";
            EditorGUILayout.LabelField($"{fieldIcon}{accessIcon} {field.Name}", _fieldStyle, GUILayout.Width(150));
            EditorGUILayout.LabelField($"({GetSimpleTypeName(field.Type)})", _valueStyle, GUILayout.Width(80));

            // 值编辑器
            if (field.CanRead)
            {
                var newValue = DrawValueEditor(fieldKey, field.Type, currentValue, field.CanWrite);
                
                if (field.CanWrite && newValue != null && !Equals(newValue, currentValue))
                {
                    if (_monitor.SetModelFieldValue(model.Type, field.Name, newValue))
                    {
                        EditorUtility.SetDirty(_monitor);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("(不可读)", _valueStyle);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制值编辑器
        /// </summary>
        private object DrawValueEditor(string fieldKey, Type fieldType, object currentValue, bool canWrite)
        {
            if (!canWrite)
            {
                EditorGUILayout.LabelField(GetValueString(currentValue), _valueStyle);
                return currentValue;
            }

            // 根据类型创建不同的编辑器
            if (fieldType == typeof(int))
            {
                return EditorGUILayout.IntField(currentValue as int? ?? 0);
            }
            else if (fieldType == typeof(float))
            {
                return EditorGUILayout.FloatField(currentValue as float? ?? 0f);
            }
            else if (fieldType == typeof(double))
            {
                return EditorGUILayout.DoubleField(currentValue as double? ?? 0.0);
            }
            else if (fieldType == typeof(bool))
            {
                return EditorGUILayout.Toggle(currentValue as bool? ?? false);
            }
            else if (fieldType == typeof(string))
            {
                return EditorGUILayout.TextField(currentValue as string ?? "");
            }
            else if (fieldType == typeof(Vector2))
            {
                return EditorGUILayout.Vector2Field("", (Vector2)(currentValue ?? Vector2.zero));
            }
            else if (fieldType == typeof(Vector3))
            {
                return EditorGUILayout.Vector3Field("", (Vector3)(currentValue ?? Vector3.zero));
            }
            else if (fieldType == typeof(Color))
            {
                return EditorGUILayout.ColorField((Color)(currentValue ?? Color.white));
            }
            else if (fieldType.IsEnum)
            {
                return EditorGUILayout.EnumPopup((Enum)(currentValue ?? Enum.GetValues(fieldType).GetValue(0)));
            }
            else
            {
                // 复杂类型只显示，不可编辑
                EditorGUILayout.LabelField(GetValueString(currentValue), _valueStyle);
                return currentValue;
            }
        }

        /// <summary>
        /// 绘制非运行时提示
        /// </summary>
        private void DrawPlayModeInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📦 Model数据监控面板", _headerStyle);
            EditorGUILayout.LabelField("请在运行时启用监控查看Model数据状态", _subHeaderStyle);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 获取简化的类型名
        /// </summary>
        private string GetSimpleTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(Color)) return "Color";
            if (type.IsEnum) return "enum";
            
            return type.Name;
        }

        /// <summary>
        /// 获取值的字符串表示
        /// </summary>
        private string GetValueString(object value)
        {
            if (value == null) return "null";
            if (value is string str) return $"\"{str}\"";
            return value.ToString();
        }

        /// <summary>
        /// 处理待执行操作
        /// </summary>
        private void ProcessPendingOperations()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                EditorApplication.delayCall += () =>
                {
                    RefreshMonitorData();
                    Repaint();
                };
            }
        }

        /// <summary>
        /// 检查自动刷新
        /// </summary>
        private void CheckAutoRefresh()
        {
            if (_monitor.AutoRefresh && _monitor.EnableMonitoring && Application.isPlaying &&
                EditorApplication.timeSinceStartup - _lastRefreshTime > _monitor.RefreshInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                EditorApplication.delayCall += () =>
                {
                    RefreshMonitorData();
                    Repaint();
                };
            }
        }

        /// <summary>
        /// 刷新监控数据
        /// </summary>
        private void RefreshMonitorData()
        {
            if (_monitor != null && _monitor.EnableMonitoring)
            {
                _monitor.RefreshModelData();
            }
        }

        /// <summary>
        /// 更新可用Model名称列表
        /// </summary>
        private void UpdateAvailableModelNames()
        {
            if (!Application.isPlaying || _monitor == null || !_monitor.EnableMonitoring)
            {
                _availableModelNames = new string[0];
                return;
            }

            var modelInfos = _monitor.GetModelInfos();
            var modelNames = new List<string> { "-- 选择Model --" };
            
            foreach (var model in modelInfos)
            {
                modelNames.Add(model.Name);
            }

            _availableModelNames = modelNames.ToArray();

            // 保持当前选择状态
            if (!string.IsNullOrEmpty(_monitor.SpecificModelTypeName))
            {
                for (int i = 1; i < _availableModelNames.Length; i++)
                {
                    if (_availableModelNames[i].Contains(_monitor.SpecificModelTypeName))
                    {
                        _selectedModelIndex = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 根据名称获取Model信息
        /// </summary>
        private ModelInfo GetModelInfoByName(string modelName)
        {
            if (_monitor == null) return null;
            
            var modelInfos = _monitor.GetModelInfos();
            return modelInfos.FirstOrDefault(m => m.Name == modelName);
        }

        /// <summary>
        /// 初始化样式
        /// </summary>
        private void InitializeStyles()
        {
            try
            {
                if (EditorStyles.boldLabel == null) return;

                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };

                _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.9f, 1f) : new Color(0.2f, 0.3f, 0.6f) }
                };

                _fieldStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
                };

                _valueStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black }
                };

                _buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 10
                };

                _modelHeaderStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ModelMonitorInspector] 初始化样式失败: {e.Message}");
            }
        }

        #endregion
    }
}