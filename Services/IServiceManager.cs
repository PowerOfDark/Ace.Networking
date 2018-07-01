﻿using Ace.Networking.Interfaces;

namespace Ace.Networking.Services
{
    public interface IServiceManager<TInterface> where TInterface : class, ICommon
    {
        T Get<T>() where T : class, IService<TInterface>;
    }
}