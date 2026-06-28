using System;
using UnityEngine;

namespace Twenty2.VomitLib.View
{
    /// <summary>
    /// 标记一个 [SerializeField] 字段为"预制体唯一名"自动绑定字段。
    /// 由编辑器工具(VomitLib/ViewTool)读取,在 Inspector 中填充引用。
    /// 运行时不读取此特性 —— 绑定结果落到 [SerializeField] 字段,运行时即为序列化引用。
    /// </summary>
    /// <remarks>
    /// 全字符串匹配:节点名必须与传入字符串完全一致(含 "%" 前缀)。
    /// "%" 前缀是"被脚本强引用"的视觉标识 —— Hierarchy 中见到 "%Xxx" 节点即知其不可随意改名/删除/移动。
    ///
    /// 用法示例(节点名需叫 "%TextInitiative"):
    /// <code>
    /// [SerializeField, UIFastBind("%TextInitiative")] private TextMeshProUGUI _textInitiative;
    /// </code>
    /// 工作流:声明字段 → 编译 → 在 Hierarchy 右键节点 → "VomitLib/UI Fast Bind"。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class UIFastBindAttribute : PropertyAttribute
    {
        /// <summary>
        /// 待查找的节点名(含 "%" 前缀,与 Hierarchy 节点名完全一致)。
        /// </summary>
        public string NodeName { get; }

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
