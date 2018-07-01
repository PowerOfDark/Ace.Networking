using Ace.Networking.Interfaces;
using Ace.Networking.Structures;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public class EntanglementProviderContext
    {
        public EntanglementProviderContext()
        {
            Sender = null;
            All = new ConnectionGroup();
        }

        public IConnection Sender { get; internal set; }
        public IConnectionGroup All { get; internal set; }
    }
}