using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// View监控标签页 - 完全重构版，支持View的全面调试
    /// </summary>
    public class ViewMonitorTab
    {
        #region Fields

        // 过滤选项
        private ViewFilterType _filterType = ViewFilterType.All;
        private string _searchFilter = "";
        private bool _groupBySortOrder = true;

        // UI状态
        private Vector2 _scrollPosition;
        private Dictionary<string, bool> _viewFoldoutStates = new Dictionary<string, bool>();
        private int _selectedViewIndex = -1;

        // 缓存的View数据
        private List<ViewDetailInfo> _viewDetails = new List<ViewDetailInfo>();

        // 样式
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _stateBadgeStyle;

        // 状态颜色
        private Color _visibleColor = new Color(0.3f, 0.9f, 0.5f);
        private Color _hiddenColor = new Color(0.9f, 0.6f, 0.3f);
        private Color _cachedColor = new Color(0.4f, 0.7f, 1f);
        private Color _preloadedColor = new Color(0.6f, 0.4f, 0.9f);

        #endregion

        #region Public Methods

        public void OnGUI(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            EnsureStylesInitialized();
            RefreshViewData(provider);

            // 绘制顶部工具栏
            DrawToolbar(window, provider);

            EditorGUILayout.Space(5);

            // 绘制统计信息
            DrawStatistics(window, provider);

            EditorGUILayout.Space(5);

            // 绘制View列表
            DrawViewList(window, provider);
        }

        #endregion

        #region Data Refresh

        private void RefreshViewData(MonitorDataProvider provider)
        {
            _viewDetails.Clear();

            var viewInfos = provider.ViewInfos;
            foreach (var viewInfo in viewInfos)
            {
                var detail = CreateViewDetail(viewInfo);
                if (detail != null)
                {
                    _viewDetails.Add(detail);
                }
            }

            // 按排序分组
            if (_groupBySortOrder)
            {
                _viewDetails = _viewDetails.OrderBy(v => v.SortOrder).ToList();
            }
        }

        private ViewDetailInfo CreateViewDetail(ViewInfo viewInfo)
        {
            if (viewInfo.GameObject == null) return null;

            var detail = new ViewDetailInfo
            {
                Name = viewInfo.Name,
                GameObject = viewInfo.GameObject,
                IsVisible = viewInfo.IsVisible,
                IsHidden = viewInfo.IsHidden,
                IsCache = viewInfo.IsCache,
                IsPreloaded = viewInfo.IsPreloaded,
                HasMask = viewInfo.HasMask,
                SortOrder = viewInfo.SortOrder
            };

            // 获取ViewLogic组件
            if (viewInfo.GameObject != null)
            {
                var viewLogic = viewInfo.GameObject.GetComponent("ViewLogic");
                if (viewLogic != null)
                {
                    detail.ViewLogicType = viewLogic.GetType();
                    // Try to get Canvas component
                    detail.Canvas = viewInfo.GameObject.GetComponent<Canvas>();
                }
            }

            // 获取Transform信息
            if (viewInfo.GameObject != null)
            {
                detail.Transform = viewInfo.GameObject.transform;
                detail.Path = GetGameObjectPath(viewInfo.GameObject);
                detail.SiblingIndex = viewInfo.GameObject.transform.GetSiblingIndex();
            }

            // 获取组件信息
            if (viewInfo.GameObject != null)
            {
                detail.Components = viewInfo.GameObject.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .ToList();
            }

            return detail;
        }

        #endregion

        #region Drawing Methods

        private void DrawToolbar(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("过滤:", _labelStyle, GUILayout.Width(35));

            _filterType = (ViewFilterType)EditorGUILayout.EnumPopup(_filterType, GUILayout.Width(80));

            GUILayout.Label("搜索:", _labelStyle, GUILayout.Width(35));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, GUILayout.Width(150));

            GUILayout.Space(10);

            _groupBySortOrder = EditorGUILayout.Toggle("按排序分组", _groupBySortOrder, GUILayout.Width(90));

            GUILayout.FlexibleSpace();

            // 刷新按钮
            if (GUILayout.Button("刷新", _buttonStyle, GUILayout.Width(60)))
            {
                RefreshViewData(provider);
            }

            // 操作按钮
            EditorGUI.BeginDisabledGroup(_selectedViewIndex < 0);
            if (GUILayout.Button("打开选中", _buttonStyle, GUILayout.Width(70)))
            {
                OpenSelectedView();
            }
            if (GUILayout.Button("关闭选中", _buttonStyle, GUILayout.Width(70)))
            {
                CloseSelectedView();
            }
            EditorGUI.EndDisabledGroup();

            // 全部操作
            if (GUILayout.Button("关闭全部可见", _buttonStyle, GUILayout.Width(80)))
            {
                CloseAllVisibleViews();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatistics(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            var viewInfos = provider.ViewInfos;

            var visibleCount = viewInfos.Count(v => v.IsVisible);
            var hiddenCount = viewInfos.Count(v => v.IsHidden);
            var cachedCount = viewInfos.Count(v => v.IsCache);
            var preloadedCount = viewInfos.Count(v => v.IsPreloaded);
            var totalCount = viewInfos.Count;

            // 统计卡片
            EditorGUILayout.BeginHorizontal();

            DrawStatCard(window, "总数", totalCount.ToString(), new Color(0.3f, 0.8f, 1f, 0.2f));
            DrawStatCard(window, "可见", visibleCount.ToString(), _visibleColor);
            DrawStatCard(window, "隐藏", hiddenCount.ToString(), _hiddenColor);
            DrawStatCard(window, "缓存", cachedCount.ToString(), _cachedColor);
            DrawStatCard(window, "预加载", preloadedCount.ToString(), _preloadedColor);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatCard(VomitMonitorWindow window, string label, string value, Color color)
        {
            EditorGUILayout.BeginVertical(window._boxStyle, GUILayout.Width(80), GUILayout.Height(50));

            GUILayout.Label(label, _labelStyle);
            GUILayout.Label(value, new GUIStyle(_valueStyle)
            {
                fontSize = 16,
                normal = { textColor = color }
            });

            EditorGUILayout.EndVertical();
        }

        private void DrawViewList(VomitMonitorWindow window, MonitorDataProvider provider)
        {
            // 应用过滤
            var filteredViews = _viewDetails.Where(v =>
            {
                // 搜索过滤
                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    if (!(v.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
                        !(v.Path.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return false;
                    }
                }

                // 类型过滤
                switch (_filterType)
                {
                    case ViewFilterType.All:
                        return true;
                    case ViewFilterType.Visible:
                        return v.IsVisible;
                    case ViewFilterType.Hidden:
                        return v.IsHidden;
                    case ViewFilterType.Cached:
                        return v.IsCache;
                    case ViewFilterType.Preloaded:
                        return v.IsPreloaded;
                    default:
                        return true;
                }
            }).ToList();

            if (filteredViews.Count == 0)
            {
                EditorGUILayout.HelpBox("没有找到匹配的View", MessageType.Info);
                return;
            }

            // 分组显示
            if (_groupBySortOrder)
            {
                DrawGroupedViews(window, filteredViews);
            }
            else
            {
                DrawFlatViews(window, filteredViews);
            }
        }

        private void DrawGroupedViews(VomitMonitorWindow window, List<ViewDetailInfo> views)
        {
            var groups = views.GroupBy(v => v.SortOrder)
                .OrderBy(g => g.Key);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var group in groups)
            {
                var groupViews = group.ToList();
                if (groupViews.Count == 0) continue;

                var sortOrder = group.Key;
                var isActive = groupViews.Any(v => v.IsVisible);

                // 组标题
                DrawGroupHeader(window, sortOrder, isActive, groupViews.Count);

                // 组内的View
                EditorGUI.indentLevel++;
                foreach (var view in groupViews)
                {
                    DrawViewItem(window, view);
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFlatViews(VomitMonitorWindow window, List<ViewDetailInfo> views)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var view in views)
            {
                DrawViewItem(window, view);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupHeader(VomitMonitorWindow window, int sortOrder, bool isActive, int count)
        {
            var rect = EditorGUILayout.GetControlRect(false, 24);
            var bgColor = isActive ? new Color(0.3f, 0.9f, 0.5f, 0.2f) : new Color(0.15f, 0.15f, 0.15f, 0.3f);

            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"排序 {sortOrder}", _titleStyle, GUILayout.Width(80));
            GUILayout.Label($"({count} View{(count > 1 ? "s" : "")})", _labelStyle);

            GUILayout.FlexibleSpace();

            var statusText = isActive ? "活跃" : "空闲";
            var statusColor = isActive ? _visibleColor : _hiddenColor;
            GUILayout.Label(statusText, new GUIStyle(_labelStyle)
            {
                normal = { textColor = statusColor }
            });

            EditorGUILayout.EndHorizontal();
        }

        private void DrawViewItem(VomitMonitorWindow window, ViewDetailInfo view)
        {
            // 初始化折叠状态
            if (!_viewFoldoutStates.ContainsKey(view.Name))
            {
                _viewFoldoutStates[view.Name] = false;
            }

            // 背景色
            Color bgColor;
            if (view.IsVisible)
                bgColor = new Color(0.2f, 0.5f, 0.3f, 0.15f);
            else if (view.IsHidden)
                bgColor = new Color(0.5f, 0.2f, 0.2f, 0.15f);
            else if (view.IsCache)
                bgColor = new Color(0.2f, 0.3f, 0.5f, 0.15f);
            else if (view.IsPreloaded)
                bgColor = new Color(0.3f, 0.2f, 0.5f, 0.15f);
            else
                bgColor = new Color(0.15f, 0.15f, 0.15f, 0.1f);

            var rect = EditorGUILayout.GetControlRect(false, view.IsVisible || view.IsPreloaded ? 28 : 24);

            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUI.DrawRect(rect, bgColor);
            GUI.backgroundColor = oldBg;

            // 选中高亮
            if (_selectedViewIndex >= 0 && _selectedViewIndex < _viewDetails.Count &&
                _viewDetails[_selectedViewIndex] == view)
            {
                var highlightRect = new Rect(rect.x + 2, rect.y + 1, rect.width - 4, rect.height - 2);
                EditorGUI.DrawRect(highlightRect, new Color(0.3f, 0.8f, 1f, 0.3f));
            }

            // 点击区域
            if (GUI.Button(rect, "", GUIStyle.none))
            {
                _selectedViewIndex = _viewDetails.IndexOf(view);
            }

            EditorGUILayout.BeginHorizontal();

            // 折叠箭头
            var foldoutRect = new Rect(rect.x + 5, rect.y, 20, rect.height);
            _viewFoldoutStates[view.Name] = EditorGUI.Foldout(foldoutRect,
                _viewFoldoutStates[view.Name], "", true);

            // View名称
            GUILayout.Label(view.Name, _titleStyle, GUILayout.Width(150));

            // 状态徽章
            DrawStateBadge(window, view);

            // 额外信息
            if (view.SortOrder > 0)
            {
                GUILayout.Label($"排序:{view.SortOrder}", _valueStyle, GUILayout.Width(60));
            }

            if (view.HasMask)
            {
                GUILayout.Label("[遮罩]", _valueStyle, GUILayout.Width(50));
            }

            // 操作按钮
            GUILayout.FlexibleSpace();

            if (view.IsVisible || view.IsPreloaded)
            {
                if (GUILayout.Button("关闭", _buttonStyle, GUILayout.Width(50)))
                {
                    CloseView(view.Name);
                }
            }
            else
            {
                if (GUILayout.Button("打开", _buttonStyle, GUILayout.Width(50)))
                {
                    OpenView(view.Name);
                }
            }

            EditorGUILayout.EndHorizontal();

            // 详细信息（折叠后显示）
            if (_viewFoldoutStates[view.Name])
            {
                EditorGUI.indentLevel++;
                DrawViewDetails(window, view);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(3);
            }
        }

        private void DrawStateBadge(VomitMonitorWindow window, ViewDetailInfo view)
        {
            string text;
            Color color;
            Color textColor = Color.white;

            if (view.IsVisible)
            {
                text = "可见";
                color = _visibleColor;
            }
            else if (view.IsHidden)
            {
                text = "隐藏";
                color = _hiddenColor;
            }
            else if (view.IsCache)
            {
                text = "缓存";
                color = _cachedColor;
            }
            else if (view.IsPreloaded)
            {
                text = "预加载";
                color = _preloadedColor;
            }
            else
            {
                text = "未知";
                color = new Color(0.5f, 0.5f, 0.5f);
            }

            var badgeStyle = new GUIStyle(_stateBadgeStyle)
            {
                normal = { background = MakeTexture(2, 2, color) },
                hover = { background = MakeTexture(2, 2, MultiplyColor(color, 1.2f)) },
                active = { background = MakeTexture(2, 2, MultiplyColor(color, 0.8f)) }
            };

            var size = badgeStyle.CalcSize(new GUIContent(text));
            GUILayout.Label(text, badgeStyle);
        }

        private void DrawViewDetails(VomitMonitorWindow window, ViewDetailInfo view)
        {
            EditorGUILayout.BeginVertical(window._boxStyle);

            // 基本信息
            EditorGUILayout.LabelField("View名称:", view.Name);
            EditorGUILayout.LabelField("完整路径:", view.Path ?? "无");
            EditorGUILayout.LabelField("排序层级:", view.SortOrder.ToString());

            if (view.ViewLogicType != null)
            {
                EditorGUILayout.LabelField("ViewLogic类型:", view.ViewLogicType.Name);
            }

            EditorGUILayout.Space(5);

            // 状态信息
            EditorGUILayout.LabelField("状态信息:", EditorStyles.boldLabel);
            DrawStateInfo("可见", view.IsVisible);
            DrawStateInfo("隐藏", view.IsHidden);
            DrawStateInfo("缓存", view.IsCache);
            DrawStateInfo("预加载", view.IsPreloaded);
            DrawStateInfo("有遮罩", view.HasMask);

            EditorGUILayout.Space(5);

            // Transform信息
            if (view.Transform != null)
            {
                EditorGUILayout.LabelField("Transform信息:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("本地位置:", view.Transform.localPosition.ToString("F2"));
                EditorGUILayout.LabelField("SiblingIndex:", view.SiblingIndex.ToString());
                EditorGUILayout.LabelField("父级:", view.Transform.parent?.name ?? "无");
            }

            // 组件列表
            if (view.Components != null && view.Components.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"组件 ({view.Components.Count}):", EditorStyles.boldLabel);
                var componentsStr = string.Join(", ", view.Components.Take(5).ToArray());
                if (view.Components.Count > 5)
                {
                    componentsStr += "...";
                }
                EditorGUILayout.LabelField(componentsStr, _valueStyle);
            }

            EditorGUILayout.Space(5);

            // 快捷操作
            EditorGUILayout.LabelField("快捷操作:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("在Hierarchy中选中", _buttonStyle))
            {
                SelectInHierarchy(view);
            }

            if (view.IsVisible || view.IsPreloaded)
            {
                if (GUILayout.Button("Ping对象", _buttonStyle))
                {
                    EditorGUIUtility.PingObject(view.GameObject);
                }
            }

            if (GUILayout.Button("复制名称", _buttonStyle))
            {
                GUIUtility.systemCopyBuffer = view.Name;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStateInfo(string label, bool value)
        {
            var color = value ? new Color(0.4f, 1f, 0.6f) : new Color(0.5f, 0.5f, 0.5f);
            var text = value ? "✓ " : "✗ ";
            EditorGUILayout.LabelField(label, text, new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = color }
            });
        }

        #endregion

        #region Helper Methods

        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "null";

            var path = obj.name;
            var parent = obj.transform.parent;

            while (parent != null)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }

            return path;
        }

        private void OpenView(string viewName)
        {
            try
            {
                var viewType = Type.GetType("Twenty2.VomitLib.View.View, Twenty2.VomitLib");
                if (viewType == null)
                {
                    Debug.LogWarning("[ViewMonitorTab] View类型未找到");
                    return;
                }

                // 尝试使用反射调用 View.Open<ViewName>()
                var openMethod = viewType.GetMethod("Open");
                if (openMethod != null)
                {
                    openMethod.Invoke(null, null);
                    Debug.Log($"[ViewMonitorTab] 打开View: {viewName}");

                    // 刷新数据
                    EditorApplication.delayCall += () =>
                    {
                        var window = EditorWindow.GetWindow<VomitMonitorWindow>();
                        if (window != null) window.Repaint();
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ViewMonitorTab] 打开View失败: {e.Message}");
            }
        }

        private void CloseView(string viewName)
        {
            try
            {
                var viewType = Type.GetType("Twenty2.VomitLib.View.View, Twenty2.VomitLib");
                if (viewType == null)
                {
                    Debug.LogWarning("[ViewMonitorTab] View类型未找到");
                    return;
                }

                var closeMethod = viewType.GetMethod("Close", new[] { typeof(string) });
                if (closeMethod != null)
                {
                    closeMethod.Invoke(null, new object[] { viewName });
                    Debug.Log($"[ViewMonitorTab] 关闭View: {viewName}");

                    EditorApplication.delayCall += () =>
                    {
                        var window = EditorWindow.GetWindow<VomitMonitorWindow>();
                        if (window != null) window.Repaint();
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ViewMonitorTab] 关闭View失败: {e.Message}");
            }
        }

        private void OpenSelectedView()
        {
            if (_selectedViewIndex >= 0 && _selectedViewIndex < _viewDetails.Count)
            {
                var view = _viewDetails[_selectedViewIndex];
                OpenView(view.Name);
            }
        }

        private void CloseSelectedView()
        {
            if (_selectedViewIndex >= 0 && _selectedViewIndex < _viewDetails.Count)
            {
                var view = _viewDetails[_selectedViewIndex];
                CloseView(view.Name);
            }
        }

        private void CloseAllVisibleViews()
        {
            var visibleViews = _viewDetails.Where(v => v.IsVisible).ToList();
            foreach (var view in visibleViews)
            {
                CloseView(view.Name);
            }
        }

        private void SelectInHierarchy(ViewDetailInfo view)
        {
            if (view.GameObject != null)
            {
                Selection.activeObject = view.GameObject;
                EditorGUIUtility.PingObject(view.GameObject);
            }
        }

        private Color MultiplyColor(Color color, float multiplier)
        {
            return new Color(
                Mathf.Clamp01(color.r * multiplier),
                Mathf.Clamp01(color.g * multiplier),
                Mathf.Clamp01(color.b * multiplier),
                color.a
            );
        }

        private Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void EnsureStylesInitialized()
        {
            if (_titleStyle != null) return;

            var lightTextColor = new Color(0.95f, 0.95f, 0.95f);
            var mediumTextColor = new Color(0.75f, 0.75f, 0.75f);

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = lightTextColor },
                padding = new RectOffset(3, 2, 3, 2)
            };

            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = mediumTextColor },
                padding = new RectOffset(3, 2, 3, 2)
            };

            _valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = mediumTextColor },
                padding = new RectOffset(3, 2, 3, 2)
            };

            _buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                padding = new RectOffset(8, 3, 8, 3)
            };

            _stateBadgeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 3, 6, 3)
            };
        }

        #endregion

        #region Data Classes

        private class ViewDetailInfo
        {
            public string Name;
            public GameObject GameObject;
            public bool IsVisible;
            public bool IsHidden;
            public bool IsCache;
            public bool IsPreloaded;
            public bool HasMask;
            public int SortOrder;
            public Type ViewLogicType;
            public Canvas Canvas;
            public Transform Transform;
            public string Path;
            public int SiblingIndex;
            public List<string> Components;
        }

        private enum ViewFilterType
        {
            All,
            Visible,
            Hidden,
            Cached,
            Preloaded
        }

        #endregion
    }
}
