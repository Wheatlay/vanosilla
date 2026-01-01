// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using DiscordNotifier.Discord;
using PhoenixLib.ServiceBus;
using WingsAPI.Communication.InstantBattle;

namespace DiscordNotifier.Consumers.GameEvents
{
    public class LogInstantBattleStartDiscordConsumer : IMessageConsumer<InstantBattleStartMessage>
    {
        private readonly IDiscordWebhookLogsService _discordWebhook;

        public LogInstantBattleStartDiscordConsumer(IDiscordWebhookLogsService discordWebhook) => _discordWebhook = discordWebhook;

        public async Task HandleAsync(InstantBattleStartMessage notification, CancellationToken token)
        {
            if (notification.HasNoDelay)
            {
                EmbedFooterBuilder embedFooterBuilder = new EmbedFooterBuilder().WithIconUrl(StaticHardcodedCode.AvatarUrl).WithText("Instant Combat");
                var embedAuthorBuilder = new EmbedAuthorBuilder { IconUrl = StaticHardcodedCode.AvatarUrl };
                var embedBuilders = new List<EmbedBuilder>
                {
                    new()
                    {
                        Author = embedAuthorBuilder,
                        Title = "[INSTANT-COMBAT] An instant combat has started!",
                        Description = "May fate be in your favor",
                        Color = Color.Orange,
                        Footer = embedFooterBuilder,
                        // todo ThumbnailUrl = $"https://friends111.nostale.club/list/ip/iconId here.png",
                        Timestamp = DateTimeOffset.UtcNow
                    }
                };


                await _discordWebhook.PublishLogsEmbedded(LogType.PLAYERS_EVENTS_CHANNEL, embedBuilders);
            }
            else
            {
                EmbedFooterBuilder embedFooterBuilder = new EmbedFooterBuilder().WithIconUrl(StaticHardcodedCode.AvatarUrl).WithText("Instant Combat");
                var embedAuthorBuilder = new EmbedAuthorBuilder { IconUrl = StaticHardcodedCode.AvatarUrl };
                var embedBuilders = new List<EmbedBuilder>
                {
                    new()
                    {
                        Author = embedAuthorBuilder,
                        Title = "[INSTANT-COMBAT] An instant combat will start in 5 minutes!",
                        Description = "Ready to fight against waves of monsters...?",
                        Color = Color.Orange,
                        Footer = embedFooterBuilder,
                        // todo ThumbnailUrl = $"https://friends111.nostale.club/list/ip/iconId here.png",
                        Timestamp = DateTimeOffset.UtcNow
                    }
                };


                await _discordWebhook.PublishLogsEmbedded(LogType.PLAYERS_EVENTS_CHANNEL, embedBuilders);
            }
        }
    }
}