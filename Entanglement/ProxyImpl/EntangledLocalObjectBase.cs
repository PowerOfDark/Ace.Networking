using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledLocalObjectBase : EntangledObjectBase
    {
        public IConnection Host { get; internal set; }

        public EntangledLocalObjectBase()
        {
            Console.WriteLine("HEllo WWorLd!");
        }

        public EntangledLocalObjectBase(IConnection host, Guid eid, InterfaceDescriptor desc)
        {
            Host = host;
            Eid = eid;
            Descriptor = desc;
        }

        public ExecuteMethod GetExecuteMethodDescriptor(string name, Type returnType, params object[] arg)
        {
            var exe = new ExecuteMethod()
            {
                Eid = this.Eid,
                Arguments = arg?.Select(t =>
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
                ReturnValueFullName = returnType.FullName
            };
            return exe;
        }

        public async Task<T> ExecuteMethod<T>(string name, params object[] arg)
        {
            var desc = GetExecuteMethodDescriptor(name, typeof(T), arg);
            var res = await Host.SendRequest<ExecuteMethod, ExecuteMethodResult>(desc);
            if (res.ExceptionAdapter != null)
                throw new RemoteException(res.ExceptionAdapter);
            return (T)res.Data;
        }

        public async Task ExecuteMethodVoid(string name, params object[] arg)
        {
            var desc = GetExecuteMethodDescriptor(name, typeof(void), arg);
            var res = await Host.SendRequest<ExecuteMethod, ExecuteMethodResult>(desc);
            if (res.ExceptionAdapter != null)
                throw new RemoteException(res.ExceptionAdapter);
        }

    }
}
