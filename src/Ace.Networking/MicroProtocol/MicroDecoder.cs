using System;
using System.IO;
using System.Runtime.CompilerServices;
using Ace.Networking.Memory;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.Structures;

namespace Ace.Networking.MicroProtocol
{
    public class MicroDecoder : IPayloadDecoder
    {
        /// <summary>
        ///     Protocol version
        /// </summary>
        public const byte Version = 3;

        private int _bytesLeftForCurrentState;
        private int _bytesLeftInSocketBuffer;
        private int[] _contentLengths;

        private readonly RecyclableMemoryStream _contentStream; // = new MemoryStream();
        private Type _firstType;
        private BasicHeader _headerObject;
        private int _headerOffset;
        private ushort _headerSize;

        private Action<BasicHeader, object, Type> _messageReceived;
        private object[] _objects;
        private int _payloadPosition;
        private byte _protocolVersion;
        private RawDataHeader.RawDataHandler _rawDataReceived;
        private int _socketBufferOffset;
        private Func<SocketBuffer, bool> _stateMethod;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MicroDecoder" /> class.
        /// </summary>
        /// <param name="serializer">The serializer used to decode the payload</param>
        /// <exception cref="System.ArgumentNullException">serializer</exception>
        public MicroDecoder(IPayloadSerializer serializer)
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _bytesLeftForCurrentState = sizeof(short);
            _stateMethod = ReadHeaderLength;
            _contentStream = MemoryManager.Instance.GetStream();
        }

        public IPayloadSerializer Serializer { get; }

        /// <summary>
        ///     Reset the decoder so that we can parse a new message
        /// </summary>
        public void Clear()
        {
            _bytesLeftForCurrentState = sizeof(short);
            _bytesLeftInSocketBuffer = 0;
            _contentStream?.Dispose();
            _headerOffset = 0;
            _socketBufferOffset = 0;
            _contentLengths = null;
            _objects = null;
            _stateMethod = ReadHeaderLength;
            _payloadPosition = 0;
        }

        /// <summary>
        ///     A new message have been received.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The message will be a deserialized message or a <c>Stream</c> derived object (if the sender sent a
        ///         <c>Stream</c> or a <c>byte[]</c> array).
        ///     </para>
        /// </remarks>
        public Action<BasicHeader, object, Type> PacketReceived
        {
            get => _messageReceived;
            set
            {
                if (value == null) value = (h, o, t) => { };

                _messageReceived = value;
            }
        }

        public RawDataHeader.RawDataHandler RawDataReceived
        {
            get => _rawDataReceived;
            set
            {
                if (value == null) value = (i, s, stream) => null;
                _rawDataReceived = value;
            }
        }

