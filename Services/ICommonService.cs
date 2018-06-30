using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IServerService : IService
    {
        void Attach(IServer server);
        void Detach(IServer server);
    }
}
