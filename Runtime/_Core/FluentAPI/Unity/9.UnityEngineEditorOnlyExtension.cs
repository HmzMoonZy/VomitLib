/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using UnityEngine;
using Twenty2.VomitLib.EngineKit;

namespace FluentAPI
{
    /// <summary>
    /// EditorOnly组件的FluentAPI扩展
    /// 提供链式调用语法，编辑器下生效，发布版本安全忽略
    /// </summary>
    public static class UnityEngineEditorOnlyExtension
    {
        #region GameObject Extensions

        /// <summary>
        /// 为GameObject添加EditorOnly组件并设置标签
        /// </summary>
        public static EditorOnly AddEditorLabel(this GameObject selfObj, string label = "Editor Only")
        {
#if UNITY_EDITOR
            var editorOnly = selfObj.GetComponent<EditorOnly>();
            if (editorOnly == null)
            {
                editorOnly = selfObj.AddComponent<EditorOnly>();
            }
            editorOnly.SetLabel(label);
            return editorOnly;
#else
            return null;
#endif
        }

        /// <summary>
        /// 设置GameObject的EditorOnly标签文本
        /// </summary>
        public static GameObject EditorLabel(this GameObject selfObj, string label)
        {
#if UNITY_EDITOR
            var editorOnly = selfObj.GetComponent<EditorOnly>();
            if (editorOnly != null)
            {
                editorOnly.SetLabel(label);
            }
#endif
            return selfObj;
        }

        /// <summary>
        /// 设置GameObject的EditorOnly标签颜色
        /// </summary>
        public static GameObject EditorLabelColor(this GameObject selfObj, Color color)
        {
#if UNITY_EDITOR
            var editorOnly = selfObj.GetComponent<EditorOnly>();
            if (editorOnly != null)
            {
                editorOnly.SetLabelColor(color);
            }
#endif
            return selfObj;
        }

        /// <summary>
        /// 设置GameObject的EditorOnly标签偏移
        /// </summary>
        public static GameObject EditorLabelOffset(this GameObject selfObj, Vector3 offset)
        {
#if UNITY_EDITOR
            var editorOnly = selfObj.GetComponent<EditorOnly>();
            if (editorOnly != null)
            {
                editorOnly.SetLabelOffset(offset);
            }
#endif
            return selfObj;
        }

        /// <summary>
        /// 设置GameObject的EditorOnly背景颜色
        /// </summary>
        public static GameObject EditorBackground(this GameObject selfObj, Color backgroundColor)
        {
#if UNITY_EDITOR
            var editorOnly = selfObj.GetComponent<EditorOnly>();
            if (editorOnly != null)
            {
                editorOnly.SetBackgroundColor(backgroundColor);
            }
#endif
            return selfObj;
        }

        /// <summary>
        /// 设置GameObject的EditorOnly背景显示状态
        /// </summary>
        public static GameObject EditorShowBackground(this GameObject selfObj, bool show)
        {
#if UNITY_EDITOR
            var editorOnly = selfObj.GetComponent<EditorOnly>();
            if (editorOnly != null)
            {
                editorOnly.SetShowBackground(show);
            }
#endif
            return selfObj;
        }

        #endregion

        #region Component Extensions

        /// <summary>
        /// 为Component的GameObject添加EditorOnly组件并设置标签
        /// </summary>
        public static T AddEditorLabel<T>(this T selfComponent, string label = "Editor Only") where T : Component
        {
#if UNITY_EDITOR
            selfComponent.gameObject.AddEditorLabel(label);
#endif
            return selfComponent;
        }

        /// <summary>
        /// 设置Component的GameObject的EditorOnly标签文本
        /// </summary>
        public static T EditorLabel<T>(this T selfComponent, string label) where T : Component
        {
#if UNITY_EDITOR
            selfComponent.gameObject.EditorLabel(label);
#endif
            return selfComponent;
        }

        /// <summary>
        /// 设置Component的GameObject的EditorOnly标签颜色
        /// </summary>
        public static T EditorLabelColor<T>(this T selfComponent, Color color) where T : Component
        {
#if UNITY_EDITOR
            selfComponent.gameObject.EditorLabelColor(color);
#endif
            return selfComponent;
        }

        /// <summary>
        /// 设置Component的GameObject的EditorOnly标签偏移
        /// </summary>
        public static T EditorLabelOffset<T>(this T selfComponent, Vector3 offset) where T : Component
        {
#if UNITY_EDITOR
            selfComponent.gameObject.EditorLabelOffset(offset);
#endif
            return selfComponent;
        }

        /// <summary>
        /// 设置Component的GameObject的EditorOnly背景颜色
        /// </summary>
        public static T EditorBackground<T>(this T selfComponent, Color backgroundColor) where T : Component
        {
#if UNITY_EDITOR
            selfComponent.gameObject.EditorBackground(backgroundColor);
#endif
            return selfComponent;
        }

        /// <summary>
        /// 设置Component的GameObject的EditorOnly背景显示状态
        /// </summary>
        public static T EditorShowBackground<T>(this T selfComponent, bool show) where T : Component
        {
#if UNITY_EDITOR
            selfComponent.gameObject.EditorShowBackground(show);
#endif
            return selfComponent;
        }

        #endregion

        #region EditorOnly Direct Extensions

        /// <summary>
        /// 设置EditorOnly标签文本（链式调用）
        /// </summary>
        public static EditorOnly Label(this EditorOnly selfEditorOnly, string label)
        {
#if UNITY_EDITOR
            if (selfEditorOnly != null)
            {
                selfEditorOnly.SetLabel(label);
            }
#endif
            return selfEditorOnly;
        }

        /// <summary>
        /// 设置EditorOnly标签颜色（链式调用）
        /// </summary>
        public static EditorOnly LabelColor(this EditorOnly selfEditorOnly, Color color)
        {
#if UNITY_EDITOR
            if (selfEditorOnly != null)
            {
                selfEditorOnly.SetLabelColor(color);
            }
#endif
            return selfEditorOnly;
        }

        /// <summary>
        /// 设置EditorOnly标签偏移（链式调用）
        /// </summary>
        public static EditorOnly LabelOffset(this EditorOnly selfEditorOnly, Vector3 offset)
        {
#if UNITY_EDITOR
            if (selfEditorOnly != null)
            {
                selfEditorOnly.SetLabelOffset(offset);
            }
#endif
            return selfEditorOnly;
        }

        /// <summary>
        /// 设置EditorOnly背景颜色（链式调用）
        /// </summary>
        public static EditorOnly BackgroundColor(this EditorOnly selfEditorOnly, Color backgroundColor)
        {
#if UNITY_EDITOR
            if (selfEditorOnly != null)
            {
                selfEditorOnly.SetBackgroundColor(backgroundColor);
            }
#endif
            return selfEditorOnly;
        }

        /// <summary>
        /// 设置EditorOnly背景显示状态（链式调用）
        /// </summary>
        public static EditorOnly ShowBackground(this EditorOnly selfEditorOnly, bool show)
        {
#if UNITY_EDITOR
            if (selfEditorOnly != null)
            {
                selfEditorOnly.SetShowBackground(show);
            }
#endif
            return selfEditorOnly;
        }

        /// <summary>
        /// 设置EditorOnly背景内边距（链式调用）
        /// </summary>
        public static EditorOnly BackgroundPadding(this EditorOnly selfEditorOnly, Vector2 padding)
        {
#if UNITY_EDITOR
            if (selfEditorOnly != null)
            {
                selfEditorOnly.SetBackgroundPadding(padding);
            }
#endif
            return selfEditorOnly;
        }

        #endregion
    }
}