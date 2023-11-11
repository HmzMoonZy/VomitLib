
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class ProcedureAttribute : Attribute
    {
        public object ProcedureID;

        public bool IsEntry;
    }
