using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking.MicroProtocol;
using Ace.Networking.MicroProtocol.Interfaces;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Serializers;
using Ace.Networking.Serializers.Protobuf;
using Ace.Networking.Threading;
using Ace.Networking.TypeResolvers;

namespace Ace.Networking
{
    public class ProtocolConfiguration
    {
        public static readonly Type[] Primitives = { typeof(object), typeof(Stream), typeof(byte), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(bool), typeof(sbyte), typeof(DateTime), typeof(void), typeof(short), typeof(ushort), typeof(double), typeof(float), typeof(List<>), typeof(Dictionary<,>)};
        public static Lazy<ProtocolConfiguration> Instance = new Lazy<ProtocolConfiguration>(() => new ProtocolConfiguration());


        protected volatile bool IsInitialized;

        public ProtocolConfiguration(IPayloadEncoder encoder, IPayloadDecoder decoder,
            ThreadedQueueProcessor<SendMessageQueueItem> customOutQueue = null,
            ThreadedQueueProcessor<ReceiveMessageQueueItem> customInQueue = null)
        {
            PayloadEncoder = encoder;
            PayloadDecoder = decoder;
            Serializer = encoder.Serializer;
            CustomOutcomingMessageQueue = customOutQueue;
            CustomIncomingMessageQueue = customInQueue;
            Initialize();
        }

        public ProtocolConfiguration()
        {
            Serializer = new ProtobufSerializer(NetworkingSettings.DefaultTypeResolver);
            
            PayloadEncoder = new MicroEncoder(Serializer.Clone());
            PayloadDecoder = new MicroDecoder(Serializer.Clone());
            CustomIncomingMessageQueue = GlobalIncomingMessageQueue.Instance;
            CustomOutcomingMessageQueue = GlobalOutcomingMessageQueue.Instance;
            Initialize();
        }

        public IPayloadSerializer Serializer { get; protected set; }
        public ITypeResolver TypeResolver => Serializer?.TypeResolver;
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
            return new ClientSslStreamFactory(targetCommonName, certificate, protocols);
        }

        public virtual ServerSslStreamFactory GetServerSslFactory(X509Certificate2 certificate = null, bool useClient = true, SslProtocols protocols = SslProtocols.Tls12)
        {
            return new ServerSslStreamFactory(certificate, useClient, protocols);
        }
    }
}