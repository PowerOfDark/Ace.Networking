using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ace.Networking.Services;

namespace Ace.Networking.Interfaces
{
    public interface IInternalStreamWrapper : IService<IConnection>
    {
        Stream Wrap(Stream stream);
    }
}
