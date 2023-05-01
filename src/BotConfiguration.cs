namespace Telegram.Bot.Examples.WebHook;

public class BotConfiguration
{
    public const string Key = "BotConfiguration";
    public string BotToken { get; init; } = default!;
    public string HostAddress { get; init; } = default!;
}
