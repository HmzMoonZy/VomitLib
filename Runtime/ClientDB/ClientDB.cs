namespace Twenty2.VomitLib.ClientDB
{
    /// <summary>
    /// 基于 Luban 的本地数据库.
    /// </summary>
    /// <typeparam name="TTable">Luban 生成的 Tables 类型.</typeparam>
    public static class ClientDB<TTable>
    {
        /// <summary>
        /// 本地数据库(只读)
        /// </summary>
        public static TTable T;

        /// <summary>
        /// 数据库是否可用
        /// </summary>
        private static bool _isValid;

        public static bool IsValid => _isValid;

        public static void Init(object t)
        {
            T = (TTable) t;
            _isValid = true;
        }
    }
}