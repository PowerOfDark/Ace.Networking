﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using Ace.Networking.Interfaces;
using Ace.Networking.Memory;
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

        private Stream _bodyStream;
        private int _bytesEnqueued;
        private int _bytesLeftToSend;
        private int _bytesTransferred;
        private bool _disposeBodyStream = true;
        private BasicHeader _header;
        private bool _headerIsSent;
        private int _headerSize;
        private object _message;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MicroEncoder" /> class.
        /// </summary>
        /// <param name="serializer">
        ///     Serializer used to serialize the messages that should be sent.
        /// </param>
        public MicroEncoder(IPayloadSerializer serializer)
        {
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _bufferSlice = new BufferSlice(new byte[65535], 0, 65535);
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
        public MicroEncoder(IPayloadSerializer serializer, IBufferSlice bufferSlice)
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
                var headerLength = CreateHeader(out var streamLen);
                var bytesToWrite = Math.Min(_bufferSlice.Capacity - headerLength, streamLen);
                _bodyStream.Read(_bufferSlice.Buffer, _bufferSlice.Offset + headerLength, bytesToWrite);
                args.SetBuffer(_bufferSlice.Buffer, _bufferSlice.Offset, bytesToWrite + headerLength);
                _bytesEnqueued = headerLength + bytesToWrite;
                _bytesLeftToSend = headerLength + streamLen;
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
            if (_bytesLeftToSend == 0) Clear();

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

            if (_disposeBodyStream)
            {
                //bodyStream is null for channels that connected
                //but never sent a message.

                _bodyStream?.Dispose();
                _bodyStream = null;
            }



            _headerIsSent = false;
            _headerSize = 0;
            _message = null;
            _disposeBodyStream = true;
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
                    _bodyStream = (Stream) _message;
                    content.ContentType = Serializer.CreateContentType(typeof(Stream));
                }
                else if (_message is byte[] buf)
                {
                    _bodyStream = new MemoryStream(buf);
                    _bodyStream.SetLength(buf.Length);
                    content.ContentType = Serializer.CreateContentType(typeof(byte[]));
                    _disposeBodyStream = true;
                }
                else
                {
                    _bodyStream = MemoryManager.Instance.GetStream();
                    _disposeBodyStream = true;
                    Serializer.Serialize(_message, _bodyStream, out var contentType);
                    if (contentType == null) contentType = Serializer.CreateContentType(_message.GetType());
                    if (contentType.Length > 2048)
                        throw new InvalidOperationException(
                            "The content type may not be larger than 2048 bytes. Type: " +
                            _message.GetType().AssemblyQualifiedName);
                    content.ContentType = contentType;
                    _bodyStream.Position = 0;
                }

                contentLength = content.ContentLength = checked((int) (_bodyStream.Length - _bodyStream.Position));
                if (content.ContentLength == 0) content.PacketFlag |= PacketFlag.NoContent;
            }
            else if (_header is RawDataHeader raw)
            {
                _bodyStream = (Stream) _message;
                if (raw.ContentLength <= 0)
                    raw.ContentLength = checked((int) (_bodyStream.Length - _bodyStream.Position));
                contentLength = raw.ContentLength;
                _disposeBodyStream = raw.DisposeStreamAfterSend;
            }

            var sliceOffset = _bufferSlice.Offset;
            var sliceBuffer = _bufferSlice.Buffer;


            const int baseHeaderSize = /* header length */ sizeof(short) + /* encoder version */ sizeof(byte);
            _headerSize = baseHeaderSize; //
            sliceBuffer[sliceOffset + sizeof(short)] = Version;
            _header.Serialize(_bufferSlice.Buffer, sliceOffset + _headerSize);
            _headerSize += _header.Position;
            if (_headerSize > ushort.MaxValue) throw new InvalidDataException("Invalid header");
            BitConverter2.GetBytes((short) _headerSize, sliceBuffer, sliceOffset);

            return _headerSize;
        }
    }
}