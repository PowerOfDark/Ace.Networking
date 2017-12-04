using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Threading;
using System.Reflection;
using Ace.Networking.ProtoBuf;

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
            var serializer = new GuidProtoBufSerializer();
            PayloadEncoder = new MicroProtocol.MicroEncoder(serializer.Clone());
            PayloadDecoder = new MicroProtocol.MicroDecoder(serializer.Clone());
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
            if (IsInitialized)
            {
                return;
            }
            IsInitialized = true;

            if(PayloadEncoder.Serializer is GuidProtoBufSerializer || PayloadDecoder.Serializer is GuidProtoBufSerializer)
            {
                GuidProtoBufSerializer.RegisterAssembly(this.GetType().GetTypeInfo().Assembly);
            }

            if (PayloadEncoder.Serializer is ProtoBufSerializer || PayloadDecoder.Serializer is ProtoBufSerializer)
            {
                ProtoBufSerializer.RegisterAssembly(this.GetType().GetTypeInfo().Assembly);
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