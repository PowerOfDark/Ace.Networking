using System;
using System.Collections.Generic;
using System.Text;
using Ace.Networking.Threading;
using Ace.Networking.Services;

namespace Ace.Networking
{
    public static class ConnectionExtensions
    {

        public static IConnectionBuilder UseServices(this IConnectionBuilder b, Func<IServicesBuilder<IConnection>, IServicesBuilder<IConnection>> config)
        {
            return b.UseServices<ServicesBuilder<IConnection>>(config);
        }

        public static IConnectionBuilder UseServicesResolving(this IConnectionBuilder b, Func<IServicesBuilder<IConnection>, IServicesBuilder<IConnection>> config)
        {
            return b.UseServices<ResolvingServicesBuilder<IConnection>>(config);
        }
    }
}
