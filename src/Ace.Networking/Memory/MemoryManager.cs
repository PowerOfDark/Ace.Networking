namespace Ace.Networking.Memory
{
    public class MemoryManager
    {
        private static RecyclableMemoryStreamManager _instance;
        private static readonly object _lock = new object();
        private static volatile bool _created;


        public static RecyclableMemoryStreamManager Instance
        {
            get
            {
                if (_created) return _instance;
                lock (_lock)
                {
                    if (_created) return _instance;
                    _instance = new RecyclableMemoryStreamManager(2.0);
                    _created = true;
                }

                return _instance;
            }
        }
    }
}