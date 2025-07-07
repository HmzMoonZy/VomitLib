using MessagePack;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System;
using System.Threading;
using System.Threading.Tasks;
using Twenty2.VomitLib;
using Debug = UnityEngine.Debug;

namespace Twenty2.Vomit.Net
{
    public class NetChannel
    {
        protected Action<Message> _onMessage;
        protected Action _onClose;
        protected Pipe _recvPipe;
        protected TcpClient _socket;
        protected CancellationTokenSource _cts = new();

        public NetChannel(TcpClient socket, Action<Message> onMessage = null, Action onClose = null)
        {
            this._socket = socket;
            this._onMessage = onMessage;
            this._onClose = onClose;
            _recvPipe = new Pipe();
        }

        public virtual void Close()
        {
            _cts.Cancel();
        }

        public virtual bool IsClose()
        {
            return _cts.IsCancellationRequested;
        }

        public async Task StartAsync()
        {
            try
            {
                _ = RecvNetData();
                var cancelToken = _cts.Token;
                while (!cancelToken.IsCancellationRequested)
                {
                    var result = await _recvPipe.Reader.ReadAsync(cancelToken);
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
                Log.Error(e.Message);
            }

            Close();
            _onClose?.Invoke();
        }

        private async Task RecvNetData()
        {
            byte[] readBuffer = new byte[2048];
            var dataPipeWriter = _recvPipe.Writer;
            var cancelToken = _cts.Token;
            while (!cancelToken.IsCancellationRequested)
            {
                var length = await _socket.GetStream().ReadAsync(readBuffer, 0, readBuffer.Length, cancelToken);
                if (length > 0)
                {
                    dataPipeWriter.Write(readBuffer.AsSpan().Slice(0, length));
                    var flushTask = dataPipeWriter.FlushAsync();
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

        protected virtual bool TryParseMessage(ref ReadOnlySequence<byte> input)
        {
            var reader = new System.Buffers.SequenceReader<byte>(input);

            if (!reader.TryReadBigEndian(out int length) || reader.Remaining < length - 4)
            {
                return false;
            }

            var payload = input.Slice(reader.Position, length - 4);
            if (payload.Length < 4)
                throw new Exception("消息长度不够");
            //消息id
            reader.TryReadBigEndian(out int msgId);

            var message = MessagePackSerializer.Deserialize<Message>(payload.Slice(4));
            Log.Debug("收到消息:" + MessagePackSerializer.SerializeToJson(message));
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
                return;

            Log.Debug("发送消息:" + MessagePackSerializer.SerializeToJson(msg));
            var bytes = MessagePackSerializer.Serialize(msg);
            int len = 4 + 8 + 4 + 4 + bytes.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(len);

            int magic = Magic + ++count;
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