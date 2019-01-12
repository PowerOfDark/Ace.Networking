using System;
using System.IO;
using System.Runtime.CompilerServices;
using Ace.Networking.Interfaces;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.PacketTypes;
using Ace.Networking.MicroProtocol.Structures;
using Ace.Networking.Serializers;

namespace Ace.Networking.MicroProtocol
{
    public class MicroEncoder : IPayloadEncoder
    {
        /// <summary>
        ///     PROTOCOL version
        /// </summary>
        public const byte Version = MicroDecoder.Version;

        private readonly IBufferSlice _bufferSlice;

        private RecyclableMemoryStream _contentStream;
        private RecyclableMemoryStream _headerStream;
        private Stream _bodyStream;

        private int _bytesEnqueued;
        private int _bytesLeftToSend;
        private int _bytesTransferred;
        private bool _disposeBodyStream = true;
        private BasicHeader _header;
        private bool _headerIsSent;
        private bool _headerCreated;
        private int _headerLength;
        private int _contentLength;
        private object _message;
        private int[] _contentLengths;
        private int _payloadPosition;


        private MicroEncoder()
        {
            _contentStream = MemoryManager.Instance.GetStream();
            _headerStream = MemoryManager.Instance.GetStream();
        }
        /// <summary>
        ///     Initializes a new instance of the <see cref="MicroEncoder" /> class.
        /// </summary>
        /// <param name="serializer">
        ///     Serializer used to serialize the messages that should be sent.
        /// </param>
        public MicroEncoder(IPayloadSerializer serializer) : this()
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MicroEncoder" /> class.
        /// </summary>
        /// <param name="serializer">
        ///     Serializer used to serialize the messages that should be sent.
        /// </param>
        /// <param name="bufferSlice">Used when sending information.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     bufferSlice; At least the header should fit in the buffer
        /// </exception>
        public MicroEncoder(IPayloadSerializer serializer, IBufferSlice bufferSlice): this()
        {
            if (bufferSlice == null) throw new ArgumentNullException(nameof(bufferSlice));
            if (bufferSlice.Capacity < 520 - 256 + 2048)
                throw new ArgumentOutOfRangeException(nameof(bufferSlice), bufferSlice.Capacity,
                    "At least the header should fit in the buffer, and the header can be up to *520-256+2048* bytes");


            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _bufferSlice = bufferSlice;
        }

