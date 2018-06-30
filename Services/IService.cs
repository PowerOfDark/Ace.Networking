using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IService
    {
        bool IsActive { get; }

        void Attach(IConnectionDispatcherInteface server);
        void Detach(IConnectionDispatcherInteface server);
    }
}
