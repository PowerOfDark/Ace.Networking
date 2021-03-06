﻿using Ace.Networking.MicroProtocol.Headers;
using Ace.Networking.MicroProtocol.PacketTypes;
using Ace.Networking.MicroProtocol.Structures;
using Ace.Networking.Serializers;

namespace Ace.Networking.MicroProtocol
{
    public interface IPayloadEncoder
    {
        IPayloadSerializer Serializer { get; }

        void Prepare(BasicHeader header, object message);

        void PrepareRaw(RawDataPacket rawData);

        void PrepareContent(object payload);

        void Prepare(IPreparedPacket packet);

        void Send(SocketBuffer buffer);

        bool OnSendCompleted(int bytesTransferred);

        void Clear();

        IPayloadEncoder Clone();
    }
}