using Twenty2.VomitLib.Procedure;
using UnityEditor;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Procedure
{
    /// <summary>
    /// Procedure系统的Unity菜单项
    /// </summary>
    public static class ProcedureMenuItems
    {
        /// <summary>
        /// 创建Procedure监控器GameObject
        /// </summary>
        [MenuItem("GameObject/VomitLib/Procedure Monitor", false, 10)]
        public static void CreateProcedureMonitor()
        {
            var go = new GameObject("ProcedureMonitor");
            go.AddComponent<ProcedureMonitor>();
            
            // 设置父级（如果有选中的GameObject）
            if (Selection.activeGameObject != null)
            {
                go.transform.SetParent(Selection.activeGameObject.transform);
            }
            
            // 选中新创建的对象
            Selection.activeGameObject = go;
            
            // 确保在场景视图中可见
            EditorGUIUtility.PingObject(go);
            
            Debug.Log("已创建Procedure监控器，请在Inspector中指定要监控的状态机类型（如：Game+State）");
        }
        
        /// <summary>
        /// 验证菜单项是否可用
        /// </summary>
        [MenuItem("GameObject/VomitLib/Procedure Monitor", true)]
        public static bool ValidateCreateProcedureMonitor()
        {
            return true;
        }
    }
}