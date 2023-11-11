using System;

namespace Twenty2.VomitLib.Procedure
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ProcedureAttribute : Attribute
    {
        public object ProcedureID;

        public bool IsEntry;
    }
}