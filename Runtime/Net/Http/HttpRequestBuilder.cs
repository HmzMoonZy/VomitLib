using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace VomitLib.Net.Http
{
    /// <summary>
    /// HTTP请求类型枚举
    /// </summary>
    public enum HttpRequestType
    {
        GET,
        POST_JSON,
        POST_FORM
    }

    /// <summary>
    /// HTTP请求构造器
    /// 支持链式调用和自动签名
    /// </summary>
    public class HttpRequestBuilder
    {
        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();
        private HttpConfig _config;
        private HttpRequestType _requestType = HttpRequestType.GET;
        private int _timeout = 10;
        private bool _enableSignature = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">HTTP配置</param>
        public HttpRequestBuilder(HttpConfig config = null)
        {
            _config = config ?? HttpSystem.Config;
            if (_config != null)
            {
                _timeout = _config.Timeout;
                _enableSignature = _config.EnableSignature;
            }
        }

        /// <summary>
        /// 设置命令参数
        /// </summary>
        /// <param name="command">命令名称</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder SetCommand(string command)
        {
            _parameters["command"] = command;
            return this;
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="key">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder AddParameter(string key, string value)
        {
            _parameters[key] = value;
            return this;
        }

        /// <summary>
        /// 添加多个参数
        /// </summary>
        /// <param name="parameters">参数字典</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder AddParameters(Dictionary<string, string> parameters)
        {
            foreach (var param in parameters)
            {
                _parameters[param.Key] = param.Value;
            }
            return this;
        }

        /// <summary>
        /// 添加对象参数（通过反射）
        /// </summary>
        /// <param name="obj">参数对象</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder AddParameters(object obj)
        {
            if (obj == null) return this;

            var type = obj.GetType();
            var properties = type.GetProperties();
            
            foreach (var prop in properties)
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    _parameters[prop.Name] = value.ToString();
                }
            }
            return this;
        }

        /// <summary>
        /// 添加请求头
        /// </summary>
        /// <param name="key">头名称</param>
        /// <param name="value">头值</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder AddHeader(string key, string value)
        {
            _headers[key] = value;
            return this;
        }

        /// <summary>
        /// 设置请求类型
        /// </summary>
        /// <param name="type">请求类型</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder SetRequestType(HttpRequestType type)
        {
            _requestType = type;
            return this;
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        /// <param name="timeout">超时时间（秒）</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder SetTimeout(int timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// 设置是否启用签名
        /// </summary>
        /// <param name="enable">是否启用</param>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder SetEnableSignature(bool enable)
        {
            _enableSignature = enable;
            return this;
        }

        /// <summary>
        /// 自动添加签名参数
        /// </summary>
        /// <returns>构造器实例</returns>
        public HttpRequestBuilder WithSignature()
        {
            if (!_enableSignature || _config == null || _config.IsDebugMode)
                return this;

            string timestamp = DateTime.Now.Ticks.ToString();
            string token = HttpSignHelper.GenerateSign(_config.HttpCode, timestamp);
            
            _parameters["timestamp"] = timestamp;
            _parameters["token"] = token;
            
            return this;
        }

        /// <summary>
        /// 构造UnityWebRequest
        /// </summary>
        /// <returns>UnityWebRequest实例</returns>
        public UnityWebRequest BuildRequest()
        {
            if (_config == null)
                throw new InvalidOperationException("HttpConfig未设置");

            // 自动添加签名
            WithSignature();

            UnityWebRequest request = null;
            
            switch (_requestType)
            {
                case HttpRequestType.GET:
                    request = BuildGetRequest();
                    break;
                case HttpRequestType.POST_JSON:
                    request = BuildPostJsonRequest();
                    break;
                case HttpRequestType.POST_FORM:
                    request = BuildPostFormRequest();
                    break;
            }

            if (request != null)
            {
                request.timeout = _timeout;
                
                // 添加自定义头
                foreach (var header in _headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            return request;
        }

        /// <summary>
        /// 发送请求（泛型版本）
        /// </summary>
        /// <typeparam name="T">响应数据类型</typeparam>
        /// <returns>响应结果</returns>
        public async UniTask<HttpResponse<T>> SendAsync<T>()
        {
            return await HttpSystem.SendAsync<T>(this);
        }

        /// <summary>
        /// 发送请求（字符串版本）
        /// </summary>
        /// <returns>响应结果</returns>
        public async UniTask<HttpResponse<string>> SendAsync()
        {
            return await HttpSystem.SendAsync<string>(this);
        }

        /// <summary>
        /// 构造GET请求
        /// </summary>
        /// <returns>UnityWebRequest实例</returns>
        private UnityWebRequest BuildGetRequest()
        {
            StringBuilder urlBuilder = new StringBuilder(_config.ServerUrl);
            urlBuilder.Append("?");
            
            bool first = true;
            foreach (var param in _parameters)
            {
                if (!first) urlBuilder.Append("&");
                urlBuilder.Append(param.Key).Append("=").Append(UnityWebRequest.EscapeURL(param.Value));
                first = false;
            }
            
            return UnityWebRequest.Get(urlBuilder.ToString());
        }

        /// <summary>
        /// 构造POST JSON请求
        /// </summary>
        /// <returns>UnityWebRequest实例</returns>
        private UnityWebRequest BuildPostJsonRequest()
        {
            var request = new UnityWebRequest(_config.ServerUrl, "POST");
            
            // 构造JSON数据
            var jsonData = JsonUtility.ToJson(new SerializableDictionary(_parameters));
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            return request;
        }

        /// <summary>
        /// 构造POST Form请求
        /// </summary>
        /// <returns>UnityWebRequest实例</returns>
        private UnityWebRequest BuildPostFormRequest()
        {
            WWWForm form = new WWWForm();
            foreach (var param in _parameters)
            {
                form.AddField(param.Key, param.Value);
            }
            
            return UnityWebRequest.Post(_config.ServerUrl, form);
        }
    }

    /// <summary>
    /// 序列化字典（用于JSON序列化）
    /// </summary>
    [Serializable]
    public class SerializableDictionary
    {
        public List<KeyValuePair> items = new List<KeyValuePair>();
        
        public SerializableDictionary(Dictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                items.Add(new KeyValuePair { key = kvp.Key, value = kvp.Value });
            }
        }
    }

    /// <summary>
    /// 键值对
    /// </summary>
    [Serializable]
    public class KeyValuePair
    {
        public string key;
        public string value;
    }
}