using System;
using System.Collections.Generic;
using UnityEngine;

namespace Twenty2.VomitLib.Editor.Monitor
{
    #region View Info

    /// <summary>
    /// View信息数据类
    /// </summary>
    [System.Serializable]
    public class ViewInfo
    {
        public string Name;
        public int SortOrder;
        public bool IsVisible;
        public bool IsHidden;
        public bool IsCache;
        public bool IsPreloaded;
        public bool HasMask;
        public GameObject GameObject;
    }

    /// <summary>
    /// View统计信息
    /// </summary>
    [System.Serializable]
    public class ViewStatistics
    {
        public int VisibleCount;
        public int HiddenCount;
        public int PreloadedCount;
        public int TotalCount;

        public override string ToString()
        {
            return $"Total: {TotalCount} (Visible: {VisibleCount}, Hidden: {HiddenCount}, Preloaded: {PreloadedCount})";
        }
    }

    #endregion

    #region Model Info

    /// <summary>
    /// Model信息
    /// </summary>
    [System.Serializable]
    public class ModelInfo
    {
        public string Name;
        public Type Type;
        public string FullTypeName;
        public bool IsInitialized;
        public int FieldCount;
        public object Instance;
    }

    #endregion

    #region System Info

    /// <summary>
    /// System信息
    /// </summary>
    [System.Serializable]
    public class SystemInfo
    {
        public string Name;
        public Type Type;
        public string FullTypeName;
        public bool IsInitialized;
        public int FieldCount;
        public int MethodCount;
        public object Instance;
    }

    #endregion

    #region Event Info

    /// <summary>
    /// Event信息
    /// </summary>
    [System.Serializable]
    public class EventInfo
    {
        public Type EventType;
        public string EventTypeName;
        public string FullTypeName;
        public int ListenerCount;
        public bool IsStruct;
        public string AssemblyName;
    }

    #endregion

    #region Command Info

    /// <summary>
    /// Command信息
    /// </summary>
    [System.Serializable]
    public class CommandInfo
    {
        public Type CommandType;
        public string CommandTypeName;
        public string FullTypeName;
        public string AssemblyName;
        public bool HasReturnValue;
        public Type ReturnType;
        public Type CommandInterfaceType;
        public bool CanInstantiate;
        public Type ParentInterfaceType;
        public bool IsNestedInInterface;
    }

    /// <summary>
    /// Command执行结果
    /// </summary>
    [System.Serializable]
    public class CommandExecutionResult
    {
        public Type CommandType;
        public TimeSpan ExecutionTime;
        public bool Success;
        public object Result;
        public Exception Exception;
        public DateTime Timestamp;
    }

    #endregion

    #region Procedure Info

    /// <summary>
    /// Procedure信息
    /// </summary>
    public class ProcedureInfo
    {
        public string Name { get; set; }
        public string StateName { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Procedure转换记录
    /// </summary>
    public class ProcedureTransition
    {
        public string FromState { get; set; }
        public string ToState { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Luban Info

    /// <summary>
    /// Luban数据表信息
    /// </summary>
    [System.Serializable]
    public class LubanTableInfo
    {
        public string TableName;
        public Type TableType;
        public string TableTypeName;
        public Type DataType;
        public string DataTypeName;
        public int DataCount;
        public int KeyCount;
        public string AssemblyName;
        public DateTime LastUpdateTime = DateTime.MinValue;
    }

    #endregion

    #region Statistics

    /// <summary>
    /// 概览统计信息
    /// </summary>
    public class OverviewStatistics
    {
        public int ViewCount { get; set; }
        public int VisibleViewCount { get; set; }
        public int ModelCount { get; set; }
        public int SystemCount { get; set; }
        public int EventCount { get; set; }
        public int CommandCount { get; set; }
        public int ProcedureCount { get; set; }
        public int LubanTableCount { get; set; }
    }

    #endregion

    #region Field/Property Info

    /// <summary>
    /// 字段/属性信息
    /// </summary>
    public class FieldPropertyInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool IsProperty { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool IsPublic { get; set; }
    }

    #endregion
}
