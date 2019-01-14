using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Ace.Networking.Extensions;
using Ace.Networking.MicroProtocol.Interfaces;

namespace Ace.Networking.Handlers
{
    public abstract class PayloadHandlerDispatcherBase
    {
        public class TypeBindings
        {
            public LinkedList<RequestHandler> RequestHandlers = new LinkedList<RequestHandler>();

            public Queue<TaskCompletionSource<IRequestWrapper>> RequestTasks =
                new Queue<TaskCompletionSource<IRequestWrapper>>();
            public LinkedList<IPayloadHandlerWrapper> TypeHandlers = new LinkedList<IPayloadHandlerWrapper>();

            public Queue<TaskCompletionSource<object>> ReceiveTasks =
                new Queue<TaskCompletionSource<object>>();

        }

        protected readonly ConcurrentDictionary<Type, TypeBindings> Bindings = new ConcurrentDictionary<Type, TypeBindings>();

        protected LinkedList<TaskCompletionSource<object>> ReceiveFilters = new
            LinkedList<TaskCompletionSource<object>>();


        protected TypeBindings GetBinding(Type type)
        {
            return Bindings.GetOrAdd(type, t => new TypeBindings());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AppendTypeHandler(Type type, IPayloadHandlerWrapper handler)
        {
            var binding = GetBinding(type);
            lock (binding.TypeHandlers)
            {
                binding.TypeHandlers.AddLast(handler);
            }
        }

        protected void AppendRequestHandler(Type type, RequestHandler handler)
        {
            var binding = GetBinding(type);
            lock (binding.RequestHandlers)
            {
                binding.RequestHandlers.AddLast(handler);
            }
        }

        protected bool RemoveRequestHandler(Type type, RequestHandler handler)
        {
            if (!Bindings.TryGetValue(type, out var binding)) return false;
            lock (binding.RequestHandlers)
            {
                binding.RequestHandlers.Remove(handler);
            }

            return true;
        }
        protected bool RemoveAllRequestHandlers(Type type, RequestHandler handler)
        {
            if (!Bindings.TryGetValue(type, out var binding)) return false;
            lock (binding.RequestHandlers)
            {
                binding.RequestHandlers.Clear();
            }

            return true;
        }


        protected void AppendRequestTask(Type type, TaskCompletionSource<IRequestWrapper> tcs)
        {
            var binding = GetBinding(type);
            lock (binding.RequestTasks)
            {
                binding.RequestTasks.Enqueue(tcs);
            }
        }

        protected void AppendReceiveTask(Type type, TaskCompletionSource<object> tcs)
        {
            var binding = GetBinding(type);
            lock (binding.ReceiveTasks)
            {
                binding.ReceiveTasks.Enqueue(tcs);
            }
        }

        protected void AppendFilter(TaskCompletionSource<object> tcs)
        {
            lock (ReceiveFilters)
            {
                ReceiveFilters.AddLast(tcs);
            }
        }

        protected bool RemoveTypeHandler(Type type, object obj)
        {
            if (!Bindings.TryGetValue(type, out var binding)) return false;
            bool ret;
            var list = binding.TypeHandlers;
            lock (list)
            {
                ret = list.RemoveFirst(t => t.HandlerEquals(obj));
            }

            return ret;
        }
        protected bool RemoveAllTypeHandlers(Type type)
        {
            if (!Bindings.TryGetValue(type, out var binding)) return false;
            lock (binding.TypeHandlers)
            {
                binding.TypeHandlers.Clear();
            }
            return true;
        }

        protected bool RemoveAllRequestHandlers(Type type)
        {
            if (!Bindings.TryGetValue(type, out var binding)) return false;
            lock (binding.RequestHandlers)
            {
                binding.RequestHandlers.Clear();
            }
            return true;
        }



        public void RemoveAllHandlers()
        {
            Bindings.Clear();
            lock (ReceiveFilters)
            {
                ReceiveFilters.Clear();
            }
        }
    }
}