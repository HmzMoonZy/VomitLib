#if UNITY_EDITOR
using UnityEngine;

namespace Twenty2.VomitLib.Monitor
{
    /// <summary>
    /// VomitMonitor根组件 - 管理View和Procedure监控器（仅Editor模式）
    /// </summary>
    public class VomitMonitor : MonoBehaviour
    {
        [Header("VomitLib监控器")]
        [SerializeField, TextArea(3, 5)]
        private string _description = "VomitLib监控系统根节点\n自动管理ViewMonitor和ProcedureMonitor子组件\n仅在Editor模式下运行";
        
        [Space(10)]
        [Header("子监控器")]
        [SerializeField] private ViewMonitor _viewMonitor;
        [SerializeField] private ProcedureMonitor _procedureMonitor;
        
        /// <summary>
        /// 获取ViewMonitor
        /// </summary>
        public ViewMonitor ViewMonitor => _viewMonitor;
        
        /// <summary>
        /// 获取ProcedureMonitor
        /// </summary>
        public ProcedureMonitor ProcedureMonitor => _procedureMonitor;
        
        private void Awake()
        {
            // 查找子监控器
            _viewMonitor = GetComponentInChildren<ViewMonitor>();
            _procedureMonitor = GetComponentInChildren<ProcedureMonitor>();
            
            Debug.Log("VomitMonitor根组件已初始化 (Editor模式，切换场景不销毁)");
        }
        
        private void OnEnable()
        {
            // 场景切换后重新验证子监控器
            if (_viewMonitor == null || _procedureMonitor == null)
            {
                _viewMonitor = GetComponentInChildren<ViewMonitor>();
                _procedureMonitor = GetComponentInChildren<ProcedureMonitor>();
            }
        }
        
        /// <summary>
        /// 设置子监控器引用（由VomitMonitorManager调用）
        /// </summary>
        public void SetChildMonitors(ViewMonitor viewMonitor, ProcedureMonitor procedureMonitor)
        {
            _viewMonitor = viewMonitor;
            _procedureMonitor = procedureMonitor;
        }
        
        /// <summary>
        /// 获取监控器状态
        /// </summary>
        public bool IsFullyInitialized => _viewMonitor != null && _procedureMonitor != null;
        
        /// <summary>
        /// 刷新所有子监控器
        /// </summary>
        [ContextMenu("刷新所有监控器")]
        public void RefreshAllMonitors()
        {
            if (_viewMonitor != null)
                _viewMonitor.ManualRefresh();
                
            if (_procedureMonitor != null)
                _procedureMonitor.ManualRefresh();
                
            Debug.Log("所有监控器已刷新");
        }
    }
}
#endif