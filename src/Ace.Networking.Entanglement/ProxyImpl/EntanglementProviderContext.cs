using Ace.Networking.Threading;
using Ace.Networking.Structures;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public class EntanglementProviderContext
    {
        public EntanglementProviderContext(ICommon host)
        {
            Host = host;
            Sender = null;
            All = new ConnectionGroup(host);
        }

        public ICommon Host { get; internal set; }
        public IConnection Sender { get; internal set; }
        public IConnectionGroup All { get; internal set; }
    }
}