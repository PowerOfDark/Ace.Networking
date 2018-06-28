using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IInternalServiceManager : IServiceManager
    {
        void Add<TBase, T>(T instance) where T : TBase where TBase : IService;
        void Attach(IServer server);
        void Detach();
    }
}
