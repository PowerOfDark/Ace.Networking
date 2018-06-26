using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Entanglement.ProxyImpl
{
    public class EntanglementProviderContext
    {
        public IConnection Sender { get; internal set; }
        public IConnectionGroup All { get; internal set; }
    }
}
