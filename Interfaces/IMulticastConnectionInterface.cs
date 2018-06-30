using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ace.Networking.Handlers;

namespace Ace.Networking.Interfaces
{
    public interface IMulticastConnectionInterface : IConnectionDispatcherInteface
    {
        void Close();
        Task Send<T>(T data);

        event Connection.DisconnectHandler Disconnected;
    }
}