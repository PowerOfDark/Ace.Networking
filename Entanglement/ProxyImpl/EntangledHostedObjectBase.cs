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
                RemoteExceptionAdapter exceptionAdapter = null;
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
                    exceptionAdapter = new RemoteExceptionAdapter("A remote task failed", e);
                }
                // HUGE HACK WARNING
                // we need to somehow operate on Task<T>, where T is unknown at compile time
                // yet it is possible to always cast it to Task, then by casting it to a dynamic object access the result
                if (exceptionAdapter != null || task.IsCompleted)
                {
                    var res = new ExecuteMethodResult();
                    if (exceptionAdapter != null || task.IsFaulted)
                    {
                        res.Data = null;
                        res.ExceptionAdapter = exceptionAdapter ?? new RemoteExceptionAdapter("A remote task failed", task.Exception);
                    }

                    //else the task is completed
                    else if (overload.RealReturnType == typeof(void))
                    {
                        res.ExceptionAdapter = null;
                        res.Data = null;
                    }
                    else
                    {
                        res.Data = (object)((dynamic) task).Result;
                        res.ExceptionAdapter = null;
                    }

                    req.SendResponse(res);
                    return;
                }
                if (overload.RealReturnType == typeof(void))
                {
                    task.ContinueWith(t =>
                    {
                        RemoteExceptionAdapter ex = null;
                        if (t.Exception != null) ex = new RemoteExceptionAdapter("A remote task failed", t.Exception);
                        req.SendResponse(new ExecuteMethodResult() {Data = null, ExceptionAdapter = ex});
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
                            exe.ExceptionAdapter = new RemoteExceptionAdapter("A remote task failed", t.Exception);
                        }
                        else
                        {
                            exe.ExceptionAdapter = null;
                            exe.Data = ((dynamic) t).Result;
                        }

                        req.SendResponse(exe);
                    });
                }

            }
        }
    }
}
