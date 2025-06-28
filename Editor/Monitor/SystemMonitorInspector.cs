using System.Collections.Generic;
using System.Linq;
using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using SystemInfo = Twenty2.VomitLib.Monitor.SystemInfo;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// SystemMonitor的自定义Inspector - System层数据监控面板
    /// 提供System数据的实时监控、编辑和方法调用功能
    /// </summary>
    [CustomEditor(typeof(SystemMonitor))]
    public class SystemMonitorInspector : UnityEditor.Editor
    {
        #region Fields

        private SystemMonitor _monitor;
        private double _lastRefreshTime;

        // 界面状态
        private bool _showSettings = false;
        private bool _showSystemList = true;
        private Dictionary<Type, bool> _systemFoldoutStates = new Dictionary<Type, bool>();
        private Dictionary<Type, bool> _systemMethodFoldoutStates = new Dictionary<Type, bool>();
        private Dictionary<string, object> _editingValues = new Dictionary<string, object>();
        private Dictionary<string, string> _editingStringValues = new Dictionary<string, string>();

        // 过滤和搜索
        private string _systemSearchFilter = "";
        private string _fieldSearchFilter = "";
        private string _methodSearchFilter = "";
        
        // System选择
        private string[] _availableSystemNames = new string[0];
        private int _selectedSystemIndex = 0;

        // 样式缓存
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _fieldStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _systemHeaderStyle;
        private GUIStyle _methodStyle;

        // 颜色
        private readonly Color _systemColor = new Color(0.4f, 0.6f, 0.9f, 0.3f);
        private readonly Color _fieldColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
        private readonly Color _editableColor = new Color(0.8f, 1f, 0.8f, 0.3f);
        private readonly Color _readOnlyColor = new Color(1f, 0.8f, 0.8f, 0.3f);
        private readonly Color _methodColor = new Color(0.9f, 0.9f, 0.4f, 0.3f);

        // GUI操作相关
        private bool _needsRefresh;
        private Vector2 _systemScrollPosition;

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            _monitor = (SystemMonitor)target;
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
                DrawSystemMonitoring();
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
            EditorGUILayout.LabelField("⚙️ System 业务逻辑监控", _headerStyle);
            
            if (Application.isPlaying && _monitor.EnableMonitoring)
            {
                var systems = _monitor.GetSystemInfos();
                
                if (_monitor.MonitorAllSystems)
                {
                    EditorGUILayout.LabelField($"📋 监控所有System: {systems.Count}个", _subHeaderStyle);
                }
                else if (!string.IsNullOrEmpty(_monitor.SpecificSystemTypeName))
                {
                    var filteredCount = systems.Count(s => s.Name.Contains(_monitor.SpecificSystemTypeName));
                    EditorGUILayout.LabelField($"🎯 监控特定System: {_monitor.SpecificSystemTypeName} ({filteredCount}个)", _subHeaderStyle);
                }
                else
                {
                    EditorGUILayout.LabelField($"监控中的System: {systems.Count}个", _subHeaderStyle);
                }

                // 显示快速选择按钮
                if (systems.Count > 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("快速选择:", GUILayout.Width(60));
                    
                    foreach (var system in systems.Take(3)) // 只显示前3个
                    {
                        if (GUILayout.Button(system.Name, _buttonStyle, GUILayout.MaxWidth(80)))
                        {
                            _monitor.MonitorAllSystems = false;
                            _monitor.SetSpecificSystem(system.Name);
                            // 同步UI状态
                            var index = Array.IndexOf(_availableSystemNames, system.Name);
                            if (index > 0) _selectedSystemIndex = index;
                            _needsRefresh = true;
                        }
                    }
                    
                    if (systems.Count > 3)
                    {
                        EditorGUILayout.LabelField($"...({systems.Count - 3}更多)", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("请在运行时启用监控查看System数据", _subHeaderStyle);
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
            
            var monitorAllProp = serializedObject.FindProperty("_monitorAllSystems");
            var specificSystemProp = serializedObject.FindProperty("_specificSystemTypeName");

            EditorGUILayout.PropertyField(monitorAllProp, new GUIContent("监控所有System"));
            
            if (!monitorAllProp.boolValue)
            {
                DrawSystemSelector(specificSystemProp);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制System选择器
        /// </summary>
        private void DrawSystemSelector(SerializedProperty specificSystemProp)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎯 System选择器", EditorStyles.boldLabel);

            // 更新可用System列表
            UpdateAvailableSystemNames();

            if (_availableSystemNames.Length == 0)
            {
                EditorGUILayout.HelpBox("没有检测到System，请确保游戏运行中且架构已初始化", MessageType.Info);
                EditorGUILayout.PropertyField(specificSystemProp, new GUIContent("手动输入System类型名"));
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("选择System:", GUILayout.Width(80));

                // 下拉选择
                var newIndex = EditorGUILayout.Popup(_selectedSystemIndex, _availableSystemNames);
                if (newIndex != _selectedSystemIndex)
                {
                    _selectedSystemIndex = newIndex;
                    if (_selectedSystemIndex > 0 && _selectedSystemIndex <= _availableSystemNames.Length)
                    {
                        var selectedSystemName = _availableSystemNames[_selectedSystemIndex];
                        specificSystemProp.stringValue = selectedSystemName;
                        _monitor.SetSpecificSystem(selectedSystemName);
                        serializedObject.ApplyModifiedProperties();
                        _needsRefresh = true;
                    }
                }

                EditorGUILayout.EndHorizontal();

                // 快速操作按钮
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("📋 全部显示", _buttonStyle))
                {
                    _selectedSystemIndex = 0;
                    specificSystemProp.stringValue = "";
                    _monitor.MonitorAllSystems = true;
                    serializedObject.ApplyModifiedProperties();
                    _needsRefresh = true;
                }

                if (_selectedSystemIndex > 0 && GUILayout.Button("🎯 只看这个", _buttonStyle))
                {
                    var selectedSystemName = _availableSystemNames[_selectedSystemIndex];
                    specificSystemProp.stringValue = selectedSystemName;
                    _monitor.MonitorAllSystems = false;
                    _monitor.SetSpecificSystem(selectedSystemName);
                    serializedObject.ApplyModifiedProperties();
                    _needsRefresh = true;
                }

                EditorGUILayout.EndHorizontal();

                // 显示当前选择的详细信息
                if (_selectedSystemIndex > 0)
                {
                    var selectedSystemName = _availableSystemNames[_selectedSystemIndex];
                    var systemInfo = GetSystemInfoByName(selectedSystemName);
                    if (systemInfo != null)
                    {
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField($"类型: {systemInfo.FullTypeName}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"字段数: {systemInfo.FieldCount}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"方法数: {systemInfo.MethodCount}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"状态: {(systemInfo.IsInitialized ? "✅ 已初始化" : "❌ 未初始化")}", EditorStyles.miniLabel);
                    }
                }

                // 手动输入选项
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("或手动输入:", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(specificSystemProp, GUIContent.none);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制System监控面板
        /// </summary>
        private void DrawSystemMonitoring()
        {
            // 搜索过滤
            DrawSearchFilters();

            // System列表
            _showSystemList = EditorGUILayout.Foldout(_showSystemList, $"⚙️ System列表", true);
            if (_showSystemList)
            {
                DrawSystemList();
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
            EditorGUILayout.LabelField("System:", GUILayout.Width(50));
            _systemSearchFilter = EditorGUILayout.TextField(_systemSearchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("字段:", GUILayout.Width(50));
            _fieldSearchFilter = EditorGUILayout.TextField(_fieldSearchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("方法:", GUILayout.Width(50));
            _methodSearchFilter = EditorGUILayout.TextField(_methodSearchFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制System列表
        /// </summary>
        private void DrawSystemList()
        {
            var systems = _monitor.GetSystemInfos();
            
            if (systems.Count == 0)
            {
                EditorGUILayout.HelpBox("没有检测到System，请确保架构已初始化", MessageType.Info);
                return;
            }

            // 过滤Systems
            var filteredSystems = string.IsNullOrEmpty(_systemSearchFilter)
                ? systems
                : systems.Where(s => s.Name.ToLower().Contains(_systemSearchFilter.ToLower())).ToList();

            _systemScrollPosition = EditorGUILayout.BeginScrollView(_systemScrollPosition);

            foreach (var system in filteredSystems)
            {
                DrawSystemInfo(system);
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单个System信息
        /// </summary>
        private void DrawSystemInfo(SystemInfo system)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = _systemColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            // System头部
            EditorGUILayout.BeginHorizontal();
            
            if (!_systemFoldoutStates.ContainsKey(system.Type))
                _systemFoldoutStates[system.Type] = false;
                
            _systemFoldoutStates[system.Type] = EditorGUILayout.Foldout(_systemFoldoutStates[system.Type], 
                $"⚙️ {system.Name}", true, _systemHeaderStyle);
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(system.IsInitialized ? "✅" : "❌", GUILayout.Width(20));
            EditorGUILayout.LabelField($"({system.FieldCount}f/{system.MethodCount}m)", GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();

            // System详情
            if (_systemFoldoutStates[system.Type])
            {
                EditorGUILayout.LabelField($"类型: {system.FullTypeName}", _fieldStyle);
                EditorGUILayout.Space(3);

                // 绘制字段
                DrawSystemFields(system);
                
                EditorGUILayout.Space(3);
                
                // 绘制方法
                DrawSystemMethods(system);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制System字段
        /// </summary>
        private void DrawSystemFields(SystemInfo system)
        {
            var fields = _monitor.GetSystemFields(system.Type);
            
            if (fields.Count == 0) return;
            
            EditorGUILayout.LabelField("📋 字段属性", EditorStyles.boldLabel);
            
            // 过滤字段
            var filteredFields = string.IsNullOrEmpty(_fieldSearchFilter)
                ? fields
                : fields.Where(f => f.Name.ToLower().Contains(_fieldSearchFilter.ToLower())).ToList();

            foreach (var field in filteredFields)
            {
                DrawFieldEditor(system, field);
            }
        }

        /// <summary>
        /// 绘制System方法
        /// </summary>
        private void DrawSystemMethods(SystemInfo system)
        {
            var methods = _monitor.GetSystemMethods(system.Type);
            
            if (methods.Count == 0) return;

            // 过滤方法
            var filteredMethods = string.IsNullOrEmpty(_methodSearchFilter)
                ? methods
                : methods.Where(m => m.Name.ToLower().Contains(_methodSearchFilter.ToLower())).ToList();

            if (filteredMethods.Count == 0) return;

            // 方法折叠状态
            if (!_systemMethodFoldoutStates.ContainsKey(system.Type))
                _systemMethodFoldoutStates[system.Type] = false;

            _systemMethodFoldoutStates[system.Type] = EditorGUILayout.Foldout(_systemMethodFoldoutStates[system.Type], 
                $"🔧 方法调用 ({filteredMethods.Count})", true);

            if (_systemMethodFoldoutStates[system.Type])
            {
                foreach (var method in filteredMethods)
                {
                    DrawMethodInvoker(system, method);
                }
            }
        }

        /// <summary>
        /// 绘制字段编辑器
        /// </summary>
        private void DrawFieldEditor(SystemInfo system, FieldPropertyInfo field)
        {
            var fieldKey = $"{system.Type.FullName}.{field.Name}";
            var currentValue = _monitor.GetSystemFieldValue(system.Type, field.Name);

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
                    if (_monitor.SetSystemFieldValue(system.Type, field.Name, newValue))
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
        /// 绘制方法调用器
        /// </summary>
        private void DrawMethodInvoker(SystemInfo system, MethodInfo method)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = _methodColor;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = originalColor;

            EditorGUILayout.BeginHorizontal();
            
            // 方法信息
            var parameters = method.GetParameters();
            var paramString = parameters.Length == 0 ? "()" : $"({parameters.Length}参数)";
            var returnType = method.ReturnType == typeof(void) ? "void" : method.ReturnType.Name;
            
            EditorGUILayout.LabelField($"🔧 {method.Name}{paramString}", _methodStyle, GUILayout.Width(200));
            EditorGUILayout.LabelField($"→ {returnType}", _valueStyle, GUILayout.Width(80));
            
            // 调用按钮
            if (parameters.Length == 0)
            {
                if (GUILayout.Button("🚀 调用", _buttonStyle, GUILayout.Width(60)))
                {
                    try
                    {
                        var result = _monitor.InvokeSystemMethod(system.Type, method.Name);
                        if (method.ReturnType != typeof(void))
                        {
                            Debug.Log($"[SystemMonitor] {system.Name}.{method.Name}() 返回: {result}");
                        }
                        else
                        {
                            Debug.Log($"[SystemMonitor] {system.Name}.{method.Name}() 调用完成");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[SystemMonitor] 调用方法失败: {e.Message}");
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("(需要参数)", _valueStyle);
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
            EditorGUILayout.LabelField("⚙️ System业务逻辑监控面板", _headerStyle);
            EditorGUILayout.LabelField("请在运行时启用监控查看System数据状态", _subHeaderStyle);
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
            
            // 系统类型不算复杂类型
            if (type.Namespace?.StartsWith("System") == true) return false;
            if (type.Namespace?.StartsWith("UnityEngine") == true) return false;
            if (type.Namespace?.StartsWith("QFramework") == true) return false;
            
            // 自定义类型算复杂类型
            return type.IsClass && !type.IsAbstract;
        }

        /// <summary>
        /// 绘制复杂类型编辑器
        /// </summary>
        private object DrawComplexTypeEditor(string fieldKey, object currentValue, Type fieldType)
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

            // 获取或创建折叠状态
            var foldoutKey = $"{fieldKey}_foldout";
            if (!_editingValues.ContainsKey(foldoutKey))
                _editingValues[foldoutKey] = false;

            var foldout = (bool)_editingValues[foldoutKey];
            
            EditorGUILayout.BeginVertical();
            
            // 类型头部（可折叠）
            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, $"📦 {fieldType.Name}", true);
            _editingValues[foldoutKey] = foldout;
            
            if (GUILayout.Button("🗑️", _buttonStyle, GUILayout.Width(25)))
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
                    EditorGUILayout.LabelField($"🔸 {field.Name}", _fieldStyle, GUILayout.Width(120));
                    
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
            EditorGUILayout.LabelField($"📦 {type.Name}", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🔸 {field.Name}:", _fieldStyle, GUILayout.Width(120));
                
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
                _monitor.RefreshSystemData();
            }
        }

        /// <summary>
        /// 更新可用System名称列表
        /// </summary>
        private void UpdateAvailableSystemNames()
        {
            if (!Application.isPlaying || _monitor == null || !_monitor.EnableMonitoring)
            {
                _availableSystemNames = new string[0];
                return;
            }

            var systemInfos = _monitor.GetSystemInfos();
            var systemNames = new List<string> { "-- 选择System --" };
            
            foreach (var system in systemInfos)
            {
                systemNames.Add(system.Name);
            }

            _availableSystemNames = systemNames.ToArray();

            // 保持当前选择状态
            if (!string.IsNullOrEmpty(_monitor.SpecificSystemTypeName))
            {
                for (int i = 1; i < _availableSystemNames.Length; i++)
                {
                    if (_availableSystemNames[i].Contains(_monitor.SpecificSystemTypeName))
                    {
                        _selectedSystemIndex = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 根据名称获取System信息
        /// </summary>
        private SystemInfo GetSystemInfoByName(string systemName)
        {
            if (_monitor == null) return null;
            
            var systemInfos = _monitor.GetSystemInfos();
            return systemInfos.FirstOrDefault(s => s.Name == systemName);
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

                _systemHeaderStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };

                _methodStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 0.6f) : new Color(0.6f, 0.4f, 0f) }
                };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SystemMonitorInspector] 初始化样式失败: {e.Message}");
            }
        }

        #endregion
    }
}