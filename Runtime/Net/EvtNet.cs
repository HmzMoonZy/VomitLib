namespace Twenty2.VomitLib.Net
{
    public abstract class EvtNet
    {
        /// <summary>
        /// 收到了一个消息
        /// </summary>
        public struct RecvMsg
        {
            public int MsgId;
            
            public Message Msg;
        }
    }
}