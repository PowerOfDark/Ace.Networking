using System;
using System.Runtime.InteropServices;

using ProtoBuf;

namespace Ace.Networking.Entanglement.Structures
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("BC3274A7-D456-41A4-AC04-AA6E03C303E3")]
    public class RemoteExceptionAdapter
    {
        public RemoteExceptionAdapter()
        {
        }

        public RemoteExceptionAdapter(string message)
        {
            Message = message;
        }

        public RemoteExceptionAdapter(string message, Exception innerException)
        {
            Message = message;
            var ex = innerException;
            while (ex != null)
            {
                Message += $"{Environment.NewLine}[{ex.GetType().Name} in {ex.Source}] " + ex.Message;
                ex = ex.InnerException;
            }
        }

        public string Message { get; protected set; }
    }
}