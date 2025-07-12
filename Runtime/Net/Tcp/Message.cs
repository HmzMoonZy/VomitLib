using MessagePack;

[MessagePackObject(true)]
public class Message
{
    /// <summary>
    /// 消息唯一id
    /// </summary>
    public int UniId { get; set; }
    [IgnoreMember]
    public virtual int MsgId { get; }
}

public static class MessageExtensions
{
    public static T GetMessage<T>(this Message msg) where T : Message
    {
        return msg as T;
    }
}


