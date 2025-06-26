using System.Collections.Generic;
using System.Reflection;
using Twenty2.VomitLib.Config;
using Twenty2.VomitLib.View;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.View
{
    /// <summary>
    /// View调试工具类 - 提供统一的数据访问接口
    /// </summary>
    public static class ViewDebugUtility
    {
        #region Reflection Cache
        
        private static FieldInfo _visibleViewMapField;
        private static FieldInfo _hiddenViewMapField;
        private static FieldInfo _preLoadMapField;
        private static MethodInfo _closeMethod;
        private static MethodInfo _showMethod;
        
        static ViewDebugUtility()
        {
            var viewType = typeof(Twenty2.VomitLib.View.View);
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            
            _visibleViewMapField = viewType.GetField("_visibleViewMap", bindingFlags);
            _hiddenViewMapField = viewType.GetField("_hiddenViewMap", bindingFlags);
            _preLoadMapField = viewType.GetField("_preLoadMap", bindingFlags);
            
            _closeMethod = viewType.GetMethod("Close", BindingFlags.Public | BindingFlags.Static, 
                null, new[] { typeof(string), typeof(ViewParameterBase) }, null);
            // 注意：View系统中没有Show方法，只有OpenAsync
            _showMethod = null;
        }
        
        #endregion
        
        #region Data Access
        
        /// <summary>
        /// 获取所有可见的View
        /// </summary>
        public static Dictionary<string, ViewLogic> GetVisibleViews()
        {
            try
            {
                return _visibleViewMapField?.GetValue(null) as Dictionary<string, ViewLogic> ?? new Dictionary<string, ViewLogic>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取可见View失败: {e.Message}");
                return new Dictionary<string, ViewLogic>();
            }
        }
        
        /// <summary>
        /// 获取所有隐藏的View
        /// </summary>
        public static Dictionary<string, ViewLogic> GetHiddenViews()
        {
            try
            {
                return _hiddenViewMapField?.GetValue(null) as Dictionary<string, ViewLogic> ?? new Dictionary<string, ViewLogic>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取隐藏View失败: {e.Message}");
                return new Dictionary<string, ViewLogic>();
            }
        }
        
        /// <summary>
        /// 获取所有预加载的View
        /// </summary>
        public static Dictionary<string, GameObject> GetPreloadedViews()
        {
            try
            {
                return _preLoadMapField?.GetValue(null) as Dictionary<string, GameObject> ?? new Dictionary<string, GameObject>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"获取预加载View失败: {e.Message}");
                return new Dictionary<string, GameObject>();
            }
        }
        
        /// <summary>
        /// 获取View的详细信息
        /// </summary>
        public static ViewDetailInfo GetViewDetailInfo(string viewName)
        {
            var info = new ViewDetailInfo { Name = viewName };
            
            // 检查可见View
            var visibleViews = GetVisibleViews();
            if (visibleViews.TryGetValue(viewName, out var visibleView))
            {
                PopulateViewInfo(info, visibleView, ViewState.Visible);
                return info;
            }
            
            // 检查隐藏View
            var hiddenViews = GetHiddenViews();
            if (hiddenViews.TryGetValue(viewName, out var hiddenView))
            {
                PopulateViewInfo(info, hiddenView, ViewState.Hidden);
                return info;
            }
            
            // 检查预加载View
            var preloadedViews = GetPreloadedViews();
            if (preloadedViews.TryGetValue(viewName, out var preloadedGameObject))
            {
                info.State = ViewState.Preloaded;
                info.GameObject = preloadedGameObject;
                
                var viewLogic = preloadedGameObject?.GetComponent<ViewLogic>();
                if (viewLogic != null)
                {
                    PopulateViewInfo(info, viewLogic, ViewState.Preloaded);
                }
            }
            
            return info;
        }
        
        /// <summary>
        /// 填充View信息
        /// </summary>
        private static void PopulateViewInfo(ViewDetailInfo info, ViewLogic viewLogic, ViewState state)
        {
            info.State = state;
            info.GameObject = viewLogic.gameObject;
            info.ViewLogic = viewLogic;
            info.SortOrder = viewLogic.ViewCanvas?.sortingOrder ?? 0;
            info.ViewType = viewLogic.GetType().Name;
            
            if (viewLogic.Config != null)
            {
                info.IsCache = viewLogic.Config.IsCache;
                info.HasMask = viewLogic.Config.EnableAutoMask;
                info.Layer = viewLogic.Config.Layer;
                info.AutoBindButtons = viewLogic.Config.AutoBindButtons;
            }
            
            // 获取Canvas信息
            if (viewLogic.ViewCanvas != null)
            {
                info.CanvasRenderMode = viewLogic.ViewCanvas.renderMode;
                info.CanvasSortingLayerName = viewLogic.ViewCanvas.sortingLayerName;
                info.CanvasSortingOrder = viewLogic.ViewCanvas.sortingOrder;
            }
            
            // 获取RectTransform信息
            if (viewLogic.transform is RectTransform rectTransform)
            {
                info.AnchoredPosition = rectTransform.anchoredPosition;
                info.SizeDelta = rectTransform.sizeDelta;
                info.LocalScale = rectTransform.localScale;
            }
        }
        
        #endregion
        
        #region Operations
        
        /// <summary>
        /// 关闭指定View
        /// </summary>
        public static bool CloseView(string viewName)
        {
            try
            {
                _closeMethod?.Invoke(null, new object[] { viewName, null });
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"关闭View失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 显示指定View
        /// </summary>
        public static bool ShowView(string viewName)
        {
            try
            {
                // 使用OpenAsync方法
                var openAsyncMethod = typeof(Twenty2.VomitLib.View.View).GetMethod("OpenAsync", 
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string), typeof(ViewParameterBase) }, null);
                
                if (openAsyncMethod != null)
                {
                    openAsyncMethod.Invoke(null, new object[] { viewName, null });
                    return true;
                }
                else
                {
                    Debug.LogWarning("未找到OpenAsync方法");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"显示View失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取View的运行时统计
        /// </summary>
        public static ViewRuntimeStatistics GetRuntimeStatistics()
        {
            var visibleViews = GetVisibleViews();
            var hiddenViews = GetHiddenViews();
            var preloadedViews = GetPreloadedViews();
            
            var stats = new ViewRuntimeStatistics
            {
                VisibleCount = visibleViews.Count,
                HiddenCount = hiddenViews.Count,
                PreloadedCount = preloadedViews.Count,
                TotalCount = visibleViews.Count + hiddenViews.Count + preloadedViews.Count
            };
            
            // 统计缓存和遮罩View
            foreach (var view in visibleViews.Values)
            {
                if (view.Config?.IsCache == true) stats.CachedCount++;
                if (view.Config?.EnableAutoMask == true) stats.MaskedCount++;
                if (view.ViewCanvas != null && view.ViewCanvas.sortingOrder > stats.MaxSortOrder)
                    stats.MaxSortOrder = view.ViewCanvas.sortingOrder;
            }
            
            foreach (var view in hiddenViews.Values)
            {
                if (view.Config?.IsCache == true) stats.CachedCount++;
                if (view.Config?.EnableAutoMask == true) stats.MaskedCount++;
            }
            
            // 计算平均层级
            var totalSortOrder = 0f;
            var sortOrderCount = 0;
            
            foreach (var view in visibleViews.Values)
            {
                if (view.ViewCanvas != null)
                {
                    totalSortOrder += view.ViewCanvas.sortingOrder;
                    sortOrderCount++;
                }
            }
            
            foreach (var view in hiddenViews.Values)
            {
                if (view.ViewCanvas != null)
                {
                    totalSortOrder += view.ViewCanvas.sortingOrder;
                    sortOrderCount++;
                }
            }
            
            stats.AvgSortOrder = sortOrderCount > 0 ? totalSortOrder / sortOrderCount : 0f;
            
            return stats;
        }
        
        #endregion
        
        #region Data Classes
        
        /// <summary>
        /// View详细信息
        /// </summary>
        public class ViewDetailInfo
        {
            public string Name;
            public ViewState State;
            public GameObject GameObject;
            public ViewLogic ViewLogic;
            public string ViewType;
            public int SortOrder;
            public bool IsCache;
            public bool HasMask;
            public ViewSortLayer Layer;
            public bool AutoBindButtons;
            
            // Canvas信息
            public RenderMode CanvasRenderMode;
            public string CanvasSortingLayerName;
            public int CanvasSortingOrder;
            
            // Transform信息
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector3 LocalScale;
        }
        
        /// <summary>
        /// View运行时统计
        /// </summary>
        public struct ViewRuntimeStatistics
        {
            public int VisibleCount;
            public int HiddenCount;
            public int PreloadedCount;
            public int TotalCount;
            public int CachedCount;
            public int MaskedCount;
            public int MaxSortOrder;
            public float AvgSortOrder;
        }
        
        /// <summary>
        /// View状态枚举
        /// </summary>
        public enum ViewState
        {
            Unknown,
            Visible,
            Hidden,
            Preloaded
        }
        
        #endregion
    }
}