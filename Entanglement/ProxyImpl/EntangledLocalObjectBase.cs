using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledLocalObjectBase : EntangledObjectBase
    {
        public IConnection Host { get; internal set; }

        public EntangledLocalObjectBase(IConnection host, Guid eid, InterfaceDescriptor desc)
        {
            Host = host;
            Eid = eid;
            Descriptor = desc;
        }

        public ExecuteMethod Execute(string name, Type returnType, params object[] c)
        {
            var exe = new ExecuteMethod()
            {
                Eid = this.Eid,
                Arguments = c.Select(t =>
                {
                    using (var ms = new MemoryStream())
                    {
                        Host.Serializer.Serialize(t, ms);

                        var p = new MethodParameter()
                        {
                            FullName = t.GetType().FullName,
                            SerializedData = ms.ToArray()
                        };
                        return p;
                    }
                        
                }).ToArray(),
                Method = name,
                ReturnValueFullName = InterfaceDescriptor.UnwrapTask(returnType).FullName
            };
            return exe;
        }
    }
}
