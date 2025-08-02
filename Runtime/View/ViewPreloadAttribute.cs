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

        public bool AlwaysDisplay { get; private set; } = false;

        public ViewPreloadAttribute(int priority = 0, bool alwaysDisplay = false)
        {
            AlwaysDisplay = alwaysDisplay;
            Priority = 0;
        }
    }
}