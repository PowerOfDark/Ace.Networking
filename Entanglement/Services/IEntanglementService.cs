using System;
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

        Guid? GetInstance(Guid interfaceId, IConnection scope = null);

        EntangledHostedObjectBase GetHostedObject(Guid eid);
    }
}