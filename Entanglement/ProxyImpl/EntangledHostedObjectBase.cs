using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public abstract class EntangledHostedObjectBase : EntangledObjectBase
    {
        protected EntanglementProviderContext Context { get; }

        public EntangledHostedObjectBase(Guid eid, InterfaceDescriptor i)
        {
            Eid = eid;
            Descriptor = i;
            Context = new EntanglementProviderContext();
        }

        public void Execute(IRequestWrapper req)
        {
            var cmd = (ExecuteMethod) req.Request;
            //find the best overload
            var overload = Descriptor.FindOverload(cmd);
            lock (Context)
            {
                Context.Sender = req.Connection;
                RemoteException exception = null;
                Task task = null;
                try
                {
                    object[] args = new object[overload.Parameters.Length];
                    for(int i = 0; i < args.Length; i++)
                    {
                        using (var ms = new MemoryStream(cmd.Arguments[i].SerializedData))
                        {
                            args[i] = req.Connection.Serializer.DeserializeType(overload.Parameters[i].Type, ms);
                        }
                    }
                    task = (Task) overload.Method.Invoke(this, cmd.Arguments.Length == 0 ? null : args);
                }
                catch (Exception e)
                {
                    exception = new RemoteException("A remote task failed", e);
                }
                // HUGE HACK WARNING
                // we need to somehow operate on Task<T>, where T is unknown at compile time
                // yet it is possible to always cast it to Task, then by casting it to a dynamic object access the result
                if (exception != null || task.IsCompleted)
                {
                    var res = new ExecuteMethodResult();
                    if (exception != null || task.IsFaulted)
                    {
                        res.Data = null;
                        res.Exception = exception ?? new RemoteException("A remote task failed", task.Exception);
                    }

                    //else the task is completed
                    else if (overload.RealReturnType == typeof(void))
                    {
                        res.Exception = null;
                        res.Data = null;
                    }
                    else
                    {
                        res.Data = (object)((dynamic) task).Result;
                        res.Exception = null;
                    }

                    req.SendResponse(res);
                    return;
                }
                if (overload.RealReturnType == typeof(void))
                {
                    task.ContinueWith(t =>
                    {
                        RemoteException ex = null;
                        if (t.Exception != null) ex = new RemoteException("A remote task failed", t.Exception);
                        req.SendResponse(new ExecuteMethodResult() {Data = null, Exception = ex});
                    });
                }
                else
                {
                    task.ContinueWith(t =>
                    {
                        var exe = new ExecuteMethodResult();
                        if (t.Exception != null)
                        {
                            exe.Data = null;
                            exe.Exception = new RemoteException("A remote task failed", t.Exception);
                        }
                        else
                        {
                            exe.Exception = null;
                            exe.Data = ((dynamic) t).Result;
                        }

                        req.SendResponse(exe);
                    });
                }

            }
        }
    }
}
