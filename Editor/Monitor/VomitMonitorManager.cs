using Twenty2.VomitLib.Monitor;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// VomitLib监控器管理器 - 自动创建和管理监控组件（仅Editor模式，强制启用）
    /// </summary>
    public static class VomitMonitorManager
    {
        private static GameObject _vomitMonitorRoot;
        private static VomitMonitor _vomitMonitor;
        private static ViewMonitor _viewMonitor;
        private static ProcedureMonitor _procedureMonitor;
        private static bool _isInitialized = false;
        
        static VomitMonitorManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Update;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Initialize();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Cleanup();
            }
        }
        
        private static void Update()
        {
            if (Application.isPlaying && !_isInitialized)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// 初始化监控器（Editor模式下强制启用）
        /// </summary>
        private static void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                CreateVomitMonitor();
                _isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"VomitMonitor初始化失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 清理监控器
        /// </summary>
        private static void Cleanup()
        {
            if (_vomitMonitorRoot != null)
            {
                Object.DestroyImmediate(_vomitMonitorRoot);
                _vomitMonitorRoot = null;
                _vomitMonitor = null;
                _viewMonitor = null;
                _procedureMonitor = null;
            }
            _isInitialized = false;
        }
        
        /// <summary>
        /// 创建VomitMonitor层次结构
        /// </summary>
        public static void CreateVomitMonitor()
        {
            // 避免重复创建
            if (_vomitMonitorRoot != null) return;
            
            // 创建根节点并挂载VomitMonitor组件
            _vomitMonitorRoot = new GameObject("[VomitMonitor]");
            _vomitMonitorRoot.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            
            // 设置为场景切换时不销毁
            Object.DontDestroyOnLoad(_vomitMonitorRoot);
            
            _vomitMonitor = _vomitMonitorRoot.AddComponent<VomitMonitor>();
            
            // 创建ViewMonitor子节点
            var viewMonitorGO = new GameObject("ViewMonitor");
            viewMonitorGO.transform.SetParent(_vomitMonitorRoot.transform);
            _viewMonitor = viewMonitorGO.AddComponent<ViewMonitor>();
            
            // 创建ProcedureMonitor子节点
            var procedureMonitorGO = new GameObject("ProcedureMonitor");
            procedureMonitorGO.transform.SetParent(_vomitMonitorRoot.transform);
            _procedureMonitor = procedureMonitorGO.AddComponent<ProcedureMonitor>();
            
            // 设置根组件的子监控器引用
            _vomitMonitor.SetChildMonitors(_viewMonitor, _procedureMonitor);
            
            Debug.Log("VomitMonitor已自动创建并初始化");
        }
        
        /// <summary>
        /// 获取当前监控器状态
        /// </summary>
        public static bool IsMonitorActive => _vomitMonitorRoot != null;
        
        /// <summary>
        /// 获取VomitMonitor根组件
        /// </summary>
        public static VomitMonitor VomitMonitor => _vomitMonitor;
        
        /// <summary>
        /// 获取ViewMonitor
        /// </summary>
        public static ViewMonitor ViewMonitor => _viewMonitor;
        
        /// <summary>
        /// 获取ProcedureMonitor
        /// </summary>
        public static ProcedureMonitor ProcedureMonitor => _procedureMonitor;

        #region Menu Items

        /// <summary>
        /// 检查监控器状态
        /// </summary>
        [MenuItem("VomitLib/Monitor/Check Status", false, 100)]
        public static void CheckMonitorStatus()
        {
            var status = VomitMonitorManager.IsMonitorActive;

            var message = $"VomitLib监控器状态:\n\n";
            message += $"配置模式: Editor强制启用\n";
            message += $"运行状态: {(status ? "正在运行" : "未运行")}\n";
            message += $"当前模式: {(Application.isPlaying ? "Play模式" : "Edit模式")}\n\n";

            if (Application.isPlaying && status)
            {
                message += "监控器正常运行中。";
            }
            else if (Application.isPlaying && !status)
            {
                message += "注意: Play模式下监控器未运行，可能发生初始化错误。";
            }
            else
            {
                message += "进入Play模式后监控器将自动创建。";
            }

            EditorUtility.DisplayDialog("VomitMonitor状态", message, "确定");
        }

        /// <summary>
        /// 手动创建监控器（测试用）
        /// </summary>
        [MenuItem("VomitLib/Monitor/Force Create (Debug)", false, 200)]
        public static void ForceCreateMonitor()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("无法创建", "监控器只能在Play模式下创建", "确定");
                return;
            }

            if (VomitMonitorManager.IsMonitorActive)
            {
                EditorUtility.DisplayDialog("已存在", "监控器已经在运行中", "确定");
                return;
            }

            // 直接调用公有方法
            VomitMonitorManager.CreateVomitMonitor();

            Debug.Log("强制创建VomitMonitor (仅用于调试)");
        }
        #endregion
    }
}