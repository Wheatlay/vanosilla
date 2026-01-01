using System;
using System.Collections.Generic;
using Discord;
using DiscordNotifier.Formatting;
using Plugin.PlayerLogs.Messages.Family;

namespace DiscordNotifier.Consumers.Family
{
    public class LogFamilyCreatedEmbedMessageFormatter : IDiscordEmbedLogFormatter<LogFamilyCreatedMessage>
    {
        public LogType LogType => LogType.PLAYERS_EVENTS_CHANNEL;

        public bool TryFormat(LogFamilyCreatedMessage message, out List<EmbedBuilder> embeds)
        {
            embeds = new List<EmbedBuilder>
            {
                new()
                {
                    Author = new EmbedAuthorBuilder { IconUrl = "https://avatars0.githubusercontent.com/u/40839221?s=200" },
                    Title = "[NosWings] Family created",
                    Description = $"Family {message.FamilyName} has been created",
                    Color = Color.Orange,
                    Footer = new EmbedFooterBuilder().WithIconUrl("https://avatars0.githubusercontent.com/u/40839221?s=200").WithText("discord-notifier microservice"),
                    //ImageUrl = "https://avatars0.githubusercontent.com/u/40839221?s=200",
                    // ThumbnailUrl = "https://avatars0.githubusercontent.com/u/40839221?s=200",
                    Timestamp = DateTimeOffset.Now
                }
            };
            return true;
        }
    }
}