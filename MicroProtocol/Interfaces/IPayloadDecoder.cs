using System;
using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.Structures;
using static Ace.Networking.MicroProtocol.Headers.RawDataHeader;

namespace Ace.Networking.MicroProtocol.Interfaces
{
    public interface IPayloadDecoder
    {
        IPayloadSerializer Serializer { get; }

        /// <summary>
        ///     A message has been received.
        /// </summary>
        /// <remarks>
        ///     Do note that streams are being reused by the decoder, so don't try to close them.
        /// </remarks>
        Action<BasicHeader, object, Type> PacketReceived { get; set; }

        RawDataHandler RawDataReceived { get; set; }


        /// <summary>
        ///     Clear state to allow this decoder to be reused.
        /// </summary>
        void Clear();

        /// <summary>
        ///     We've received bytes from the socket. Build a message out of them.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <remarks></remarks>
        void ProcessReadBytes(SocketBuffer buffer);

        IPayloadDecoder Clone();
    }
}