using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Ace.Networking.Entanglement.Packets;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Reflection;
using Ace.Networking.Entanglement.Services;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Handlers;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement
{
    public class EntanglementHostService : IEntanglementHostService
    {
        protected ConcurrentDictionary<Guid /*InterfaceId*/, Guid /*Eid*/> GlobalObjectMap =
            new ConcurrentDictionary<Guid, Guid>();

        protected ConcurrentDictionary<Guid /*InterfaceId*/, InterfaceEntry> Interfaces =
            new ConcurrentDictionary<Guid, InterfaceEntry>();

        protected HashSet<Guid /*Eid*/> ManualMap = new HashSet<Guid>();

        protected ConcurrentDictionary<Guid /*Eid*/, EntangledHostedObjectBase> Objects =
            new ConcurrentDictionary<Guid, EntangledHostedObjectBase>();

        protected ConcurrentDictionary<IConnection, ConcurrentDictionary<Guid /*InterfaceId*/, Guid /*Eid*/>>
            ScopedObjectMap =
                new ConcurrentDictionary<IConnection, ConcurrentDictionary<Guid, Guid>>();


        protected HashSet<ICommon> BoundInterfaces { get; } =
            new HashSet<ICommon>();

        public bool IsActive => BoundInterfaces.Count > 0;

        public void Attach(ICommon server)
        {
            if (BoundInterfaces.Contains(server)) return;
            BoundInterfaces.Add(server);

            server.ClientDisconnected += Server_ClientDisconnected;

            server.OnRequest<ExecuteMethod>(OnRequestExecuteMethodResult);
            server.OnRequest<EntangleRequest>(OnRequestEntangle);
            server.OnRequest<UpdateRequest>(OnRequestUpdate);
            Console.WriteLine("Entanglement service online!");
        }

        public void Detach(ICommon server)
        {
            if (!BoundInterfaces.Remove(server)) return;

            server.ClientDisconnected -= Server_ClientDisconnected;

            server.OffRequest<ExecuteMethod>(OnRequestExecuteMethodResult);
            server.OffRequest<EntangleRequest>(OnRequestEntangle);
            server.OffRequest<UpdateRequest>(OnRequestUpdate);
        }

        public EntangledHostedObjectBase GetHostedObject(Guid eid)
        {
            return Objects.TryGetValue(eid, out var obj) ? obj : null;
        }


        public Guid? GetInstance(Guid interfaceId, IConnection scope = null)
        {
            if (!Interfaces.TryGetValue(interfaceId, out var ie))
                return null;
            return GetInstance(ie, scope);
        }


        private void Register(Type baseType, Type type, EntanglementAccess access)
        {
            var guid = baseType.GetTypeInfo().GUID;
            if (!Interfaces.TryAdd(guid,
                new InterfaceEntry
                {
                    Access = access,
                    Type = type,
                    InterfaceId = guid,
                    InterfaceDescriptor = InterfaceDescriptor.Get(baseType)
                }))
                Interfaces[guid].Access = access;
        }

        public IEntanglementHostService Register<TBase, T>(EntanglementAccess access)
            where TBase : class/*, IEntangledObject*/
            where T : EntangledHostedObjectBase, TBase
        {
            Register(typeof(TBase), typeof(T), access);
            return this;
        }

        public IEntanglementHostService RegisterAll(string namespaceBase = null, Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetEntryAssembly();
            var dot = (namespaceBase??string.Empty) + ".";
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetTypeInfo().GetCustomAttribute<EntanglementAttribute>();
                if (attr == null) continue;
                if (namespaceBase == null || type.Namespace == namespaceBase || type.Namespace.StartsWith(dot))
                {
                    Register(attr.Interface, type, attr.Access);
                }
            }

            return this;
        }

        private void Server_ClientDisconnected(IConnection connection, Exception exception)
        {
            if (ScopedObjectMap.TryRemove(connection, out var objects))
                foreach (var obj in objects)
                    Objects.TryRemove(obj.Value, out _);
        }

        protected Guid? GetExistingEid(InterfaceEntry ie, IConnection scope = null)
        {
            if (ie.Access == EntanglementAccess.Scoped && scope != null &&
                ScopedObjectMap.TryGetValue(scope, out var mapScoped) &&
                (mapScoped?.TryGetValue(ie.InterfaceId, out var sEid) ?? false))
                return sEid;
            if (ie.Access == EntanglementAccess.Global &&
                GlobalObjectMap.TryGetValue(ie.InterfaceId, out var gEid))
                return gEid;
            return null;
        }

        protected Guid GetFreeEidSlot()
        {
            var eid = Guid.NewGuid();
            while (!Objects.TryAdd(eid, null)) eid = Guid.NewGuid();
            return eid;
        }

        protected Guid? CreateInstance(InterfaceEntry ie, IConnection scope = null)
        {
            if (ie.Access == EntanglementAccess.Manual && scope != null) return null;
            if (ie.Access == EntanglementAccess.Scoped && (scope == null || !scope.Connected)) return null;
            if (GetExistingEid(ie, scope).HasValue) return null;

            var eid = GetFreeEidSlot();

            switch (ie.Access)
            {
                case EntanglementAccess.Scoped:
                    if (!ScopedObjectMap.TryGetValue(scope, out var scopedMap))
                    {
                        scopedMap = new ConcurrentDictionary<Guid, Guid>();
                        ScopedObjectMap.TryAdd(scope, scopedMap);
                    }

                    scopedMap.TryAdd(ie.InterfaceId, eid);
                    break;
                case EntanglementAccess.Global:
                    GlobalObjectMap[ie.InterfaceId] = eid;
                    break;
                case EntanglementAccess.Manual:
                    ManualMap.Add(eid);
                    break;
            }

            Objects[eid] = (EntangledHostedObjectBase) Activator.CreateInstance(ie.Type, eid, ie.InterfaceDescriptor);
            return eid;
        }

        protected Guid? GetInstance(InterfaceEntry ie, IConnection scope = null)
        {
            var existingEid = GetExistingEid(ie, scope);
            return existingEid ?? CreateInstance(ie, scope);
        }

        private bool OnRequestEntangle(IRequestWrapper request)
        {
            Console.WriteLine("On request entangle");
            var req = (EntangleRequest) request.Request;

            Guid? eid = null;

            if (Interfaces.TryGetValue(req.InterfaceId, out var ie))
            {
                if (req.Eid.HasValue)
                {
                    if (ie.Access == EntanglementAccess.Manual || ie.Access == EntanglementAccess.Global)
                    {
                        AddClient(request.Connection, req.Eid.Value);
                        eid = req.Eid;
                    }
                }
                else
                {
                    eid = GetInstance(ie, request.Connection);
                    if (eid.HasValue)
                        AddClient(request.Connection, eid.Value);
                }
            }

            request.SendResponse(new EntangleResult {Eid = eid});

            return true;
        }

        protected void AddClient(IConnection client, Guid eid)
        {
            Objects[eid].AddClient(client);
        }

        /*public void RegisterScoped<TBase, T>() where TBase : class, IEntangledObject where T : EntangledHostedObjectBase, TBase
        {
            Register<TBase, T>(EntanglementAccess.Scoped);
        }

        public void RegisterGlobal<TBase, T>() where TBase : class, IEntangledObject where T : EntangledHostedObjectBase, TBase
        {
            Register<TBase, T>(EntanglementAccess.Global);
        }*/

        protected bool OnRequestExecuteMethodResult(IRequestWrapper wrapper)
        {
            var cmd = (ExecuteMethod) wrapper.Request;
            if (Objects.TryGetValue(cmd.Eid, out var obj)) obj.Execute(wrapper);
            return true;
        }

        private bool OnRequestUpdate(RequestWrapper request)
        {
            var req = (UpdateRequest) request.Request;
            if (Objects.TryGetValue(req.Eid, out var obj)) obj.SendState(request);
            return true;
        }
    }
}