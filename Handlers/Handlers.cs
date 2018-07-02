using System;
using Ace.Networking.Interfaces;

namespace Ace.Networking.Handlers
{
    public delegate object GenericPayloadHandler<in T>(IConnection connection, T payload);

    public delegate void GlobalPayloadHandler(IConnection connection, object payload, Type type);

    public delegate object PayloadHandler(IConnection connection, object payload, Type type);

    public delegate bool RequestHandler(RequestWrapper request);
}