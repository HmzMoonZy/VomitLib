using System;

namespace Twenty2.VomitLib.LubanSupport
{
    public enum LubanFormat
    {
        [Obsolete("请使用 NewtonsoftJson")]
        SimpleJson,
            
        NewtonsoftJson,
            
        Bin,
    }
}