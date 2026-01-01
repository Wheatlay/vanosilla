using DiscordNotifier.Formatting;
using Plugin.PlayerLogs.Messages;

namespace DiscordNotifier.Consumers.Player
{
    public class LogStrangeBehaviorMessageFormatter : IDiscordLogFormatter<LogStrangeBehaviorMessage>
    {
        public LogType LogType => LogType.STRANGE_BEHAVIORS;

        public bool TryFormat(LogStrangeBehaviorMessage message, out string formattedString)
        {
            formattedString =
                $"{message.CreatedAt:yyyy-MM-dd HH:mm:ss} | CHANNEL {message.ChannelId} | {message.CharacterName} | [{message.SeverityType}] -> {message.Message}";
            return true;
        }
    }
}