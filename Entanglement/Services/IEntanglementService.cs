using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Entanglement.ProxyImpl;
using Ace.Networking.Entanglement.Structures;
using Ace.Networking.Interfaces;
using Ace.Networking.Services;

namespace Ace.Networking.Entanglement.Services
{
    public interface IEntanglementService : IService
    {
        void Register<TBase, T>(EntanglementAccess access) where TBase : class, IEntangledObject
            where T : EntangledHostedObjectBase, TBase;

        Guid? GetInstance(Guid interfaceId, IConnection scope = null);

        EntangledHostedObjectBase GetObject(Guid eid);
    }
}
