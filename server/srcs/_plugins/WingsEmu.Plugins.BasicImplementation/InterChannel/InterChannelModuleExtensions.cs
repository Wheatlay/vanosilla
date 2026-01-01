using Microsoft.Extensions.DependencyInjection;
using PhoenixLib.ServiceBus.Extensions;
using WingsEmu.Plugins.BasicImplementations.InterChannel.Consumer;
using WingsEmu.Plugins.DistributedGameEvents.InterChannel;

namespace WingsEmu.Plugins.BasicImplementations.InterChannel;

public static class InterChannelModuleExtensions
{
    public static void AddInterChannelModule(this IServiceCollection services)
    {
        services.AddMessagePublisher<InterChannelSendChatMsgByCharIdMessage>();
        services.AddMessageSubscriber<InterChannelSendChatMsgByCharIdMessage, InterChannelSendChatMsgByCharIdMessageConsumer>();

        services.AddMessagePublisher<InterChannelSendChatMsgByNicknameMessage>();
        services.AddMessageSubscriber<InterChannelSendChatMsgByNicknameMessage, InterChannelSendChatMsgByNicknameMessageConsumer>();

        services.AddMessagePublisher<InterChannelSendWhisperMessage>();
        services.AddMessageSubscriber<InterChannelSendWhisperMessage, InterChannelSendWhisperMessageConsumer>();

        services.AddMessagePublisher<InterChannelSendInfoByCharIdMessage>();
        services.AddMessageSubscriber<InterChannelSendInfoByCharIdMessage, InterChannelSendInfoByCharIdMessageConsumer>();

        services.AddMessagePublisher<InterChannelSendInfoByNicknameMessage>();
        services.AddMessageSubscriber<InterChannelSendInfoByNicknameMessage, InterChannelSendInfoByNicknameMessageConsumer>();

        services.AddMessagePublisher<InterChannelChatMessageBroadcastMessage>();
        services.AddMessageSubscriber<InterChannelChatMessageBroadcastMessage, InterChannelChatMessageBroadcastMessageConsumer>();

        services.AddMessagePublisher<ChatShoutAdminMessage>();
        services.AddMessageSubscriber<ChatShoutAdminMessage, ChatShoutAdminMessageConsumer>();

        services.AddMessagePublisher<BazaarNotificationMessage>();
        services.AddMessageSubscriber<BazaarNotificationMessage, BazaarNotificationMessageConsumer>();
    }
}