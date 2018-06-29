using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ace.Networking.Interfaces
{
    public interface IMulticastConnectionInterface
    {
        void Close();
        Task Send<T>(T data);
    }
}
