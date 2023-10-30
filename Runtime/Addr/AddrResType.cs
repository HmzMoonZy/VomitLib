using System;
using QFramework;

namespace Twenty2.VomitLib.Addr
{
    /// <summary>
    /// 囊括了常用的需要动态加载的资源类型.
    /// </summary>
    public struct AddrResType
    {
        public readonly Type Type;

        public readonly int SubType;
        
        public AddrResType(Type type, object subType)
        {
            try
            {
                Type = type;
                SubType = (int)subType;
            }
            catch (Exception e)
            {
                LogKit.E("创建Addr子类型时出错.");
                throw;
            }
        }
    }
}