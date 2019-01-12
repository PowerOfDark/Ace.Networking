using System;
using System.Collections.Generic;
using System.Text;

namespace Ace.Networking.Memory
{
    public class MemoryManager
    {
        private static RecyclableMemoryStreamManager _instance;
        private static object _lock = new object();
        private static volatile bool _created = false;


        public static RecyclableMemoryStreamManager Instance
        {
            get
            {
                if (_created) return _instance;
                lock (_lock)
                {
                    if (_created) return _instance;
                    _created = true;
                    _instance = new RecyclableMemoryStreamManager(2.0);
                }

                return _instance;
            }
        }
    }
}
