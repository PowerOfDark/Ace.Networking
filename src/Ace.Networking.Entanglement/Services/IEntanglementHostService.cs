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

        /// <summary>
        /// Calls <see cref="Register{TBase, T}(EntanglementAccess)"/> for each type (with <see cref="EntanglementAttribute"/> applied) found in the <paramref name="assembly"/>,
        /// optionally filtering by namespace <paramref name="namespaceBase"/>.
        /// </summary>
        /// <param name="namespaceBase">The base namespace; specify <code>null</code> for any</param>
        /// <param name="assembly">The target assembly to be scanned; specify <code>null</code> to use the entry assembly</param>
        IEntanglementHostService RegisterAll(string namespaceBase = null, Assembly assembly = null);

        Guid? GetInstance(Guid interfaceId, IConnection scope = null);

        EntangledHostedObjectBase GetHostedObject(Guid eid);
    }
}