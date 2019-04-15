﻿using System;
using Ace.Networking.Threading;

namespace Ace.Networking.Services
{
    public interface IServicesBuilder<TInterface> where TInterface : class, ICommon
    {
        IServicesBuilder<TInterface> Add<TBase, T>(T instance, Action<T> config = null)
            where T : class, TBase where TBase : class;

        IServicesBuilder<TInterface> Add<TBase, T>()
            where T : class, TBase where TBase : class;

        IInternalServiceManager<TInterface> Build();
    }
}