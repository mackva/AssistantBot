using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Examples.WebHook.Services;

public class ConfigureWebhook : IHostedService
{
    private readonly ILogger<ConfigureWebhook> _logger;
    private readonly IServiceProvider _services;
    private readonly BotConfiguration _botConfig;

    public ConfigureWebhook(ILogger<ConfigureWebhook> logger,
                            IServiceProvider serviceProvider,
                            IOptions<BotConfiguration> botConfiguration)
    {
        _logger = logger;
        _services = serviceProvider;
        _botConfig = botConfiguration.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Configure custom endpoint per Telegram API recommendations:
        // https://core.telegram.org/bots/api#setwebhook
        var expectedWebhookAddress = @$"{_botConfig.HostAddress}/bot/{_botConfig.BotToken}";

        var attempts = 3;
        string actualWebhookAddress;
        do
        {
            _logger.LogInformation("Setting webhook: {WebhookAddress}", expectedWebhookAddress);
            await botClient.SetWebhookAsync(
                url: expectedWebhookAddress,
                allowedUpdates: Array.Empty<UpdateType>(),
                cancellationToken: cancellationToken);

            var webhookInfo = await botClient.GetWebhookInfoAsync(cancellationToken);
            actualWebhookAddress = webhookInfo.Url;
            attempts -= 1;

            await Task.Delay(5000);
        } while (attempts > 0 && !expectedWebhookAddress.Equals(actualWebhookAddress, StringComparison.InvariantCultureIgnoreCase));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Remove webhook upon app shutdown
        _logger.LogInformation("Removing webhook");
        await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }
}
