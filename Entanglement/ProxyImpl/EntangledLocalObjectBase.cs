using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Extensions;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;
using Ace.Networking.Memory;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledLocalObjectBase : EntangledObjectBase
    {
        protected object _sync = new object();

        public EntangledLocalObjectBase(IConnection host, Guid eid, InterfaceDescriptor desc)
        {
            Host = host;
            _Eid = eid;
            _Descriptor = desc;
        }

        public IConnection Host { get; internal set; }

        public ExecuteMethod GetExecuteMethodDescriptor(string name, Type returnType, params object[] arg)
        {
            byte[][] arguments = null;
            if ((arg?.Length??0) > 0)
            {
                arguments = new byte[arg.Length][];
                using (var mm = MemoryManager.Instance.GetStream())
                {
                    int i = 0;
                    foreach (var a in arg)
                    {
                        mm.SetLength(0);
                        Host.Serializer.Serialize(a, mm, out _);
                        arguments[i++] = mm.ToArray();
                    }
                }

            }
            var exe = new ExecuteMethod
            {
                Eid = _Eid,
                Arguments = arguments,
                Method = name,
                _ReturnType = returnType
            };
            return exe;
        }

        public async Task<T> ExecuteMethod<T>(string name, params object[] arg)
        {
            var desc = GetExecuteMethodDescriptor(name, typeof(T), arg);
            var res = await Host.SendRequest<ExecuteMethod, ExecuteMethodResult>(desc);
            if (res.ExceptionAdapter != null)
                throw new RemoteException(res.ExceptionAdapter);
            if (res.SerializedData == null || res.SerializedData.Length == 0) return default;
            return res.SerializedData.Deserialize<T>(this.Host.Serializer);
        }

        public async Task ExecuteMethodVoid(string name, params object[] arg)
        {
            var desc = GetExecuteMethodDescriptor(name, typeof(void), arg);
            var res = await Host.SendRequest<ExecuteMethod, ExecuteMethodResult>(desc);
            if (res.ExceptionAdapter != null)
                throw new RemoteException(res.ExceptionAdapter);
        }

        public void UpdateProperties(IConnection host, UpdateProperties updates)
        {
            if ((updates?.Updates.Count ?? 0) == 0) return;

            lock (_sync)
            {
                foreach (var update in updates.Updates)
                    if (_Descriptor.Properties.TryGetValue(update.PropertyName, out var prop))
                        using (var ms = new MemoryStream(update.SerializedData))
                        {
                            prop.BackingField.SetValue(this,
                                host.Serializer.Deserialize(Host.Serializer.SupportedContentType, ms, out _));
                        }
            }

            foreach (var update in updates.Updates) OnPropertyChanged(update.PropertyName);
        }

        private object updateProperties(IConnection host, UpdateProperties updates)
        {
            UpdateProperties(host, updates);
            return null;
        }
    }
}