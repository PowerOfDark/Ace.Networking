using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Services
{
    public interface IServiceManager
    {
        T Get<T>() where T : class, IService;
    }
}
