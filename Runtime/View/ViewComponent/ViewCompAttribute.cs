using System;

namespace Twenty2.VomitLib.View
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewCompAttribute : Attribute
    {
        public Type[] ParentTypes;
        
        public ViewCompAttribute(params Type[] viewType)
        {
            ParentTypes = viewType;
        }
    }
}