using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;
using Ace.Networking.Structures;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public class EntanglementProviderContext
    {
        public IConnection Sender { get; internal set; }
        public IConnectionGroup All { get; internal set; }

        public EntanglementProviderContext()
        {
            Sender = null;
            All = new ConnectionGroup();
        }
    }
}
