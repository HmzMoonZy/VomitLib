using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Twenty2.VomitLib.View;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Twenty2.VomitLib.Editor
{
    /// <summary>
    /// UIFastBind 编辑器工具。
    ///
    /// 工作流:在 Hierarchy 右键一个节点 X → "VomitLib/UI Fast Bind",
    /// 工具会扫描 X <b>自身</b>挂载的脚本中所有 [UIFastBind] 字段,
    /// 按"唯一名"在 X 的<b>子树</b>内查找目标节点,并填充 [SerializeField] 引用。
    ///
    /// 关键语义:只改动右键节点 X 上的脚本,不递归处理子树中其他节点的脚本。
    /// 这样右键的意图是明确的 —— "我知道 X 上的脚本需要绑定,只动它"。
    /// </summary>
    public static class UIFastBinderTool
    {
        private const string MenuPath = "GameObject/VomitLib/UI Fast Bind";

        [MenuItem(MenuPath, priority = 20)]
        private static void Bind()
        {
            var target = Selection.activeTransform;
            if (target == null)
            {
                Debug.LogWarning("[UIFastBind] 请先在 Hierarchy 选中一个节点。");
                return;
            }

            int success = 0;
            var failures = new List<string>();

            // 只扫描右键节点自身挂载的脚本(不递归子树中的其他脚本)
            foreach (var behaviour in target.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue; // 缺失脚本的占位
                success += BindComponent(behaviour, target, failures);
            }

            // 汇总报告
            var sb = new StringBuilder();
            sb.AppendLine($"[UIFastBind] '{target.name}' 绑定完成: 成功 {success} 个");
            if (failures.Count > 0)
            {
                sb.AppendLine($"失败 {failures.Count} 个:");
                foreach (var f in failures) sb.AppendLine("  - " + f);
            }
            if (failures.Count > 0) Debug.LogWarning(sb.ToString());
            else Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 绑定单个 MonoBehaviour 上的所有 [UIFastBind] 字段。
        /// 目标节点在 <paramref name="searchRoot"/> 的子树内查找。
        /// </summary>
        /// <returns>成功填充的字段数。</returns>
        private static int BindComponent(MonoBehaviour target, Transform searchRoot, List<string> failures)
        {
            var type = target.GetType();
            var fields = CollectFastBindFields(type);
            if (fields.Count == 0) return 0;

            int success = 0;

            // 预解析所有字段,先确认能绑定多少,再决定是否进入 Undo+修改流程。
            // 这样失败的字段不会污染 Undo 栈,也不会留下半修改状态。
            var pending = new List<(SerializedProperty prop, Object value)>();

            using (var so = new SerializedObject(target))
            {
                foreach (var (field, attr) in fields)
                {
                    if (TryResolve(field, attr, searchRoot, out var resolved, out var error))
                    {
                        var prop = so.FindProperty(field.Name);
                        if (prop != null)
                        {
                            pending.Add((prop, resolved));
                        }
                        else
                        {
                            failures.Add($"{type.Name}.{field.Name} 找不到序列化属性(可能未标注 [SerializeField])");
                        }
                    }
                    else
                    {
                        failures.Add($"{type.Name}.{field.Name} → {error}");
                    }
                }

                if (pending.Count == 0)
                {
                    return 0;
                }

                // 修改前注册 Undo,撤销可恢复到绑定前状态。
                Undo.RegisterCompleteObjectUndo(target, "UI Fast Bind");

                foreach (var (prop, value) in pending)
                {
                    // objectReferenceValue 兼容 GameObject / Component 字段
                    prop.objectReferenceValue = value;
                    success++;
                }

                // ApplyModifiedProperties 会标记场景/Prefab 为 dirty 并持久化修改。
                so.ApplyModifiedProperties();
            }

            return success;
        }

        /// <summary>
        /// 反射收集类型(含继承链)上所有带 [UIFastBind] 的实例字段。
        /// </summary>
        private static List<(FieldInfo field, UIFastBindAttribute attr)> CollectFastBindFields(Type type)
        {
            var result = new List<(FieldInfo field, UIFastBindAttribute attr)>();
            const BindingFlags Flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var f in t.GetFields(Flags))
                {
                    // GetCustomAttribute 沿继承链查(Inherited=true),为避免重复,只收声明在该层的
                    if (!result.Any(x => x.field.Name == f.Name))
                    {
                        var attr = f.GetCustomAttribute<UIFastBindAttribute>();
                        if (attr != null) result.Add((f, attr));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 解析字段引用。
        /// <para>无参 <see cref="UIFastBindAttribute"/>:绑定到 searchRoot 所在 GameObject 自身(GetComponent,不查子树)。</para>
        /// <para>带参:在 searchRoot 的子树内按唯一名查找节点,再取字段类型对应的组件引用。</para>
        /// </summary>
        private static bool TryResolve(
            FieldInfo field, UIFastBindAttribute attr, Transform searchRoot,
            out Object resolved, out string error)
        {
            // 无参:绑定到当前组件所在 GameObject 自身
            if (attr.NodeName == null)
            {
                return TryGetComponentOn(searchRoot.gameObject, field.FieldType, out resolved, out error);
            }

            // 带参:子树唯一名查找
            var matches = new List<Transform>();
            FindByName(searchRoot, attr.NodeName, matches); 

            if (matches.Count == 0)
            {
                resolved = null;
                error = $"子树内找不到名为 '{attr.NodeName}' 的节点";
                return false;
            }

            if (matches.Count > 1)
            {
                resolved = null;
                var paths = string.Join(", ", matches.Select(m => HierarchicalPath(m)));
                error = $"节点名 '{attr.NodeName}' 不唯一({matches.Count} 个: {paths}),请重命名后重试";
                return false;
            }

            var node = matches[0];
            return TryGetComponentOn(node.gameObject, field.FieldType, out resolved, out error);
        }

        /// <summary>
        /// 在指定 GameObject 上取 type 对应的引用。
        /// 字段类型为 GameObject → 给节点本身;为 Component 子类 → GetComponent。
        /// </summary>
        private static bool TryGetComponentOn(GameObject go, Type fieldType, out Object resolved, out string error)
        {
            if (fieldType == typeof(GameObject))
            {
                resolved = go;
                error = null;
                return true;
            }

            var comp = go.GetComponent(fieldType);
            if (comp == null)
            {
                resolved = null;
                error = $"节点 '{go.name}' 上找不到组件 {fieldType.Name}";
                return false;
            }

            resolved = comp;
            error = null;
            return true;
        }

        /// <summary>
        /// 递归查找子树内名为 targetName 的所有 Transform。
        /// </summary>
        private static void FindByName(Transform current, string targetName, List<Transform> results)
        {
            if (current.name == targetName) results.Add(current);
            for (int i = 0; i < current.childCount; i++)
            {
                FindByName(current.GetChild(i), targetName, results);
            }
        }

        /// <summary>
        /// 取节点相对根的可读路径,用于错误报告。
        /// </summary>
        private static string HierarchicalPath(Transform t)
        {
            var stack = new Stack<string>();
            var cur = t;
            while (cur != null)
            {
                stack.Push(cur.name);
                cur = cur.parent;
            }
            return string.Join("/", stack);
        }
    }
}
