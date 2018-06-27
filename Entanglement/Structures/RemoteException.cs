using System;
using System.Collections.Generic;
using System.Text;

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
