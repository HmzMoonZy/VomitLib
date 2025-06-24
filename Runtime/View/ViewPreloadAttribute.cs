using System;

namespace Twenty2.VomitLib.View
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewPreloadAttribute : Attribute
    {
        /// <summary>                                                                            
        /// 预加载优先级（数字越小优先级越高）                                                   
        /// </summary>                                                                           
        public int Priority { get; } = 0;

        /// <summary>
        /// 加载路径
        /// </summary>
        public string ResourcePath;
        
        public ViewPreloadAttribute(string resourcePath)
        {
            ResourcePath = resourcePath;
        }

        public ViewPreloadAttribute(string resourcePath, int priority)
        {
            ResourcePath = resourcePath;
            Priority = priority;
        }


    }
}