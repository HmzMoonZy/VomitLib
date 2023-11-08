namespace Twenty2.VomitLib.ClientDB
{
    public interface IDataRef<out IDType, out DataType>
    {
        /// <summary>
        /// 数据ID
        /// </summary>
        public IDType DataID { get; }

        /// <summary>
        /// 只读的数据
        /// </summary>
        /// <returns></returns>
        public DataType GetData();
    }
}