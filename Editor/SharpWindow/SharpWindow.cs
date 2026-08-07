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
        private readonly List<ISharpModule> _modules = new List<ISharpModule>
        {
            new TimeScaleModule(),
        };

        private Vector2 _scrollPos;

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
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _modules.Count; i++)
            {
                var m = _modules[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(m.Name, EditorStyles.boldLabel);
                EditorGUILayout.Space(2);
                m.OnGUI();
                EditorGUILayout.EndVertical();

                if (i < _modules.Count - 1)
                {
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
