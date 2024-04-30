using System;

namespace Twenty2.VomitLib.View
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewComponentAttribute : Attribute
    {
        public Type[] ParentTypes;

        public ViewComponentAttribute(params Type[] parentType)
        {
            ParentTypes = parentType;
        }
    }
}