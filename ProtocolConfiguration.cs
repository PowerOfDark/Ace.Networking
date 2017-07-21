using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Threading;

namespace Ace.Networking
{
    public abstract class ProtocolConfiguration
    {
        protected volatile bool IsInitialized;

        public ProtocolConfiguration(IPayloadEncoder encoder, IPayloadDecoder decoder,
            ThreadedQueueProcessor<SendMessageQueueItem> customOutQueue = null,
            ThreadedQueueProcessor<ReceiveMessageQueueItem> customInQueue = null)
        {
            PayloadEncoder = encoder;
            PayloadDecoder = decoder;
            CustomOutcomingMessageQueue = customOutQueue;
            CustomIncomingMessageQueue = customInQueue;
            Initialize();
        }

        protected ProtocolConfiguration()
        {
        }

        public IPayloadEncoder PayloadEncoder { get; protected set; }
        public IPayloadDecoder PayloadDecoder { get; protected set; }

        public SslMode SslMode { get; set; }

        public bool RequireClientCertificate { get; protected set; }
        public ThreadedQueueProcessor<SendMessageQueueItem> CustomOutcomingMessageQueue { get; protected set; }
        public ThreadedQueueProcessor<ReceiveMessageQueueItem> CustomIncomingMessageQueue { get; protected set; }

        protected virtual void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;
        }

        public virtual ClientSslStreamFactory GetClientSslFactory(string targetCommonName = "",
            X509Certificate2 certificate = null, SslProtocols protocols = SslProtocols.Tls12)
        {
            return null;
        }

        public virtual ServerSslStreamFactory GetServerSslFactory(X509Certificate2 certificate = null)
        {
            return null;
        }
    }
}