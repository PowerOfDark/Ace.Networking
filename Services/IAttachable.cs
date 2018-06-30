using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IAttachable
    {
        void Attach(IConnectionDispatcherInteface connection);
        void Detach(IConnectionDispatcherInteface connection);
    }
}
