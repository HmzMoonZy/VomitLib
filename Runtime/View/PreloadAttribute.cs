using System;

namespace Twenty2.VomitLib.View
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PreloadAttribute : Attribute
    {
        public bool IsHide;
    }
}