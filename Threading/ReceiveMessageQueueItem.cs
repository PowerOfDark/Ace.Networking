using System;
using Ace.Networking.MicroProtocol.Headers;

namespace Ace.Networking.Threading
{
    public class ReceiveMessageQueueItem
    {
        public BasicHeader Header;
        public object Payload;

        public Action<BasicHeader, object, Type> PayloadReceived;

        public Type Type;

        public ReceiveMessageQueueItem(Action<BasicHeader, object, Type> payloadReceived, BasicHeader header,
            object payload, Type type)
        {
            PayloadReceived = payloadReceived;
            Header = header;
            Payload = payload;
            Type = type;
        }
    }
}