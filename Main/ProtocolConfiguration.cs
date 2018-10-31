using System;
using System.IO;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Serializers;
using Ace.Networking.Serializers.Protobuf;
using Ace.Networking.Serializers.TypeResolvers;
using Ace.Networking.Threading;

namespace Ace.Networking
{
    public class ProtocolConfiguration
    {
        public static readonly Type[] Primitives = { typeof(object), typeof(Stream), typeof(byte[]) };
        public static Lazy<ProtocolConfiguration> Instance = new Lazy<ProtocolConfiguration>(() => new ProtocolConfiguration());


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
            TypeResolver = new GuidTypeResolver();
            var serializer = new ProtobufSerializer(TypeResolver);
            
            PayloadEncoder = new MicroEncoder(serializer.Clone());
            PayloadDecoder = new MicroDecoder(serializer.Clone());
            CustomIncomingMessageQueue = GlobalIncomingMessageQueue.Instance;
            CustomOutcomingMessageQueue = GlobalOutcomingMessageQueue.Instance;
            Initialize();
        }

        public ITypeResolver TypeResolver { get; protected set; }
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


            TypeResolver.RegisterAssembly(GetType().GetTypeInfo().Assembly);
            TypeResolver.RegisterAssembly(typeof(Connection).GetTypeInfo().Assembly);
            foreach (var primitive in Primitives)
                TypeResolver.RegisterType(primitive);
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