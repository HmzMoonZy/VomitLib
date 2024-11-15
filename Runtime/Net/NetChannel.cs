using MessagePack;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using QFramework;
using Debug = UnityEngine.Debug;

namespace Twenty2.VomitLib.Net
{
    public class NetChannel
    {
        protected Action<Message> _onMessage;
        protected Action _onClose;
        protected Pipe _recvPipe;
        protected TcpClient _socket;
        protected CancellationTokenSource _closeSrc = new CancellationTokenSource();

        public NetChannel(TcpClient socket, Action<Message> onMessage = null, Action onClose = null)
        {
            this._socket = socket;
            this._onMessage = onMessage;
            this._onClose = onClose;
            _recvPipe = new Pipe();

            _ = StartAsync();
        }

        private async Task StartAsync()
        {
            try
            {
                _ = ReadSocketTask();
                
                while (!IsClose())
                {
                    var result = await _recvPipe.Reader.ReadAsync(_closeSrc.Token);
                    var buffer = result.Buffer;
                    if (buffer.Length > 0)
                    {
                        while (TryParseMessage(ref buffer)) ;
                        _recvPipe.Reader.AdvanceTo(buffer.Start, buffer.End);
                    }
                    else if (result.IsCanceled || result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            Close();
            _onClose?.Invoke();
        }
        
        /// <summary>
        /// 接收数据任务，异步读取套接字流中的数据并写入接收管道
        /// </summary>
        private async Task ReadSocketTask()
        {
            var readBuffer = new byte[2048];
            var writer = _recvPipe.Writer;
            while (!IsClose())
            {
                // 从套接字流中异步读取数据
                var length = await _socket.GetStream().ReadAsync(readBuffer, 0, readBuffer.Length, _closeSrc.Token);
                if (length > 0)
                {
                    // 将读取到的数据写入接收管道
                    writer.Write(readBuffer.AsSpan().Slice(0, length));
                    var flushTask = writer.FlushAsync();
                    if (!flushTask.IsCompleted)
                    {
                        await flushTask.ConfigureAwait(false);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public virtual void Close()
        {
            _closeSrc.Cancel();
        }

        public virtual bool IsClose()
        {
            return _closeSrc.IsCancellationRequested;
        }

        protected virtual bool TryParseMessage(ref ReadOnlySequence<byte> input)
        {
            var reader = new MessagePack.SequenceReader<byte>(input);

            if (!reader.TryReadBigEndian(out int length) || reader.Remaining < length - 4)
            {
                return false;
            }

            var payload = input.Slice(reader.Position, length - 4);
            if (payload.Length < 4)
            {
                throw new Exception("消息长度不够");
            }
            
            //消息id
            reader.TryReadBigEndian(out int msgId);

            var message = MessagePackSerializer.Deserialize<Message>(payload.Slice(4));
#if UNITY_EDITOR
            LogKit.I("收到消息:" + MessagePackSerializer.SerializeToJson(message));
#endif
            if (message.MsgId != msgId)
            {
                throw new Exception($"解析消息错误，注册消息id和消息无法对应.real:{message.MsgId}, register:{msgId}");
            }

            _onMessage(message);
            
            input = input.Slice(input.GetPosition(length));
            
            return true;
        }

        private const int Magic = 0x1234;
        int count = 0;

        public void Write(Message msg)
        {
            if (IsClose())
            {
                return;
            }

#if UNITY_EDITOR
            LogKit.I("发送消息:" + MessagePackSerializer.SerializeToJson(msg));
#endif
            var bytes = MessagePackSerializer.Serialize(msg);
            int len = 4 + 8 + 4 + 4 + bytes.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(len);

            count++;
            int magic = Magic + count;
            magic ^= Magic << 8;
            magic ^= len;

            int offset = 0;
            var buffSpan = buffer.AsSpan();
            buffSpan.WriteInt(len, ref offset);
            buffSpan.WriteLong(DateTime.Now.Ticks, ref offset);
            buffSpan.WriteInt(magic, ref offset);
            buffSpan.WriteInt(msg.MsgId, ref offset);
            buffSpan.WriteBytesWithoutLength(bytes, ref offset);
            _socket.GetStream().Write(buffer, 0, len);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}