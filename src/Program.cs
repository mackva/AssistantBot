using Microsoft.Extensions.DependencyInjection;
using System;
using Telegram.Bot;
using Telegram.Bot.Examples.WebHook;
using Telegram.Bot.Examples.WebHook.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var botToken = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BotToken")) ? Environment.GetEnvironmentVariable("BotToken") : builder.Configuration["BotConfiguration:BotToken"];
var hostAddress = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HostAddress")) ? Environment.GetEnvironmentVariable("HostAddress") : builder.Configuration["BotConfiguration:HostAddress"];

builder.Services.AddHostedService<ConfigureWebhook>(serviceProvider => new ConfigureWebhook(
            serviceProvider.GetService<ILogger<ConfigureWebhook>>(),
            serviceProvider.GetService<IServiceProvider>(),
            new BotConfiguration { BotToken = botToken, HostAddress = hostAddress }));

builder.Services.AddHttpClient("tgwebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botToken, httpClient));
builder.Services.AddScoped<HandleUpdateService>();
builder.Services.AddControllers().AddNewtonsoftJson();


var portVar = Environment.GetEnvironmentVariable("PORT");
if (portVar is { Length: > 0 } && int.TryParse(portVar, out int port))
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(port);
    });
}

var app = builder.Build();

app.UseRouting();
app.UseCors();
app.MapHealthChecks("/health");

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(name: "tgwebhook",
                                 pattern: $"bot/{botToken}",
                                 new { controller = "Webhook", action = "Post" });
    endpoints.MapControllers();
});

app.Run();
