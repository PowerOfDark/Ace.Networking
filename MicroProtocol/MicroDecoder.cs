using System;
using System.IO;
using System.Runtime.CompilerServices;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.PacketTypes;
using Ace.Networking.MicroProtocol.Structures;

namespace Ace.Networking.MicroProtocol
{
    public class MicroDecoder : IPayloadDecoder
    {
        /// <summary>
        ///     Protocol version
        /// </summary>
        public const byte Version = 2;

        private readonly MemoryStream _contentStream = new MemoryStream();
        private readonly byte[] _header = new byte[short.MaxValue];

        private readonly IPayloadSerializer _serializer;

        private int _bytesLeftForCurrentState;
        private int _bytesLeftInSocketBuffer;
        private BasicHeader _headerObject;
        private int _headerOffset;
        private short _headerSize;

        private Action<BasicHeader, object, Type> _messageReceived;
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
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _bytesLeftForCurrentState = sizeof(short);
            _stateMethod = ReadHeaderLength;
        }

        /// <summary>
        ///     Reset the decoder so that we can parse a new message
        /// </summary>
        public void Clear()
        {
            _bytesLeftForCurrentState = sizeof(short);
            _bytesLeftInSocketBuffer = 0;
            _contentStream.SetLength(0);
            _headerOffset = 0;
            _socketBufferOffset = 0;
            _stateMethod = ReadHeaderLength;
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
                if (value == null)
                {
                    value = (h, o, t) => { };
                }

                _messageReceived = value;
            }
        }

        public RawDataHeader.RawDataHandler RawDataReceived
        {
            get => _rawDataReceived;
            set
            {
                if (value == null)
                {
                    value = (i, s, stream) => null;
                }
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
            return new MicroDecoder(_serializer.Clone());
        }

        private bool ReadHeaderLength(SocketBuffer e)
        {
            if (!CopyBytes(e))
            {
                return false;
            }

            _headerSize = BitConverter.ToInt16(_header, 0);
            _bytesLeftForCurrentState = _headerSize - sizeof(short);
            _stateMethod = ProcessFixedHeader;
            _headerOffset = 0;
            return true;
        }

        private bool CopyBytes(SocketBuffer e)
        {
            if (_bytesLeftInSocketBuffer == 0)
            {
                return false;
            }

            if (_bytesLeftForCurrentState > 0)
            {
                var toCopy = Math.Min(_bytesLeftForCurrentState, _bytesLeftInSocketBuffer);
                Buffer.BlockCopy(e.Buffer, _socketBufferOffset, _header, _headerOffset, toCopy);
                _headerOffset += toCopy;
                _bytesLeftForCurrentState -= toCopy;
                _bytesLeftInSocketBuffer -= toCopy;
                _socketBufferOffset += toCopy;
            }

            return _bytesLeftForCurrentState == 0;
        }

        private bool ProcessFixedHeader(SocketBuffer e)
        {
            if (!CopyBytes(e))
            {
                return false;
            }

            _protocolVersion = _header[0];

            _headerObject = BasicHeader.Upgrade(_header, 1);

            _stateMethod = ProcessContent;
            _bytesLeftForCurrentState = (_headerObject as ContentHeader)?.ContentLength ?? -1;
            if (_headerObject.PacketType == PacketType.RawData)
            {
                _bytesLeftForCurrentState = (_headerObject as RawDataHeader)?.ContentLength ?? -1;
            }
            if (_headerObject.PacketFlag.HasFlag(PacketFlag.NoContent) || _bytesLeftForCurrentState == 0)
            {
                _bytesLeftForCurrentState = -1;
            }
            _headerOffset = 0;
            _contentStream.Position = 0;
            _contentStream.SetLength(0);
            return true;
        }

        private bool ProcessContent(SocketBuffer arg)
        {
            if (_bytesLeftForCurrentState == -1)
            {
                goto SKIP_CHECKS;
            }
            if (_bytesLeftForCurrentState == 0 || _bytesLeftInSocketBuffer == 0)
            {
                return false;
            }

            var bytesToCopy = Math.Min(_bytesLeftForCurrentState, _bytesLeftInSocketBuffer);
            _contentStream.Write(arg.Buffer, _socketBufferOffset, bytesToCopy);
            _bytesLeftInSocketBuffer -= bytesToCopy;
            _bytesLeftForCurrentState -= bytesToCopy;
            _socketBufferOffset += bytesToCopy;

            if (_bytesLeftForCurrentState > 0)
            {
                return false;
            }
            SKIP_CHECKS:

            _bytesLeftForCurrentState = sizeof(short);
            _headerOffset = 0;
            _stateMethod = ReadHeaderLength;
            _contentStream.Position = 0;

            if (_headerObject.PacketType == PacketType.RawData)
            {
                if (!(_headerObject is RawDataHeader rawData))
                {
                    return false;
                }
                RawDataReceived(rawData.RawDataBufferId, rawData.RawDataSeq, _contentStream);
                return true;
            }


            if (_headerObject is ContentHeader content)
            {
                var contentType = content.ContentType;
                var packet = new DefaultContentPacket(content, null);
                var message = _serializer.Deserialize(contentType, _contentStream, out Type resolvedType);
                packet.Payload = message;
                packet.Type = resolvedType;

                if (packet.Payload != null)
                {
                    PacketReceived(packet.Header, packet.Payload, packet.Type);
                }
                return true;
            }

            return false;
        }
    }
}