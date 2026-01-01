using System;
using System.Net.Mime;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Mqtt;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus.Routing;

namespace PhoenixLib.ServiceBus.Protocol
{
    internal class CloudEventsJsonMessageSerializer : IMessageSerializer
    {
        private static readonly JsonSerializerSettings _settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly IServiceBusInstance _busInstance;
        private readonly IMessageRouter _messageRouter;

        public CloudEventsJsonMessageSerializer(IServiceBusInstance busInstance, IMessageRouter messageRouter)
        {
            _busInstance = busInstance;
            _messageRouter = messageRouter;
        }

        public MqttApplicationMessage ToMessage<T>(T packet) where T : IMessage
        {
            IRoutingInformation routingInfos = _messageRouter.GetRoutingInformation<T>();
            MqttApplicationMessage tmp = Create(routingInfos, packet, _busInstance.Id.ToString());
            return tmp;
        }

        public (object obj, Type objType) FromMessage(MqttApplicationMessage message)
        {
            var container = message.ToCloudEvent(new JsonEventFormatter());
            if (container.Source.OriginalString.Contains(_busInstance.Id.ToString()))
            {
                Log.Debug("[SERVICE_BUS][SUBSCRIBER] Message received from myself");
                // should take a look to broker's ACL
                // https://stackoverflow.com/questions/59565487/mqtt-message-subscription-all-except-me
                return (null, null);
            }

            if (!(container.Data is string eventContent))
            {
                Log.Debug("container.Data as string == null");
                throw new ArgumentNullException(nameof(container.Data));
            }

            IRoutingInformation routingInformation = _messageRouter.GetRoutingInformation(container.Type);


            Log.Debug($"[SERVICE_BUS][SUBSCRIBER] Message received from sender : {container.Source} topic {message.Topic}");
            object packet = JsonConvert.DeserializeObject(eventContent, routingInformation.ObjectType);
            return (packet, routingInformation.ObjectType);
        }

        private static MqttApplicationMessage Create(IRoutingInformation routingInformation, object content, string source)
        {
            var cloudEvent = new CloudEvent(CloudEventsSpecVersion.V1_0, routingInformation.EventType, new Uri("publisher:" + source), Guid.NewGuid().ToString(), DateTime.UtcNow)
            {
                DataContentType = new ContentType(MediaTypeNames.Application.Json),
                Data = JsonConvert.SerializeObject(content, _settings)
            };
            return new MqttCloudEventMessage(cloudEvent, new JsonEventFormatter()) { Topic = routingInformation.Topic, QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce };
        }
    }
}