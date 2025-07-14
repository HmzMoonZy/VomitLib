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

        public ViewPreloadAttribute()
        {
            
        }

        public ViewPreloadAttribute(int priority)
        {
            Priority = priority;
        }


    }
}