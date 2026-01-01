using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus.Protocol;
using PhoenixLib.ServiceBus.Routing;

namespace PhoenixLib.ServiceBus.MQTT
{
    internal sealed class MqttMessagingService : IMessagingService
    {
        private static readonly MethodInfo HandleMessageMethod =
            typeof(MqttMessagingService).GetMethod(nameof(HandleMessageReceived), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly IManagedMqttClient _client;
        private readonly ManagedMqttClientOptions _options;
        private readonly IMessageSerializer _packetSerializer;
        private readonly IServiceProvider _provider;
        private readonly HashSet<string> _queues;
        private readonly IMessageRouter _router;
        private readonly IServiceBusInstance _serviceBusInstance;
        private TaskCompletionSource<bool> _clientConnectionReady;

        public MqttMessagingService(MqttConfiguration conf, IMessageRouter router, IServiceProvider provider, IServiceBusInstance serviceBusInstance, IMessageSerializer packetSerializer)
        {
            _router = router;
            _provider = provider;
            _serviceBusInstance = serviceBusInstance;
            _packetSerializer = packetSerializer;


            _client = new MqttFactory().CreateManagedMqttClient(new MqttNetLogger(conf.ClientName));
            _queues = new HashSet<string>();
            _options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId($"{conf.ClientName}-{_serviceBusInstance.Id.ToString()}")
                    .WithTcpServer(conf.Address, conf.Port)
                    .Build())
                .Build();
            // event handlers
            _client.UseApplicationMessageReceivedHandler(Client_OnMessage);
        }

        private bool IsInitialized => _client.IsStarted;


        public async Task SendAsync<T>(T eventToSend) where T : IMessage
        {
            if (!IsInitialized)
            {
                await StartAsync();
            }

            Log.Debug($"[SERVICE_BUS][PUBLISHER] Sending<{typeof(T)}>...");
            await SendAsync<T>(_packetSerializer.ToMessage(eventToSend));
        }

        public async Task StartAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            Log.Debug("[SERVICE_BUS][SUBSCRIBER] Starting...");
            _client.UseConnectedHandler(new MqttClientConnectedHandlerDelegate(WaitReadyAsync));
            _clientConnectionReady = new TaskCompletionSource<bool>(TimeSpan.FromSeconds(10));
            await _client.StartAsync(_options);
            await _clientConnectionReady.Task;
            if (_clientConnectionReady.Task.IsCanceled)
            {
                throw new Exception("Could not connect to MQTT broker within 10 seconds");
            }

            Log.Debug("[SERVICE_BUS][SUBSCRIBER] Started !");
            await SubscribeRegisteredEventsAsync();
        }

        public async ValueTask DisposeAsync() => _client.Dispose();

        private async Task SendAsync<T>(MqttApplicationMessage container)
        {
            IRoutingInformation infos = GetRoutingInformation<T>();
            await _client.PublishAsync(container);
            Log.Debug($"[SERVICE_BUS][PUBLISHER] Message sent from : {_client.Options.ClientOptions.ClientId} topic {infos.Topic}");
        }

        private async Task Client_OnMessage(MqttApplicationMessageReceivedEventArgs mqttEventArgs)
        {
            (object message, Type objType) = _packetSerializer.FromMessage(mqttEventArgs.ApplicationMessage);

            if (message == null || objType == null)
            {
                return;
            }

            try
            {
                MethodInfo method = HandleMessageMethod.MakeGenericMethod(objType);
                var task = (Task)method.Invoke(this, new[] { message });
                await task;
            }
            catch (Exception e)
            {
                Log.Error($"Client_OnMessage<{objType.Name}", e);
                throw;
            }
        }

        private async Task HandleMessageReceived<T>(T message)
        {
            try
            {
                IEnumerable<IMessageConsumer<T>> tmp = _provider.GetServices<IMessageConsumer<T>>();
                var cts = new CancellationTokenSource();
                foreach (IMessageConsumer<T> subscriber in tmp)
                {
                    await subscriber.HandleAsync(message, cts.Token);
                }
            }
            catch (Exception e)
            {
                Log.Error($"HandleMessageReceived<{typeof(T).Name}", e);
                throw;
            }
        }

        private async Task TrySubscribeAsync(IRoutingInformation infos)
        {
            if (_queues.Contains(infos.Topic))
            {
                return;
            }

            await _client.SubscribeAsync(infos.Topic);
            _queues.Add(infos.Topic);
            Log.Debug($"[SERVICE_BUS][SUBSCRIBER] Subscribed to topic : {infos.Topic} with eventType: {infos.EventType}");
        }

        private IRoutingInformation GetRoutingInformation<T>() => GetRoutingInformation(typeof(T));

        private IRoutingInformation GetRoutingInformation(Type type)
        {
            IRoutingInformation routingInfos = _router.GetRoutingInformation(type);
            if (string.IsNullOrEmpty(routingInfos.Topic))
            {
                throw new ArgumentException("routing information couldn't be retrieved");
            }

            return routingInfos;
        }


        private async Task SubscribeRegisteredEventsAsync()
        {
            IEnumerable<ISubscribedMessage> subs = _provider.GetServices<ISubscribedMessage>();

            foreach (ISubscribedMessage sub in subs)
            {
                IRoutingInformation routingInfo = GetRoutingInformation(sub.Type);
                await TrySubscribeAsync(routingInfo);
            }
        }

        private async Task WaitReadyAsync(MqttClientConnectedEventArgs onConnectedArgs)
        {
            if (onConnectedArgs.AuthenticateResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                _clientConnectionReady.SetResult(true);
            }
        }
    }
}