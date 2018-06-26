using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ace.Networking.Interfaces
{
    public interface IRequestWrapper
    {
        object Request { get; }
        IConnection Connection { get; }
        Task SendResponse<T>(T response);
    }
}
