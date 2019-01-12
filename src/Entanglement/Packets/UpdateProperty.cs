using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
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

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("8559B5CA-2A9D-4D26-9FC4-29105C13CCA5")]
    public class UpdateProperties
    {
        public List<PropertyData> Updates { get; set; }
        public Guid Eid { get; set; }
    }
}