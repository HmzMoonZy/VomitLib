using System;
using UnityEngine;

namespace VomitLib.Net.Http
{
    /// <summary>
    /// HTTP响应状态枚举
    /// </summary>
    public enum HttpResponseStatus
    {
        Success,
        NetworkError,
        HttpError,
        ParseError,
        SignError,
        TimeoutError,
        Unknown
    }

    /// <summary>
    /// HTTP响应基类
    /// </summary>
    [Serializable]
    public class HttpResponse
    {
        public HttpResponseStatus Status { get; set; } = HttpResponseStatus.Unknown;
        public string Message { get; set; } = string.Empty;
        public string RawData { get; set; } = string.Empty;
        public long ResponseCode { get; set; }
        public float ResponseTime { get; set; }

        public bool IsSuccess => Status == HttpResponseStatus.Success;
        public bool IsError => Status != HttpResponseStatus.Success;

        public HttpResponse()
        {
        }

        public HttpResponse(HttpResponseStatus status, string message = "")
        {
            Status = status;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Status}] {Message}";
        }
    }

    /// <summary>
    /// 泛型HTTP响应类
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    [Serializable]
    public class HttpResponse<T> : HttpResponse
    {
        public T Data { get; set; }

        public HttpResponse() : base()
        {
        }

        public HttpResponse(T data) : base(HttpResponseStatus.Success)
        {
            Data = data;
        }

        public HttpResponse(HttpResponseStatus status, string message = "") : base(status, message)
        {
        }

        public HttpResponse(HttpResponseStatus status, string message, T data) : base(status, message)
        {
            Data = data;
        }
    }

    /// <summary>
    /// 服务器标准响应格式
    /// 对应GeekServer的HttpResult
    /// </summary>
    [Serializable]
    public class ServerResponse
    {
        public string status;
        public string message;
        public string data;

        public bool IsSuccess => status == "success";
        public bool IsIllegal => status == "illegal";
        public bool IsParamError => status == "param_error";
        public bool IsActionFailed => status == "action_failed";
    }
}