using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.SharpWindow
{
    /// <summary>
    /// 通用快速调试工具窗口。常驻可用（不限 Play 模式）。
    /// 通过 <see cref="ISharpModule"/> 组合各调试功能。
    /// </summary>
    internal class SharpWindow : EditorWindow
    {
        private const string SelectedTabSessionKey =
            "Twenty2.VomitLib.SharpWindow.SelectedTab";

        private sealed class SharpTab
        {
            public string Name;
            public readonly List<ISharpModule> Modules =
                new List<ISharpModule>();
            public Vector2 ScrollPosition;
        }

        private readonly List<ISharpModule> _modules =
            new List<ISharpModule>();
        private readonly List<SharpTab> _tabs =
            new List<SharpTab>();

        private string[] _tabNames = Array.Empty<string>();
        private int _selectedTabIndex = -1;

        [MenuItem("VomitLib/Sharp Window %q")]
        private static void Open()
        {
            // toggle：已开则关，未开则开
            if (HasOpenInstances<SharpWindow>())
            {
                GetWindow<SharpWindow>().Close();
            }
            else
            {
                GetWindow<SharpWindow>("Sharp");
            }
        }

        private void OnEnable()
        {
            minSize = new Vector2(320f, 180f);
            DiscoverModules();
            foreach (var m in _modules)
            {
                m.OnEnable();
            }
        }

        private void OnDisable()
        {
            foreach (var m in _modules)
            {
                m.OnDisable();
            }
        }

        private void OnGUI()
        {
            if (_tabs.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "未发现可用的 SharpWindow 模块。",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int nextIndex = GUILayout.Toolbar(
                    _selectedTabIndex,
                    _tabNames,
                    EditorStyles.toolbarButton,
                    GUILayout.ExpandWidth(true));
                SelectTab(nextIndex);
            }

            int selectedIndex = Mathf.Clamp(
                _selectedTabIndex,
                0,
                _tabs.Count - 1);
            SharpTab selectedTab = _tabs[selectedIndex];
            Vector2 scrollPosition = EditorGUILayout.BeginScrollView(
                selectedTab.ScrollPosition);
            selectedTab.ScrollPosition = scrollPosition;

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Space(6f);
                DrawTabModules(selectedTab);
                EditorGUILayout.Space(6f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DiscoverModules()
        {
            _modules.Clear();
            _tabs.Clear();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ISharpModule>())
            {
                if (type.IsAbstract ||
                    type.IsInterface ||
                    type.ContainsGenericParameters)
                {
                    continue;
                }

                try
                {
                    var module = Activator.CreateInstance(type, true) as
                        ISharpModule;
                    if (module != null)
                    {
                        _modules.Add(module);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"SharpWindow 无法创建模块 {type.FullName}: " +
                        exception.Message);
                }
            }

            _modules.Sort(CompareModules);

            foreach (ISharpModule module in _modules)
            {
                string tabName = GetTabName(module);
                SharpTab tab = FindTab(tabName);
                if (tab == null)
                {
                    tab = new SharpTab { Name = tabName };
                    _tabs.Add(tab);
                }

                tab.Modules.Add(module);
            }

            _tabNames = new string[_tabs.Count];
            string selectedTabName = SessionState.GetString(
                SelectedTabSessionKey,
                string.Empty);
            _selectedTabIndex = _tabs.Count > 0 ? 0 : -1;
            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabNames[i] = _tabs[i].Name;
                if (_tabs[i].Name == selectedTabName)
                {
                    _selectedTabIndex = i;
                }
            }
        }

        private void DrawTabModules(SharpTab tab)
        {
            bool drawModuleHeader = tab.Modules.Count > 1 ||
                tab.Modules[0].Name != tab.Name;
            for (int i = 0; i < tab.Modules.Count; i++)
            {
                ISharpModule module = tab.Modules[i];
                if (drawModuleHeader)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(
                            module.Name,
                            EditorStyles.boldLabel);
                        EditorGUILayout.Space(2f);
                        module.OnGUI();
                    }
                }
                else
                {
                    module.OnGUI();
                }

                if (i < tab.Modules.Count - 1)
                {
                    EditorGUILayout.Space(6f);
                }
            }
        }

        private static int CompareModules(
            ISharpModule left,
            ISharpModule right)
        {
            int tabComparison = string.Compare(
                GetTabName(left),
                GetTabName(right),
                StringComparison.Ordinal);
            return tabComparison != 0
                ? tabComparison
                : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
        }

        private static string GetTabName(ISharpModule module)
        {
            if (module is ISharpTabModule tabModule &&
                !string.IsNullOrWhiteSpace(tabModule.TabName))
            {
                return tabModule.TabName;
            }

            return module.Name;
        }

        private SharpTab FindTab(string tabName)
        {
            foreach (SharpTab tab in _tabs)
            {
                if (tab.Name == tabName)
                {
                    return tab;
                }
            }

            return null;
        }

        private void SelectTab(int tabIndex)
        {
            if (tabIndex < 0 ||
                tabIndex >= _tabs.Count ||
                tabIndex == _selectedTabIndex)
            {
                return;
            }

            _selectedTabIndex = tabIndex;
            SessionState.SetString(
                SelectedTabSessionKey,
                _tabs[tabIndex].Name);
            GUI.FocusControl(null);
        }
    }
}
