using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IClientService : IService
    {
        void Attach(IConnection server);
        void Detach(IConnection server);
    }
}
