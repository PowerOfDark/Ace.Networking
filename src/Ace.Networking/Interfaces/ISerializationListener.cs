using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Threading
{
    public interface ISerializationListener
    {
        void PreSerialize(IPayloadSerializer serializer, Stream stream);
        void PostSerialize(IPayloadSerializer serializer, Stream stream);

        void PostDeserialize(IPayloadSerializer serializer, Stream stream);
    }
}
