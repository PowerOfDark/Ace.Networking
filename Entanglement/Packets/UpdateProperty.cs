using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract]
    public class PropertyData : IEqualityComparer<PropertyData>
    {
        public string PropertyName { get; set; }
        public byte[] SerializedData { get; set; }

        public bool Equals(PropertyData x, PropertyData y)
        {
            return x.PropertyName == y.PropertyName;
        }

        public int GetHashCode(PropertyData obj)
        {
            return obj.PropertyName?.GetHashCode() ?? 0;
        }
    }

    [ProtoContract]
    public class UpdateProperties
    {
        public List<PropertyData> Updates { get; set; }
        public Guid Eid { get; set; }
    }
}