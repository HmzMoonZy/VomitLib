using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// View系统监控器 - 用于在Inspector中显示View调试信息
    /// 注意：此类在Runtime中定义，但Inspector绘制功能仅在Editor下生效
    /// </summary>
    [System.Serializable]
    public class ViewMonitor : MonoBehaviour
    {
        #region Fields

        [Header("View监控设置")] [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _refreshInterval = 0.5f;
        [SerializeField] private bool _autoRefresh = true;

        [Header("显示选项")] [SerializeField] private bool _showVisibleViews = true;
        [SerializeField] private bool _showHiddenViews = true;
        [SerializeField] private bool _showPreloadedViews = true;

        // Runtime数据（仅Editor可见）
        [System.NonSerialized] private double _lastRefreshTime;
        [System.NonSerialized] private List<ViewInfo> _cachedVisibleViews = new List<ViewInfo>();
        [System.NonSerialized] private List<ViewInfo> _cachedHiddenViews = new List<ViewInfo>();
        [System.NonSerialized] private List<ViewInfo> _cachedPreloadedViews = new List<ViewInfo>();

        #endregion

        #region Properties

        public bool EnableMonitoring
        {
            get => _enableMonitoring;
            set => _enableMonitoring = value;
        }

        public float RefreshInterval
        {
            get => _refreshInterval;
            set => _refreshInterval = Mathf.Clamp(value, 0.1f, 5f);
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set => _autoRefresh = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            
            if (_enableMonitoring)
            {
                RefreshViewData();
            }
        }

        private void Update()
        {
            // Runtime中不执行任何操作，所有逻辑由Editor的Inspector处理
            // 保留这个方法以防将来需要Runtime逻辑
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 手动刷新View数据（在Editor中通过Inspector调用）
        /// </summary>
        public void RefreshViewData()
        {
            // Runtime中不执行反射操作，由Editor的Inspector负责更新数据
            // 这个方法保留作为接口，实际刷新由Inspector执行
        }

        /// <summary>
        /// 获取View统计信息
        /// </summary>
        public ViewStatistics GetViewStatistics()
        {
            if (!_enableMonitoring)
                return new ViewStatistics();

            return new ViewStatistics
            {
                VisibleCount = _cachedVisibleViews.Count,
                HiddenCount = _cachedHiddenViews.Count,
                PreloadedCount = _cachedPreloadedViews.Count,
                TotalCount = _cachedVisibleViews.Count + _cachedHiddenViews.Count + _cachedPreloadedViews.Count
            };
        }

        #endregion

        #region Data Update Interface (for Editor)

#if UNITY_EDITOR

        /// <summary>
        /// 更新可见View列表（由Editor调用）
        /// </summary>
        public void UpdateVisibleViews(List<ViewInfo> views)
        {
            _cachedVisibleViews = views ?? new List<ViewInfo>();
        }

        /// <summary>
        /// 更新隐藏View列表（由Editor调用）
        /// </summary>
        public void UpdateHiddenViews(List<ViewInfo> views)
        {
            _cachedHiddenViews = views ?? new List<ViewInfo>();
        }

        /// <summary>
        /// 更新预加载View列表（由Editor调用）
        /// </summary>
        public void UpdatePreloadedViews(List<ViewInfo> views)
        {
            _cachedPreloadedViews = views ?? new List<ViewInfo>();
        }

#endif

        #endregion

        #region Internal Access Methods (for Editor)

#if UNITY_EDITOR

        /// <summary>
        /// 获取缓存的可见View列表（仅Editor访问）
        /// </summary>
        public List<ViewInfo> GetCachedVisibleViews() => _cachedVisibleViews ?? new List<ViewInfo>();

        /// <summary>
        /// 获取缓存的隐藏View列表（仅Editor访问）
        /// </summary>
        public List<ViewInfo> GetCachedHiddenViews() => _cachedHiddenViews ?? new List<ViewInfo>();

        /// <summary>
        /// 获取缓存的预加载View列表（仅Editor访问）
        /// </summary>
        public List<ViewInfo> GetCachedPreloadedViews() => _cachedPreloadedViews ?? new List<ViewInfo>();

        /// <summary>
        /// 关闭指定View（仅Editor访问，实际操作由Inspector执行）
        /// </summary>
        public void CloseView(string viewName)
        {
            // Runtime中不执行反射操作，由Editor的Inspector负责实际关闭View
            Debug.Log($"[ViewMonitor] 请求关闭View: {viewName}");
        }

        /// <summary>
        /// 显示指定View（仅Editor访问，实际操作由Inspector执行）
        /// </summary>
        public void ShowView(string viewName)
        {
            // Runtime中不执行反射操作，由Editor的Inspector负责实际显示View
            Debug.Log($"[ViewMonitor] 请求显示View: {viewName}");
        }

        /// <summary>
        /// 关闭所有可见View（仅Editor访问，实际操作由Inspector执行）
        /// </summary>
        public void CloseAllVisibleViews()
        {
            // Runtime中不执行反射操作，由Editor的Inspector负责实际关闭所有View
            var visibleViews = GetCachedVisibleViews();
            Debug.Log($"[ViewMonitor] 请求关闭所有可见View，共{visibleViews.Count}个");
        }

#endif

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// View信息数据类
    /// </summary>
    [System.Serializable]
    public class ViewInfo
    {
        public string Name;
        public int SortOrder;
        public bool IsVisible;
        public bool IsHidden;
        public bool IsPreloaded;
        public bool IsCache;
        public bool HasMask;
        public GameObject GameObject;
    }

    /// <summary>
    /// View统计信息
    /// </summary>
    [System.Serializable]
    public class ViewStatistics
    {
        public int VisibleCount;
        public int HiddenCount;
        public int PreloadedCount;
        public int TotalCount;

        public override string ToString()
        {
            return $"Total: {TotalCount} (Visible: {VisibleCount}, Hidden: {HiddenCount}, Preloaded: {PreloadedCount})";
        }
    }

    #endregion
}