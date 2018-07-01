using System;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        IServicesBuilder<TInterface> Add<TBase, T>(T instance, Action<T> config = null)
            where T : TBase where TBase : IService<TInterface>;

        IInternalServiceManager<TInterface> Build();
    }
}