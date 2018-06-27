using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MessagePack;
using ProtoBuf;

namespace Ace.Networking.Entanglement.Structures
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("BC3274A7-D456-41A4-AC04-AA6E03C303E3")]
    [MessagePackObject]
    public class RemoteExceptionAdapter
    {
        [Key(0)]
        public string Message { get; protected set; }
        public RemoteExceptionAdapter()
        {

        }

        public RemoteExceptionAdapter(string message)
        {
            this.Message = message;
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
    }
}
