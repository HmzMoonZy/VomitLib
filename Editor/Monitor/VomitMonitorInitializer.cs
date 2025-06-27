using UnityEditor;

namespace Twenty2.VomitLib.Editor.Monitor
{
    /// <summary>
    /// VomitMonitor初始化器 - 确保VomitMonitorManager在Editor启动时被初始化
    /// </summary>
    [InitializeOnLoad]
    public static class VomitMonitorInitializer
    {
        static VomitMonitorInitializer()
        {
            // 确保VomitMonitorManager被初始化
            _ = VomitMonitorManager.IsMonitorActive;
        }
    }
}