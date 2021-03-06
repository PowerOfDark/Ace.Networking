﻿using System.Threading.Tasks;

namespace Ace.Networking.Handlers
{
    public interface IRequestWrapper
    {
        object Request { get; }
        IConnection Connection { get; }
        Task SendResponse<T>(T response);
        bool TrySendResponse<T>(T response, out Task task);
    }
}