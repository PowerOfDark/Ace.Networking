using System;
using System.Reflection;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking.Entanglement.Services
{
    public interface IEntanglementHostService : IService<ICommon>
    {
        IEntanglementHostService Register<TBase, T>(EntanglementAccess access) where TBase : class/*, IEntangledObject*/
            where T : EntangledHostedObjectBase, TBase;

        IEntanglementHostService RegisterAll(string namespaceBase = null, Assembly assembly = null);

        Guid? GetInstance(Guid interfaceId, IConnection scope = null);

        EntangledHostedObjectBase GetHostedObject(Guid eid);
    }
}