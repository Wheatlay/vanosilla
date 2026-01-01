using System;
using DiscordNotifier.Consumers.Chat;
using DiscordNotifier.Consumers.Family;
using DiscordNotifier.Consumers.GameEvents;
using DiscordNotifier.Consumers.Item;
using DiscordNotifier.Consumers.Maintenance;
using DiscordNotifier.Consumers.Minigame;
using DiscordNotifier.Consumers.Player;
using DiscordNotifier.Discord;
using DiscordNotifier.Formatting;
using DiscordNotifier.Managers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PhoenixLib.Caching;
using PhoenixLib.Configuration;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus.Extensions;
using Plugin.Database;
using Plugin.PlayerLogs.Messages;
using Plugin.PlayerLogs.Messages.Family;
using Plugin.PlayerLogs.Messages.LevelUp;
using Plugin.PlayerLogs.Messages.Miniland;
using Plugin.PlayerLogs.Messages.Player;
using Plugin.PlayerLogs.Messages.Upgrade;
using Plugin.ResourceLoader;
using WingsAPI.Communication.InstantBattle;
using WingsAPI.Communication.Services.Messages;
using WingsEmu.Health.Extensions;

namespace DiscordNotifier
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMqttConfigurationFromEnv();
            services.AddEventPipeline();
            services.AddEventHandlersInAssembly<Startup>();
            services.AddMaintenanceMode();
            services.AddPhoenixLogging();

            services.TryAddSingleton(typeof(ILongKeyCachedRepository<>), typeof(InMemoryCacheRepository<>));

            new FileResourceLoaderPlugin().AddDependencies(services);

            // discord
            services.AddYamlConfigurationHelper();
            services.AddSingleton(new DiscordWebhookConfiguration
            {
                { LogType.PLAYERS_EVENTS_CHANNEL, Environment.GetEnvironmentVariable("WINGSEMU_DISCORD_WEBHOOK_URL") },
                { LogType.CHAT_FAMILIES, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_CHAT_FAMILIES") },
                { LogType.CHAT_FRIENDS, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_CHAT_FRIENDS") },
                { LogType.CHAT_GENERAL, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_CHAT_GENERAL") },
                { LogType.CHAT_GROUPS, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_CHAT_GROUPS") },
                { LogType.CHAT_SPEAKERS, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_CHAT_SPEAKERS") },
                { LogType.CHAT_WHISPERS, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_CHAT_WHISPERS") },
                { LogType.FARMING_LEVEL_UP, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_FARMING_LEVEL_UP") },
                { LogType.COMMANDS_PLAYER_COMMAND_EXECUTED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_PLAYER_COMMAND_EXECUTED") },
                { LogType.COMMANDS_GM_COMMAND_EXECUTED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_GM_COMMAND_EXECUTED") },
                { LogType.FAMILY_CREATED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_FAMILY_CREATED") },
                { LogType.FAMILY_DISBANDED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_FAMILY_DISBANDED") },
                { LogType.FAMILY_JOINED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_FAMILY_JOINED") },
                { LogType.FAMILY_LEFT, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_FAMILY_LEFT") },
                { LogType.FAMILY_KICK, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_FAMILY_KICK") },
                { LogType.FAMILY_MESSAGES, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_FAMILY_MESSAGES") },
                { LogType.MINIGAME_REWARD_CLAIMED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_MINIGAME_REWARDS_CLAIMED") },
                { LogType.MINIGAME_SCORE, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_MINIGAME_SCORE") },
                { LogType.ITEM_GAMBLED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_ITEM_GAMBLED") },
                { LogType.ITEM_UPGRADED, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_ITEM_UPGRADED") },
                { LogType.STRANGE_BEHAVIORS, Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_STRANGE_BEHAVIORS") }
            });
            services.AddSingleton<IDiscordWebhookLogsService, DiscordWebhookLogsService>();
            services.AddSingleton<ItemManager>();

            new DatabasePlugin().AddDependencies(services);

            services.AddDiscordFormattedLog<LogPlayerChatMessage, LogChatMessageMessageFormatter>();
            services.AddDiscordFormattedLog<LogLevelUpCharacterMessage, LogPlayerLevelUpMessageFormatter>();
            services.AddDiscordFormattedLog<LogPlayerCommandExecutedMessage, LogPlayerCommandExecutedMessageFormatter>();
            services.AddDiscordFormattedLog<LogGmCommandExecutedMessage, LogGmCommandExecutedMessageFormatter>();

            // Family discord
            services.AddDiscordFormattedLog<LogFamilyCreatedMessage, LogFamilyCreatedMessageFormatter>();
            services.AddDiscordFormattedLog<LogFamilyDisbandedMessage, LogFamilyDisbandedMessageFormatter>();
            services.AddDiscordFormattedLog<LogFamilyJoinedMessage, LogFamilyJoinedMessageFormatter>();
            services.AddDiscordFormattedLog<LogFamilyKickedMessage, LogFamilyKickedMessageFormatter>();
            services.AddDiscordFormattedLog<LogFamilyLeftMessage, LogFamilyLeftMessageFormatter>();
            services.AddDiscordFormattedLog<LogFamilyMessageMessage, LogFamilyMessageMessageFormatter>();
            services.AddDiscordEmbedFormattedLog<LogFamilyCreatedMessage, LogFamilyCreatedEmbedMessageFormatter>();

            // Minigames discord
            services.AddDiscordFormattedLog<LogMinigameRewardClaimedMessage, LogMinigameRewardClaimedMessageFormatter>();
            services.AddDiscordFormattedLog<LogMinigameScoreMessage, LogMinigameScoreMessageFormatter>();

            // Items discord
            services.AddDiscordFormattedLog<LogItemGambledMessage, LogItemGambledMessageFormatter>();
            services.AddDiscordFormattedLog<LogItemUpgradedMessage, LogItemUpgradedMessageFormatter>();

            services.AddDiscordFormattedLog<LogStrangeBehaviorMessage, LogStrangeBehaviorMessageFormatter>();

            services.AddMessageSubscriber<InstantBattleStartMessage, LogInstantBattleStartDiscordConsumer>();

            // healthcheck
            services.AddMessageSubscriber<ServiceDownMessage, ServiceDownMessageConsumer>();

            // maintenance
            services.AddMessageSubscriber<ServiceMaintenanceNotificationMessage, ServiceMaintenanceNotificationMessageConsumer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
        }
    }
}