        public IPayloadSerializer Serializer { get; }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareRaw(RawDataPacket rawDataContainer)
        {
            if (rawDataContainer == null) throw new ArgumentNullException(nameof(rawDataContainer));
            _headerIsSent = false;
            _header = rawDataContainer.Header;
            _message = rawDataContainer.Payload ??
                       throw new InvalidDataException("Invalid raw data (needs to be a Stream)");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Prepare(BasicHeader header, object payload)
        {
            _header = header;
            _message = payload;
            _headerIsSent = _headerCreated = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareContent(object payload)
        {
            Prepare(new ContentHeader(), payload);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Prepare(IPreparedPacket p)
        {
            Prepare(p.GetHeader(), p.GetPayload());
        }

        /// <summary>
        ///     Serialize message and add it to the buffer
        /// </summary>
        /// <param name="args">Socket buffer</param>
        public void Send(SocketBuffer args)
        {
            if (!_headerIsSent)
            {
                if (!_headerCreated)
                {
                    var headerLength = CreateHeader(out _contentLength);
                    _headerStream.Position = _contentStream.Position = 0;
                    _headerCreated = true;
                    _bytesLeftToSend = headerLength;
                }
                if (_headerStream.CurrentBlockCapacity == 0)
                    _headerStream.MoveNext();
                var toWrite = Math.Min(NetworkingSettings.BufferSize, Math.Min(_headerStream.CurrentBlockCapacity, _bytesLeftToSend));
                args.SetBuffer(_headerStream.CurrentBlock, _headerStream.CurrentBlockOffset, toWrite);
                _headerStream.Position += toWrite;
            }
            else
            {
                if (_contentStream.CurrentBlockCapacity == 0)
                    _contentStream.MoveNext();
                var toWrite = Math.Min(NetworkingSettings.BufferSize, Math.Min(_contentStream.CurrentBlockCapacity, _bytesLeftToSend));
                args.SetBuffer(_contentStream.CurrentBlock, _contentStream.CurrentBlockOffset, toWrite);
                _contentStream.Position += toWrite;
            }
        }

        /// <summary>
        ///     The previous <see cref="IPayloadEncoder.Send" /> has just completed.
        /// </summary>
        /// <param name="bytesTransferred"></param>
        /// <remarks>
        ///     <c>true</c> if the message have been sent successfully; otherwise <c>false</c>.
        /// </remarks>
        public bool OnSendCompleted(int bytesTransferred)
        {
            // Make sure that the header is sent
            // required so that the Send() method can switch to the body state.
            _bytesTransferred += bytesTransferred;
            _bytesLeftToSend -= bytesTransferred;
            if (!_headerIsSent)
            {
                //_headerLength -= bytesTransferred;
                if (_bytesLeftToSend <= 0)
                {
                    _headerIsSent = true;
                    _bytesTransferred = 0;
                    _bytesLeftToSend = _contentLength;
                }
            }

            //_bytesTransferred = bytesTransferred;
            if (_bytesLeftToSend == 0) Clear();

            return _bytesLeftToSend == 0;
        }

        /// <summary>
        ///     Remove everything used for the last message
        /// </summary>
        public void Clear()
        {
            _bytesTransferred = 0;
            _bytesLeftToSend = 0;
            _payloadPosition = 0;
            _contentLengths = null;
            if (_disposeBodyStream)
            {
                //bodyStream is null for channels that connected
                //but never sent a message.

                _bodyStream?.Dispose();
                _bodyStream = null;
            }

            _headerStream.SetLength(0);
            _contentStream.SetLength(0);

            _headerIsSent = _headerCreated = false;
            _message = null;
            _disposeBodyStream = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPayloadEncoder Clone()
        {
            return new MicroEncoder(Serializer.Clone());
        }

        private int CreateHeader(out int contentLength)
        {
            contentLength = 0;
            if (_header is ContentHeader content)
            {
                if (_message is Stream)
                {
                    throw new NotSupportedException();
                }
                else if (_message is byte[] buf)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    _bodyStream = _contentStream;
                    long pos = _bodyStream.Position;
                    if (_message is IDynamicPayload dp)
                    {
                        content.PacketFlag |= PacketFlag.MultiContent;
                        var obj = dp.Deconstruct();
                        content.ContentLength = new int[obj.Length];
                        byte[] ct = null;
                        for (int i = 0; i < obj.Length; i++)
                        {
                            long cur = _bodyStream.Position;
                            Serializer.Serialize(obj[i], _bodyStream, out ct);
                            content.ContentLength[i] = checked((int)(_bodyStream.Position - cur));
                        }
                        content.ContentType = ct;
                    }
                    else
                    {
                        byte[] contentType = null;
                        Serializer.Serialize(_message, _bodyStream, out contentType);
                        content.ContentType = contentType;
                        content.ContentLength = new int[1] { checked((int)(_bodyStream.Position - pos)) };
                    }
                    _bodyStream.Position = pos;
                    contentLength = checked((int)(_bodyStream.Length - _bodyStream.Position));
                }

                if (contentLength == 0)
                    content.PacketFlag |= PacketFlag.NoContent;
                if (content.ContentLength.Length > 1)
                    content.PacketFlag |= PacketFlag.MultiContent;
            }

            else if (_header is RawDataHeader raw)
            {
                _bodyStream = (Stream) _message;
                if (raw.ContentLength <= 0)
                    raw.ContentLength = checked((int) (_bodyStream.Length - _bodyStream.Position));
                contentLength = raw.ContentLength;
                _disposeBodyStream = raw.DisposeStreamAfterSend;
            }

            _headerStream.TryExtend(512);
            _headerStream.Position = sizeof(short);
            _headerStream.Write(Version);
            _header.Serialize(_headerStream);
            short len = (short)_headerStream.Position;
            if (_headerStream.Position > ushort.MaxValue)
                throw new InvalidDataException("Invalid header");
            _headerStream.Position = 0;
            _headerStream.Write(len);

            return len;
        }

    }
}