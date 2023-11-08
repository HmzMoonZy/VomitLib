using System;

namespace Twenty2.VomitLib.ClientDB
{
    public interface ICustomTableItem : IComparable
    {
        public int GetID();

        public string GetName();
    }
}