using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Entanglement.Extensions
{
    public static class InternalExtensions
    {
        public static T Deserialize<T>(this byte[] data, IPayloadSerializer serializer)
        {
            using (var ms = new MemoryStream(data))
            {
                return (T)serializer.DeserializeType(typeof(T), ms);
            }
        }

        public static byte[] Serialize(object obj, IPayloadSerializer serializer)
        {
            using (var ms = new MemoryStream())
            {
                serializer.Serialize(obj, ms);
                return ms.ToArray();
            }
        }
    }
}
