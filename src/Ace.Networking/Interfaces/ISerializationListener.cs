using System.IO;
using Ace.Networking.Serializers;

namespace Ace.Networking.Interfaces
{
    public interface ISerializationListener
    {
        void PreSerialize(IPayloadSerializer serializer, Stream stream);
        void PostSerialize(IPayloadSerializer serializer, Stream stream);

        void PostDeserialize(IPayloadSerializer serializer, Stream stream);
    }
}