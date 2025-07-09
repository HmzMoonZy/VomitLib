using UnityEngine;

namespace VomitLib.Net.Http
{
    /// <summary>
    /// HTTP配置ScriptableObject
    /// 用于管理服务器连接配置
    /// </summary>
    [CreateAssetMenu(fileName = "HttpConfig", menuName = "VomitLib/Net/HttpConfig")]
    public class HttpConfig : ScriptableObject
    {
        [Header("服务器配置")]
        [SerializeField] private string serverUrl = "http://127.0.0.1:20000/game/api";
        [SerializeField] private string httpCode = "your_http_code_here";
        [SerializeField] private bool enableSignature = true;
        
        [Header("请求配置")]
        [SerializeField] private int timeout = 10;
        [SerializeField] private int retryCount = 3;
        [SerializeField] private float retryDelay = 1f;
        
        [Header("调试配置")]
        [SerializeField] private bool isDebugMode = false;
        [SerializeField] private bool logRequests = true;
        [SerializeField] private bool logResponses = true;

        /// <summary>
        /// 服务器API地址
        /// </summary>
        public string ServerUrl => serverUrl;

        /// <summary>
        /// HTTP签名密钥
        /// </summary>
        public string HttpCode => httpCode;

        /// <summary>
        /// 是否启用签名验证
        /// </summary>
        public bool EnableSignature => enableSignature;

        /// <summary>
        /// 请求超时时间（秒）
        /// </summary>
        public int Timeout => timeout;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount => retryCount;

        /// <summary>
        /// 重试间隔（秒）
        /// </summary>
        public float RetryDelay => retryDelay;

        /// <summary>
        /// 是否为调试模式
        /// </summary>
        public bool IsDebugMode => isDebugMode;

        /// <summary>
        /// 是否记录请求日志
        /// </summary>
        public bool LogRequests => logRequests;

        /// <summary>
        /// 是否记录响应日志
        /// </summary>
        public bool LogResponses => logResponses;

        /// <summary>
        /// 在运行时设置服务器URL
        /// </summary>
        /// <param name="url">服务器URL</param>
        public void SetServerUrl(string url)
        {
            serverUrl = url;
        }

        /// <summary>
        /// 在运行时设置HttpCode
        /// </summary>
        /// <param name="code">HttpCode</param>
        public void SetHttpCode(string code)
        {
            httpCode = code;
        }

        /// <summary>
        /// 在运行时设置调试模式
        /// </summary>
        /// <param name="debug">是否调试</param>
        public void SetDebugMode(bool debug)
        {
            isDebugMode = debug;
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool ValidateConfig()
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                Debug.LogError("HttpConfig: ServerUrl不能为空");
                return false;
            }

            if (enableSignature && string.IsNullOrEmpty(httpCode))
            {
                Debug.LogError("HttpConfig: 启用签名验证时HttpCode不能为空");
                return false;
            }

            if (timeout <= 0)
            {
                Debug.LogError("HttpConfig: Timeout必须大于0");
                return false;
            }

            if (retryCount < 0)
            {
                Debug.LogError("HttpConfig: RetryCount不能小于0");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            // 在编辑器中验证配置
            if (timeout <= 0)
                timeout = 10;
            
            if (retryCount < 0)
                retryCount = 0;
            
            if (retryDelay < 0)
                retryDelay = 1f;
        }
    }
}