using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Services
{
    public interface IServicesBuilder
    {
        IServicesBuilder Add<TBase, T>(T instance, Action<T> config = null) where T : TBase where TBase : IService;
        IInternalServiceManager Build();
    }
}
