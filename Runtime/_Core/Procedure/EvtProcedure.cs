using QFramework;

namespace Twenty2.VomitLib.Procedure
{
    public abstract class EvtProcedure
    {
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public struct Changed
        {
            public string FromStateId;
            public string ToStateId;
            public IProcedureState FromState;
            public IProcedureState ToState;
        }
    }
}