        /// <summary>
        ///     Process bytes that we've received on the socket.
        /// </summary>
        /// <param name="buffer">Buffer to process.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessReadBytes(SocketBuffer buffer)
        {
            _bytesLeftInSocketBuffer = buffer.BytesTransferred;
            _socketBufferOffset = buffer.Offset;


            while (_stateMethod(buffer))
            {
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPayloadDecoder Clone()
        {
            return new MicroDecoder(Serializer.Clone());
        }

        private bool ReadHeaderLength(SocketBuffer e)
        {
            if (!CopyBytes(e)) return false;
            _contentStream.Position = 0;
            _headerSize = _contentStream.ReadUInt16();
            _contentStream.Position = 0;
            _bytesLeftForCurrentState = _headerSize - sizeof(ushort);
            _contentStream.ReserveSingleBlock(_bytesLeftForCurrentState);
            _stateMethod = ProcessFixedHeader;
            return true;
        }

        private bool CopyBytes(SocketBuffer e)
        {
            if (_bytesLeftInSocketBuffer == 0) return false;

            if (_bytesLeftForCurrentState > 0)
            {
                var toCopy = Math.Min(_bytesLeftForCurrentState, _bytesLeftInSocketBuffer);
                _contentStream.Write(e.Buffer, _socketBufferOffset, toCopy);

                _bytesLeftForCurrentState -= toCopy;
                _bytesLeftInSocketBuffer -= toCopy;
                _socketBufferOffset += toCopy;
            }

            return _bytesLeftForCurrentState == 0;
        }

        private bool ProcessFixedHeader(SocketBuffer e)
        {
            if (!CopyBytes(e)) return false;
            _contentStream.Position = 0;
            _protocolVersion = (byte) _contentStream.ReadByte();

            _headerObject = BasicHeader.Upgrade(_contentStream);

            _stateMethod = ProcessContent;
            var contentHeader = _headerObject as ContentHeader;
            if (contentHeader != null)
            {
                _contentLengths = contentHeader.ContentLength;
                if (contentHeader.PacketFlag.HasFlag(PacketFlag.MultiContent))
                    _objects = new object[contentHeader.ContentLength.Length];
            }

            _bytesLeftForCurrentState = contentHeader?.ContentLength[0] ?? -1;
            if (_headerObject.PacketType == PacketType.RawData)
                _bytesLeftForCurrentState = (_headerObject as RawDataHeader)?.ContentLength ?? -1;


            if (_headerObject.PacketFlag.HasFlag(PacketFlag.NoContent) || _bytesLeftForCurrentState == 0)
            {
                _bytesLeftForCurrentState = -1;
                _contentStream.SetLength(0);
            }
            else
            {
                _contentStream.Position = 0;
                _contentStream.ReserveSingleBlock(_bytesLeftForCurrentState);
            }


            return true;
        }

        private bool ProcessContent(SocketBuffer arg)
        {
            if (_bytesLeftForCurrentState == -1) goto SKIP_CHECKS;
            if (_bytesLeftForCurrentState == 0 || _bytesLeftInSocketBuffer == 0) return false;

            var bytesToCopy = Math.Min(_bytesLeftForCurrentState, _bytesLeftInSocketBuffer);
            _contentStream.Write(arg.Buffer, _socketBufferOffset, bytesToCopy);
            _bytesLeftInSocketBuffer -= bytesToCopy;
            _bytesLeftForCurrentState -= bytesToCopy;
            _socketBufferOffset += bytesToCopy;

            if (_bytesLeftForCurrentState > 0) return false;
            SKIP_CHECKS:

            _bytesLeftForCurrentState = sizeof(ushort);
            //_headerOffset = 0;
            _stateMethod = ReadHeaderLength;
            _contentStream?.Seek(0, SeekOrigin.Begin);

            var isProcessed = false;
            if (_headerObject.PacketType == PacketType.RawData)
            {
                if (!(_headerObject is RawDataHeader rawData)) return false;
                RawDataReceived(rawData.RawDataBufferId, rawData.RawDataSeq, _contentStream);
                isProcessed = true;
            }


            if (!isProcessed && _headerObject is ContentHeader content)
            {
                var contentType = content.ContentType;
                //var packet = new DefaultContentPacket(content, null);
                Type type;
                object payload;
                if (_contentStream.Length == 0)
                {
                    payload = null;
                    type = typeof(object);
                }
                else
                {
                    var message = Serializer.Deserialize(contentType, _contentStream, out var resolvedType);
                    payload = message;
                    type = resolvedType;
                }

                if (_objects != null)
                {
                    _objects[_payloadPosition++] = payload;
                    if (_payloadPosition == 1)
                        _firstType = type;
                    if (_payloadPosition == _objects.Length)
                    {
                        (_objects[0] as IDynamicPayload).Construct(_objects);
                        PacketReceived(content, _objects[0], _firstType);
                        _contentLengths = null;
                        _payloadPosition = 0;
                        _objects = null;
                    }
                }
                else
                {
                    PacketReceived(content, payload, type);
                }

                isProcessed = true;
            }

            if (isProcessed)
            {
                _contentStream?.SetLength(0);
                if (_objects != null)
                {
                    _stateMethod = ProcessContent;
                    _bytesLeftForCurrentState = _contentLengths[_payloadPosition];
                }

                return true;
            }

            return false;
        }
    }
}