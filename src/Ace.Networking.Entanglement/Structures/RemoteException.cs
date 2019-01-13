using System;

namespace Ace.Networking.Entanglement.Structures
{
    public class RemoteException : Exception
    {
        public RemoteException()
        {
        }

        public RemoteException(RemoteExceptionAdapter adapter) : base(adapter.Message)
        {
        }
    }
}