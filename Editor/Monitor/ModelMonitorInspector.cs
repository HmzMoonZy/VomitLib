using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

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
                if (IsComplexType(fieldType))
                {
                    DrawComplexTypeDisplay(currentValue, fieldType, false);
                }
                else
                {
                    EditorGUILayout.LabelField(GetValueString(currentValue), _valueStyle);
                }
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
            else if (IsComplexType(fieldType))
            {
                // 复杂类型展开显示和编辑
                return DrawComplexTypeEditor(fieldKey, currentValue, fieldType);
            }
            else
            {
                // 其他类型只显示，不可编辑
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
        /// 判断是否为复杂类型（可展开编辑的自定义类）
        /// </summary>
        private bool IsComplexType(Type type)
        {
            // 基础类型和Unity内置类型不算复杂类型
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return false;
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Color)) return false;
            if (type == typeof(Quaternion) || type == typeof(Vector4)) return false;
            
            // 集合类型特殊处理
            if (IsCollectionType(type)) return true;
            
            // 系统类型不算复杂类型
            if (type.Namespace?.StartsWith("System") == true) return false;
            if (type.Namespace?.StartsWith("UnityEngine") == true) return false;
            if (type.Namespace?.StartsWith("QFramework") == true) return false;
            
            // 自定义类型算复杂类型
            return type.IsClass && !type.IsAbstract;
        }

        /// <summary>
        /// 判断是否为集合类型
        /// </summary>
        private bool IsCollectionType(Type type)
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                return genericType == typeof(List<>) || 
                       genericType == typeof(Dictionary<,>) ||
                       genericType == typeof(HashSet<>) ||
                       genericType == typeof(Queue<>) ||
                       genericType == typeof(Stack<>);
            }
            
            return type.IsArray;
        }

        /// <summary>
        /// 绘制复杂类型编辑器
        /// </summary>
        private object DrawComplexTypeEditor(string fieldKey, object currentValue, Type fieldType)
        {
            // 集合类型特殊处理
            if (IsCollectionType(fieldType))
            {
                return DrawCollectionEditor(fieldKey, currentValue, fieldType);
            }

            if (currentValue == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("null", _valueStyle);
                if (GUILayout.Button("创建", _buttonStyle, GUILayout.Width(40)))
                {
                    currentValue = Activator.CreateInstance(fieldType);
                }
                EditorGUILayout.EndHorizontal();
                return currentValue;
            }

            // 获取或创建折叠状态
            var foldoutKey = $"{fieldKey}_foldout";
            if (!_editingValues.ContainsKey(foldoutKey))
                _editingValues[foldoutKey] = false;

            var foldout = (bool)_editingValues[foldoutKey];
            
            EditorGUILayout.BeginVertical();
            
            // 类型头部（可折叠）
            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, $"{fieldType.Name}", true);
            _editingValues[foldoutKey] = foldout;
            
            if (GUILayout.Button("删除", _buttonStyle, GUILayout.Width(40)))
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return null;
            }
            EditorGUILayout.EndHorizontal();

            // 展开显示字段
            if (foldout)
            {
                EditorGUI.indentLevel++;
                var fields = fieldType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    if (field.IsStatic) continue;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{field.Name}", _fieldStyle, GUILayout.Width(120));
                    
                    var fieldValue = field.GetValue(currentValue);
                    var newValue = DrawValueEditor($"{fieldKey}.{field.Name}", field.FieldType, fieldValue, true);
                    
                    if (!Equals(newValue, fieldValue))
                    {
                        field.SetValue(currentValue, newValue);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            return currentValue;
        }

        /// <summary>
        /// 绘制集合类型编辑器
        /// </summary>
        private object DrawCollectionEditor(string fieldKey, object currentValue, Type fieldType)
        {
            if (currentValue == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("null", _valueStyle);
                if (GUILayout.Button("创建", _buttonStyle, GUILayout.Width(40)))
                {
                    currentValue = Activator.CreateInstance(fieldType);
                }
                EditorGUILayout.EndHorizontal();
                return currentValue;
            }

            // 获取折叠状态
            var foldoutKey = $"{fieldKey}_collection_foldout";
            if (!_editingValues.ContainsKey(foldoutKey))
                _editingValues[foldoutKey] = false;

            var foldout = (bool)_editingValues[foldoutKey];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 集合头部信息
            EditorGUILayout.BeginHorizontal();
            
            if (fieldType.IsGenericType)
            {
                var genericType = fieldType.GetGenericTypeDefinition();
                string collectionIcon = "📋";
                if (genericType == typeof(List<>)) collectionIcon = "📋";
                else if (genericType == typeof(Dictionary<,>)) collectionIcon = "📚";
                else if (genericType == typeof(HashSet<>)) collectionIcon = "🎯";
                else if (genericType == typeof(Queue<>)) collectionIcon = "⏭️";
                else if (genericType == typeof(Stack<>)) collectionIcon = "📚";

                var count = GetCollectionCount(currentValue);
                foldout = EditorGUILayout.Foldout(foldout, $"{collectionIcon} {GetCollectionTypeName(fieldType)} [{count}]", true);
                _editingValues[foldoutKey] = foldout;
            }
            else if (fieldType.IsArray)
            {
                var array = currentValue as Array;
                foldout = EditorGUILayout.Foldout(foldout, $"📋 {fieldType.GetElementType().Name}[] [{array?.Length ?? 0}]", true);
                _editingValues[foldoutKey] = foldout;
            }

            GUILayout.FlexibleSpace();

            // 操作按钮
            if (GUILayout.Button("删除", _buttonStyle, GUILayout.Width(40)))
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return null;
            }

            EditorGUILayout.EndHorizontal();

            // 展开显示内容
            if (foldout)
            {
                EditorGUI.indentLevel++;
                
                if (fieldType.IsGenericType)
                {
                    var genericType = fieldType.GetGenericTypeDefinition();
                    
                    if (genericType == typeof(List<>))
                    {
                        DrawListEditor(fieldKey, currentValue, fieldType);
                    }
                    else if (genericType == typeof(Dictionary<,>))
                    {
                        DrawDictionaryEditor(fieldKey, currentValue, fieldType);
                    }
                    else
                    {
                        DrawGenericCollectionDisplay(currentValue, fieldType);
                    }
                }
                else if (fieldType.IsArray)
                {
                    DrawArrayEditor(fieldKey, currentValue, fieldType);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return currentValue;
        }

        /// <summary>
        /// 绘制List编辑器
        /// </summary>
        private void DrawListEditor(string fieldKey, object listObj, Type listType)
        {
            var elementType = listType.GetGenericArguments()[0];
            var list = listObj as System.Collections.IList;
            
            if (list == null) return;

            // 添加新元素按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"元素类型: {GetSimpleTypeName(elementType)}", _fieldStyle);
            if (GUILayout.Button("添加", _buttonStyle, GUILayout.Width(40)))
            {
                var defaultValue = GetDefaultValue(elementType);
                list.Add(defaultValue);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 显示列表元素
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                
                var currentValue = list[i];
                var newValue = DrawValueEditor($"{fieldKey}[{i}]", elementType, currentValue, true);
                
                if (!Equals(newValue, currentValue))
                {
                    list[i] = newValue;
                }
                
                if (GUILayout.Button("删除", _buttonStyle, GUILayout.Width(40)))
                {
                    list.RemoveAt(i);
                    break; // 避免索引越界
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 绘制Dictionary编辑器
        /// </summary>
        private void DrawDictionaryEditor(string fieldKey, object dictObj, Type dictType)
        {
            var genericArgs = dictType.GetGenericArguments();
            var keyType = genericArgs[0];
            var valueType = genericArgs[1];
            
            var dict = dictObj as System.Collections.IDictionary;
            if (dict == null) return;

            // 新键值对输入区域
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"添加新项 - Key: {GetSimpleTypeName(keyType)}, Value: {GetSimpleTypeName(valueType)}", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // Key输入
            var newKeyInputKey = $"{fieldKey}_newKey";
            if (!_editingStringValues.ContainsKey(newKeyInputKey))
                _editingStringValues[newKeyInputKey] = "";
            
            EditorGUILayout.LabelField("Key:", GUILayout.Width(30));
            _editingStringValues[newKeyInputKey] = EditorGUILayout.TextField(_editingStringValues[newKeyInputKey], GUILayout.Width(80));
            
            // Value输入
            var newValueInputKey = $"{fieldKey}_newValue";
            if (!_editingValues.ContainsKey(newValueInputKey))
                _editingValues[newValueInputKey] = GetDefaultValue(valueType);
            
            EditorGUILayout.LabelField("Value:", GUILayout.Width(40));
            var newValueObj = DrawValueEditor(newValueInputKey, valueType, _editingValues[newValueInputKey], true);
            _editingValues[newValueInputKey] = newValueObj;
            
            // 添加按钮
            if (GUILayout.Button("添加", _buttonStyle, GUILayout.Width(40)))
            {
                var keyString = _editingStringValues[newKeyInputKey];
                if (!string.IsNullOrEmpty(keyString))
                {
                    try
                    {
                        // 转换Key到正确的类型
                        object convertedKey = ConvertStringToType(keyString, keyType);
                        
                        if (convertedKey != null && !dict.Contains(convertedKey))
                        {
                            dict.Add(convertedKey, newValueObj);
                            _editingStringValues[newKeyInputKey] = ""; // 清空输入
                            _editingValues[newValueInputKey] = GetDefaultValue(valueType);
                        }
                        else if (dict.Contains(convertedKey))
                        {
                            Debug.LogWarning($"Key '{keyString}' 已存在于Dictionary中");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"无法将 '{keyString}' 转换为类型 {keyType.Name}: {e.Message}");
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(2);

            // 显示现有字典元素
            var keysToRemove = new List<object>();
            var keysToUpdate = new Dictionary<object, object>();
            
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                
                // Key编辑
                EditorGUILayout.LabelField("Key:", GUILayout.Width(30));
                var keyEditKey = $"{fieldKey}_editKey_{entry.Key}";
                if (!_editingStringValues.ContainsKey(keyEditKey))
                    _editingStringValues[keyEditKey] = GetValueString(entry.Key);
                
                var editedKeyString = EditorGUILayout.TextField(_editingStringValues[keyEditKey], GUILayout.Width(80));
                _editingStringValues[keyEditKey] = editedKeyString;
                
                // Key更新按钮
                if (GUILayout.Button("更新Key", _buttonStyle, GUILayout.Width(60)))
                {
                    try
                    {
                        var convertedKey = ConvertStringToType(editedKeyString, keyType);
                        if (convertedKey != null && !Equals(convertedKey, entry.Key) && !dict.Contains(convertedKey))
                        {
                            keysToUpdate[entry.Key] = convertedKey;
                        }
                        else if (dict.Contains(convertedKey))
                        {
                            Debug.LogWarning($"Key '{editedKeyString}' 已存在于Dictionary中");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"无法将 '{editedKeyString}' 转换为类型 {keyType.Name}: {e.Message}");
                    }
                }
                
                // Value编辑
                EditorGUILayout.LabelField("Value:", GUILayout.Width(40));
                var newValue = DrawValueEditor($"{fieldKey}[{entry.Key}]", valueType, entry.Value, true);
                
                if (!Equals(newValue, entry.Value))
                {
                    dict[entry.Key] = newValue;
                }
                
                if (GUILayout.Button("删除", _buttonStyle, GUILayout.Width(40)))
                {
                    keysToRemove.Add(entry.Key);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            
            // 应用Key更新
            foreach (var kvp in keysToUpdate)
            {
                var oldKey = kvp.Key;
                var updatedKey = kvp.Value;
                var value = dict[oldKey];
                dict.Remove(oldKey);
                dict.Add(updatedKey, value);
                
                // 更新编辑状态
                var oldKeyEditKey = $"{fieldKey}_editKey_{oldKey}";
                var newKeyEditKey = $"{fieldKey}_editKey_{updatedKey}";
                if (_editingStringValues.ContainsKey(oldKeyEditKey))
                {
                    _editingStringValues.Remove(oldKeyEditKey);
                    _editingStringValues[newKeyEditKey] = GetValueString(updatedKey);
                }
            }
            
            // 删除标记的键
            foreach (var key in keysToRemove)
            {
                dict.Remove(key);
                var keyEditKey = $"{fieldKey}_editKey_{key}";
                if (_editingStringValues.ContainsKey(keyEditKey))
                    _editingStringValues.Remove(keyEditKey);
            }
        }

        /// <summary>
        /// 绘制数组编辑器
        /// </summary>
        private void DrawArrayEditor(string fieldKey, object arrayObj, Type arrayType)
        {
            var array = arrayObj as Array;
            if (array == null) return;

            var elementType = arrayType.GetElementType();
            
            EditorGUILayout.LabelField($"数组长度: {array.Length} (类型: {GetSimpleTypeName(elementType)})", _fieldStyle);
            
            for (int i = 0; i < array.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                
                var currentValue = array.GetValue(i);
                var newValue = DrawValueEditor($"{fieldKey}[{i}]", elementType, currentValue, true);
                
                if (!Equals(newValue, currentValue))
                {
                    array.SetValue(newValue, i);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 绘制通用集合显示
        /// </summary>
        private void DrawGenericCollectionDisplay(object collection, Type collectionType)
        {
            var enumerable = collection as System.Collections.IEnumerable;
            if (enumerable == null) return;

            var count = GetCollectionCount(collection);
            EditorGUILayout.LabelField($"元素数量: {count}", _fieldStyle);
            
            int index = 0;
            foreach (var item in enumerable)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{index++}]", GUILayout.Width(30));
                EditorGUILayout.LabelField(GetValueString(item), _valueStyle);
                EditorGUILayout.EndHorizontal();
                
                if (index > 20) // 限制显示数量
                {
                    EditorGUILayout.LabelField("...(超过20个元素，已截断显示)", EditorStyles.miniLabel);
                    break;
                }
            }
        }

        /// <summary>
        /// 获取集合元素数量
        /// </summary>
        private int GetCollectionCount(object collection)
        {
            if (collection == null) return 0;
            
            if (collection is System.Collections.ICollection coll)
                return coll.Count;
                
            if (collection is Array array)
                return array.Length;
                
            if (collection is System.Collections.IEnumerable enumerable)
            {
                int count = 0;
                foreach (var _ in enumerable) count++;
                return count;
            }
            
            return 0;
        }

        /// <summary>
        /// 获取集合类型名称
        /// </summary>
        private string GetCollectionTypeName(Type collectionType)
        {
            if (!collectionType.IsGenericType) return collectionType.Name;
            
            var genericType = collectionType.GetGenericTypeDefinition();
            var genericArgs = collectionType.GetGenericArguments();
            
            if (genericType == typeof(List<>))
                return $"List<{GetSimpleTypeName(genericArgs[0])}>";
            else if (genericType == typeof(Dictionary<,>))
                return $"Dictionary<{GetSimpleTypeName(genericArgs[0])}, {GetSimpleTypeName(genericArgs[1])}>";
            else if (genericType == typeof(HashSet<>))
                return $"HashSet<{GetSimpleTypeName(genericArgs[0])}>";
            else if (genericType == typeof(Queue<>))
                return $"Queue<{GetSimpleTypeName(genericArgs[0])}>";
            else if (genericType == typeof(Stack<>))
                return $"Stack<{GetSimpleTypeName(genericArgs[0])}>";
                
            return collectionType.Name;
        }

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            else if (type == typeof(string))
                return "";
            else
                return null;
        }

        /// <summary>
        /// 绘制复杂类型显示（只读）
        /// </summary>
        private void DrawComplexTypeDisplay(object value, Type type, bool canExpand = true)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField("null", _valueStyle);
                return;
            }

            if (!canExpand)
            {
                EditorGUILayout.LabelField($"{type.Name} {{ ... }}", _valueStyle);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"{type.Name}", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{field.Name}:", _fieldStyle, GUILayout.Width(120));
                
                var fieldValue = field.GetValue(value);
                EditorGUILayout.LabelField(GetValueString(fieldValue), _valueStyle);
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

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
            
            // 复杂类型显示完整名称
            if (IsComplexType(type)) return type.Name;
            
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
        /// 将字符串转换为指定类型
        /// </summary>
        private object ConvertStringToType(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value)) return GetDefaultValue(targetType);
            
            try
            {
                if (targetType == typeof(string))
                    return value;
                else if (targetType == typeof(int))
                    return int.Parse(value);
                else if (targetType == typeof(float))
                    return float.Parse(value);
                else if (targetType == typeof(double))
                    return double.Parse(value);
                else if (targetType == typeof(bool))
                    return bool.Parse(value);
                else if (targetType == typeof(long))
                    return long.Parse(value);
                else if (targetType == typeof(short))
                    return short.Parse(value);
                else if (targetType == typeof(byte))
                    return byte.Parse(value);
                else if (targetType.IsEnum)
                    return Enum.Parse(targetType, value);
                else
                    return System.Convert.ChangeType(value, targetType);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"类型转换失败: {value} -> {targetType.Name}, 错误: {e.Message}");
                return GetDefaultValue(targetType);
            }
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