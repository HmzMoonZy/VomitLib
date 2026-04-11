using UnityEngine;

namespace Twenty2.VomitLib.EngineKit
{
    /// <summary>
    /// 简单的DontDestroyOnLoad组件
    /// 挂载此组件的GameObject将在场景切换时不被销毁
    /// </summary>
    public class DontDestroyOnLoad : MonoBehaviour
    {
        [Header("DontDestroyOnLoad设置")]
        [SerializeField] private bool _preventDuplicates = true;
        
        private void Awake()
        {
            // 防止重复实例（可选）
            if (_preventDuplicates)
            {
                // 检查是否已存在同名对象
                var existingObjects = FindObjectsByType<DontDestroyOnLoad>(FindObjectsSortMode.None);
                foreach (var obj in existingObjects)
                {
                    if (obj != this && obj.gameObject.name == gameObject.name)
                    {
                        Log.Warning($"[DontDestroyOnLoad] 发现重复对象: {gameObject.name}, 销毁新创建的实例");
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            
            // 标记为不销毁
            DontDestroyOnLoad(gameObject);
            Log.Debug($"[DontDestroyOnLoad] 对象已标记为不销毁: {gameObject.name}");
        }
    }
}