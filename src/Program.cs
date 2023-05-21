using Telegram.Bot;
using Telegram.Bot.Examples.WebHook;
using Telegram.Bot.Examples.WebHook.Services;
using Telegram.Bot.Examples.WebHook.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

builder.Services.Configure<GStorageConfiguration>(builder.Configuration.GetSection(GStorageConfiguration.Key));
builder.Services.AddSingleton<IGStorageSessionFactory, GStorageSessionFactory>();
builder.Services.AddSingleton<IDistributedStorage, DistributedStorage>();


var botConfigSection = builder.Configuration.GetSection(BotConfiguration.Key);
builder.Services.Configure<BotConfiguration>(botConfigSection);
var botConfig = botConfigSection.Get<BotConfiguration>();
builder.Services.AddHostedService<ConfigureWebhook>();


builder.Services.AddHttpClient("tgwebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfig.BotToken, httpClient));
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

var sessionFactory = app.Services.GetRequiredService<IGStorageSessionFactory>();
if (sessionFactory is GStorageSessionFactory gStorageSessionFactory)
{
    await gStorageSessionFactory.InitAsync();
}

app.UseRouting();
app.UseCors();
app.MapHealthChecks("/health");

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(name: "tgwebhook",
                                 pattern: $"bot/{botConfig.BotToken}",
                                 new { controller = "Webhook", action = "Post" });
    endpoints.MapControllers();
});

app.Run();