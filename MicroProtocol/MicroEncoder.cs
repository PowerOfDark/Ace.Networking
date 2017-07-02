using System;
using System.IO;
using System.Runtime.CompilerServices;
using Ace.Networking.Interfaces;
using Ace.Networking.MicroProtocol.Enums;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.PacketTypes;
using Ace.Networking.MicroProtocol.Structures;

namespace Ace.Networking.MicroProtocol
{
    public class MicroEncoder : IPayloadEncoder
    {
        /// <summary>
        ///     PROTOCOL version
        /// </summary>
        public const byte Version = MicroDecoder.Version;

        private readonly IBufferSlice _bufferSlice;

        private readonly MemoryStream _internalStream = new MemoryStream();
        private readonly IPayloadSerializer _serializer;
        private Stream _bodyStream;
        private int _bytesEnqueued;
        private int _bytesLeftToSend;
        private int _bytesTransferred;
        private BasicHeader _header;
        private bool _headerIsSent;
        private int _headerSize;
        private object _message;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MicroMessageEncoder" /> class.
        /// </summary>
        /// <param name="serializer">
        ///     Serializer used to serialize the messages that should be sent.
        /// </param>
        public MicroEncoder(IPayloadSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _bufferSlice = new BufferSlice(new byte[65535], 0, 65535);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MicroMessageEncoder" /> class.
        /// </summary>
        /// <param name="serializer">
        ///     Serializer used to serialize the messages that should be sent.
        /// </param>
        /// <param name="bufferSlice">Used when sending information.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     bufferSlice; At least the header should fit in the buffer
        /// </exception>
        public MicroEncoder(IPayloadSerializer serializer, IBufferSlice bufferSlice)
        {
            if (bufferSlice == null)
            {
                throw new ArgumentNullException(nameof(bufferSlice));
            }
            if (bufferSlice.Capacity < 520 - 256 + 2048)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSlice), bufferSlice.Capacity,
                    "At least the header should fit in the buffer, and the header can be up to *520-256+2048* bytes");
            }


            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _bufferSlice = bufferSlice;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrepareRaw(RawDataPacket rawDataContainer)
        {
            if (rawDataContainer == null)
            {
                throw new ArgumentNullException(nameof(rawDataContainer));
            }
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
            _headerIsSent = false;
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
        ///     Serialize message and sent it add it to the buffer
        /// </summary>
        /// <param name="args">Socket buffer</param>
        public void Send(SocketBuffer args)
        {
            if (_bytesTransferred < _bytesEnqueued)
            {
                //TODO: Is this faster than moving the bytes to the beginning of the buffer and append more bytes?
                args.SetBuffer(_bufferSlice.Buffer, _bufferSlice.Offset + _bytesTransferred,
                    _bytesEnqueued - _bytesTransferred);
                return;
            }

            if (!_headerIsSent)
            {
                var headerLength = CreateHeader();
                var bytesToWrite = (int) Math.Min(_bufferSlice.Capacity - headerLength, _bodyStream.Length);
                _bodyStream.Read(_bufferSlice.Buffer, _bufferSlice.Offset + headerLength, bytesToWrite);
                args.SetBuffer(_bufferSlice.Buffer, _bufferSlice.Offset, bytesToWrite + headerLength);
                _bytesEnqueued = headerLength + bytesToWrite;
                _bytesLeftToSend = headerLength + (int) _bodyStream.Length;
            }
            else
            {
                _bytesEnqueued = Math.Min(_bufferSlice.Capacity, _bytesLeftToSend);
                _bodyStream.Read(_bufferSlice.Buffer, _bufferSlice.Offset, _bytesEnqueued);
                args.SetBuffer(_bufferSlice.Buffer, _bufferSlice.Offset, _bytesEnqueued);
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
            if (!_headerIsSent)
            {
                _headerSize -= bytesTransferred;
                if (_headerSize <= 0)
                {
                    _headerIsSent = true;
                    _headerSize = 0;
                }
            }

            _bytesTransferred = bytesTransferred;
            _bytesLeftToSend -= bytesTransferred;
            if (_bytesLeftToSend == 0)
            {
                Clear();
            }

            return _bytesLeftToSend == 0;
        }

        /// <summary>
        ///     Remove everything used for the last message
        /// </summary>
        public void Clear()
        {
            _bytesEnqueued = 0;
            _bytesTransferred = 0;
            _bytesLeftToSend = 0;

            if (!ReferenceEquals(_bodyStream, _internalStream))
            {
                //bodyStream is null for channels that connected
                //but never sent a message.

                _bodyStream?.Dispose();
                _bodyStream = null;
            }
            else
            {
                _internalStream.SetLength(0);
            }

            _headerIsSent = false;
            _headerSize = 0;
            _message = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IPayloadEncoder Clone()
        {
            return new MicroEncoder(_serializer.Clone());
        }

        private int CreateHeader()
        {
            if (_header is ContentHeader content)
            {
                if (_message is Stream)
                {
                    _bodyStream = (Stream) _message;
                    content.ContentType = _serializer.CreateContentType(typeof(Stream));
                }
                else if (_message is byte[] buf)
                {
                    _bodyStream = new MemoryStream(buf);
                    _bodyStream.SetLength(buf.Length);
                    content.ContentType = _serializer.CreateContentType(typeof(byte[]));
                }
                else
                {
                    _bodyStream = _internalStream;
                    _serializer.Serialize(_message, _bodyStream, out byte[] contentType);
                    if (contentType == null)
                    {
                        contentType = _serializer.CreateContentType(_message.GetType());
                    }
                    if (contentType.Length > 2048)
                    {
                        throw new InvalidOperationException(
                            "The content type may not be larger than 2048 bytes. Type: " +
                            _message.GetType().AssemblyQualifiedName);
                    }
                    content.ContentType = contentType;
                }
                content.ContentLength = (int) _bodyStream.Length;
                if (content.ContentLength == 0)
                {
                    content.PacketFlag |= PacketFlag.NoContent;
                }
            }
            else if (_header is RawDataHeader raw)
            {
                _bodyStream = (MemoryStream) _message;
                if (raw.ContentLength == 0)
                {
                    raw.ContentLength = (int) _bodyStream.Length;
                }
            }

            _bodyStream.Position = 0;
            var sliceOffset = _bufferSlice.Offset;
            var sliceBuffer = _bufferSlice.Buffer;
            _headerSize = 1 + 2; //
            sliceBuffer[sliceOffset + 2] = Version;
            _header.Serialize(_bufferSlice.Buffer, sliceOffset + 3);
            _headerSize += (ushort) _header.Position;
            BitConverter2.GetBytes((short) _headerSize, sliceBuffer, sliceOffset);

            return _headerSize;
        }
    }
}