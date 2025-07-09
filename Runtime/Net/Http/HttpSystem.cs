using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Twenty2.VomitLib;

namespace VomitLib.Net.Http
{
    /// <summary>
    /// HTTP客户端管理器
    /// 静态类，提供统一的HTTP请求接口
    /// </summary>
    public static class HttpSystem
    {
        private static HttpConfig _config;
        private static readonly object _lock = new object();

        /// <summary>
        /// HTTP配置
        /// </summary>
        public static HttpConfig Config => _config;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized => _config != null;

        /// <summary>
        /// 初始化HTTP客户端
        /// </summary>
        /// <param name="config">HTTP配置</param>
        public static void Initialize(HttpConfig config)
        {
            lock (_lock)
            {
                _config = config;
                
                if (_config != null && !_config.ValidateConfig())
                {
                    Log.Error("HttpClient: 配置验证失败");
                    return;
                }

                if (_config?.LogRequests == true)
                {
                    Log.Info($"HttpClient: 初始化完成，服务器地址：{_config.ServerUrl}");
                }
            }
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        /// <param name="command">命令名称</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>响应结果</returns>
        public static async UniTask<HttpResponse<string>> GetAsync(string command, object parameters = null)
        {
            var builder = new HttpRequestBuilder(_config)
                .SetCommand(command)
                .SetRequestType(HttpRequestType.GET);

            if (parameters != null)
                builder.AddParameters(parameters);

            return await SendAsync<string>(builder);
        }

        /// <summary>
        /// 发送GET请求（泛型版本）
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="command">命令名称</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>响应结果</returns>
        public static async UniTask<HttpResponse<T>> GetAsync<T>(string command, object parameters = null)
        {
            var builder = new HttpRequestBuilder(_config)
                .SetCommand(command)
                .SetRequestType(HttpRequestType.GET);

            if (parameters != null)
                builder.AddParameters(parameters);

            return await SendAsync<T>(builder);
        }

        /// <summary>
        /// 发送POST JSON请求
        /// </summary>
        /// <param name="command">命令名称</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>响应结果</returns>
        public static async UniTask<HttpResponse<string>> PostJsonAsync(string command, object parameters = null)
        {
            var builder = new HttpRequestBuilder(_config)
                .SetCommand(command)
                .SetRequestType(HttpRequestType.POST_JSON);

            if (parameters != null)
                builder.AddParameters(parameters);

            return await SendAsync<string>(builder);
        }

        /// <summary>
        /// 发送POST JSON请求（泛型版本）
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="command">命令名称</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>响应结果</returns>
        public static async UniTask<HttpResponse<T>> PostJsonAsync<T>(string command, object parameters = null)
        {
            var builder = new HttpRequestBuilder(_config)
                .SetCommand(command)
                .SetRequestType(HttpRequestType.POST_JSON);

            if (parameters != null)
                builder.AddParameters(parameters);

            return await SendAsync<T>(builder);
        }

        /// <summary>
        /// 发送POST Form请求
        /// </summary>
        /// <param name="command">命令名称</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>响应结果</returns>
        public static async UniTask<HttpResponse<string>> PostFormAsync(string command, object parameters = null)
        {
            var builder = new HttpRequestBuilder(_config)
                .SetCommand(command)
                .SetRequestType(HttpRequestType.POST_FORM);

            if (parameters != null)
                builder.AddParameters(parameters);

            return await SendAsync<string>(builder);
        }

        /// <summary>
        /// 发送POST Form请求（泛型版本）
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="command">命令名称</param>
        /// <param name="parameters">参数对象</param>
        /// <returns>响应结果</returns>
        public static async UniTask<HttpResponse<T>> PostFormAsync<T>(string command, object parameters = null)
        {
            var builder = new HttpRequestBuilder(_config)
                .SetCommand(command)
                .SetRequestType(HttpRequestType.POST_FORM);

            if (parameters != null)
                builder.AddParameters(parameters);

            return await SendAsync<T>(builder);
        }

        /// <summary>
        /// 发送请求（使用构造器）
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <param name="builder">请求构造器</param>
        /// <returns>响应结果</returns>
        public static async UniTask<HttpResponse<T>> SendAsync<T>(HttpRequestBuilder builder)
        {
            if (_config == null)
            {
                return new HttpResponse<T>(HttpResponseStatus.NetworkError, "HttpClient未初始化");
            }

            var startTime = Time.realtimeSinceStartup;
            var response = new HttpResponse<T>();
            
            try
            {
                // 构造请求
                using (var request = builder.BuildRequest())
                {
                    if (_config.LogRequests)
                    {
                        Debug.Log($"HttpClient: 发送请求 {request.method} {request.url}");
                    }

                    // 发送请求并等待响应
                    await request.SendWebRequest();
                    
                    response.ResponseTime = Time.realtimeSinceStartup - startTime;
                    response.ResponseCode = request.responseCode;
                    response.RawData = request.downloadHandler?.text ?? "";

                    if (_config.LogResponses)
                    {
                        Debug.Log($"HttpClient: 收到响应 [{request.responseCode}] {response.RawData}");
                    }

                    // 检查请求结果
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        response.Status = HttpResponseStatus.Success;
                        response.Data = ParseResponse<T>(response.RawData);
                    }
                    else
                    {
                        response.Status = GetErrorStatus(request.result);
                        response.Message = request.error ?? "Unknown error";
                        
                        if (_config.LogResponses)
                        {
                            Debug.LogError($"HttpClient: 请求失败 {response.Status} - {response.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Status = HttpResponseStatus.NetworkError;
                response.Message = ex.Message;
                response.ResponseTime = Time.realtimeSinceStartup - startTime;
                
                Debug.LogError($"HttpClient: 请求异常 {ex.Message}");
            }

            return response;
        }

        /// <summary>
        /// 解析响应数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="rawData">原始数据</param>
        /// <returns>解析后的数据</returns>
        private static T ParseResponse<T>(string rawData)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)rawData;
                }

                // 首先尝试解析为ServerResponse
                var serverResponse = JsonUtility.FromJson<ServerResponse>(rawData);
                if (serverResponse != null)
                {
                    if (!serverResponse.IsSuccess)
                    {
                        throw new Exception($"服务器返回错误: {serverResponse.message}");
                    }

                    // 如果有数据字段，尝试解析数据
                    if (!string.IsNullOrEmpty(serverResponse.data))
                    {
                        return JsonUtility.FromJson<T>(serverResponse.data);
                    }
                }

                // 直接解析为目标类型
                return JsonUtility.FromJson<T>(rawData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"HttpClient: 解析响应失败 {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 获取错误状态
        /// </summary>
        /// <param name="result">UnityWebRequest结果</param>
        /// <returns>HttpResponseStatus</returns>
        private static HttpResponseStatus GetErrorStatus(UnityWebRequest.Result result)
        {
            switch (result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    return HttpResponseStatus.NetworkError;
                case UnityWebRequest.Result.DataProcessingError:
                    return HttpResponseStatus.ParseError;
                case UnityWebRequest.Result.ProtocolError:
                    return HttpResponseStatus.HttpError;
                default:
                    return HttpResponseStatus.Unknown;
            }
        }
    }
}