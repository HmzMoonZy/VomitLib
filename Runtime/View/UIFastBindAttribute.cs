using System;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// 标记一个 [SerializeField] 字段为自动绑定字段。
    /// 由编辑器工具(VomitLib/ViewTool)读取,在 Inspector 中填充引用。
    /// 运行时不读取此特性 —— 绑定结果落到 [SerializeField] 字段,运行时即为序列化引用。
    /// </summary>
    /// <remarks>
    /// 两种形式:
    /// <list type="bullet">
    /// <item>带参 <c>[UIFastBind("%TextInitiative")]</c>:按唯一名在子树查找。节点名需与字符串全匹配(含 "%" 前缀)。
    /// "%" 前缀是"被脚本强引用"的视觉标识 —— Hierarchy 中见到 "%Xxx" 节点即知其不可随意改名/删除/移动。</item>
    /// <item>无参 <c>[UIFastBind]</c>:绑定到"当前组件所在 GameObject"上、字段类型对应的组件(自身节点 GetComponent,不查子树)。
    /// 用于引用本节点上的 RectTransform / CanvasGroup / GameObject 等。</item>
    /// </list>
    /// 工作流:声明字段 → 编译 → 在 Hierarchy 右键节点 → "VomitLib/UI Fast Bind"。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class UIFastBindAttribute : PropertyAttribute
    {
        /// <summary>
        /// 待查找的节点名(含 "%" 前缀,与 Hierarchy 节点名完全一致)。
        /// 无参时为 null,表示绑定到当前组件所在 GameObject 自身。
        /// </summary>
        public string NodeName { get; }

        /// <summary>无参:绑定到当前组件所在 GameObject 自身(自身节点 GetComponent,不查子树)。</summary>
        public UIFastBindAttribute()
        {
            NodeName = null;
        }

        /// <param name="name">节点全名,如 "%TextInitiative"。</param>
        public UIFastBindAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("UIFastBind name 不能为空", nameof(name));
            }

            NodeName = name;
        }
    }
}
