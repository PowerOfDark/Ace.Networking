using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Serializers;
using Ace.Networking.Threading;

namespace Ace.Networking
{
    public class ProtocolConfiguration
    {
        public static ProtocolConfiguration Instance = new ProtocolConfiguration();

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

        public ProtocolConfiguration()
        {
            var serializer = new MsgPackSerializer();
            PayloadEncoder = new MicroEncoder(serializer.Clone());
            PayloadDecoder = new MicroDecoder(serializer.Clone());
            Initialize();
        }

        public IPayloadEncoder PayloadEncoder { get; protected set; }
        public IPayloadDecoder PayloadDecoder { get; protected set; }

        public SslMode SslMode { get; set; }

        public bool RequireClientCertificate { get; protected set; }
        public ThreadedQueueProcessor<SendMessageQueueItem> CustomOutcomingMessageQueue { get; protected set; }
        public ThreadedQueueProcessor<ReceiveMessageQueueItem> CustomIncomingMessageQueue { get; protected set; }

        protected virtual void Initialize()
        {
            if (IsInitialized) return;
            IsInitialized = true;


            PayloadEncoder.Serializer.RegisterAssembly(GetType().GetTypeInfo().Assembly);
            PayloadEncoder.Serializer.RegisterAssembly(typeof(Connection).GetTypeInfo().Assembly);
            if (PayloadEncoder.Serializer != PayloadDecoder.Serializer)
            {
                PayloadDecoder.Serializer.RegisterAssembly(GetType().GetTypeInfo().Assembly);
                PayloadDecoder.Serializer.RegisterAssembly(typeof(Connection).GetTypeInfo().Assembly);
            }
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