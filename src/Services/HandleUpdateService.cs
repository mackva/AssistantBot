using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly string _botName;
    private const bool _silentMode = true; //TODO Use internal state

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger)
    {
        _botClient = botClient;
        _logger = logger;
        _botName = "MissJuliaBot";
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage => BotOnMessageReceived(update.EditedMessage!),
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
#pragma warning disable CA1031
        catch (Exception exception)
#pragma warning restore CA1031
        {
            await HandleErrorAsync(exception);
        }
    }
    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Type != MessageType.Text)
        {
            return;
        }

        _logger.LogInformation("Receive message text: {message}", message.Text);

        if (!message.Text!.Contains($"@{_botName}", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        var command = message.Text!.Split(" ")[0];

        if (command.Equals($"/pin@{_botName}", StringComparison.InvariantCultureIgnoreCase))
        {
            await PinMessage(_botClient, message, _silentMode);
        }
        else if (command.Equals($"/help@{_botName}", StringComparison.InvariantCultureIgnoreCase))
        {
            await Usage(_botClient, message, _botName);
        }
        else
        {
            return;
        }

        static async Task PinMessage(ITelegramBotClient bot, Message message, bool silentMode)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            if (message.ReplyToMessage != null)
            {
                try
                {
                    await bot.PinChatMessageAsync(message.Chat.Id, message.ReplyToMessage.MessageId, false);
                }
                catch (ApiRequestException ex) when (ex.ErrorCode == 400)
                {
                    await SendTextMessageAsync(bot, message.Chat.Id, "Looks like I dont have permission to pin messages. Could you please promote me?", silentMode, replyToMessageId: message.MessageId);
                }
            }
            else
            {
                await SendTextMessageAsync(bot, message.Chat.Id, "You need to reply to a message to pin it!", silentMode, replyToMessageId: message.MessageId);
            }

            await DeleteMessageAsync(bot, message.Chat.Id, message.MessageId, silentMode);
        }

        static async Task<Message> Usage(ITelegramBotClient bot, Message message, string botName)
        {
            var usage = "Usage:\n" +
                        "/pin@" + botName + "   - Pin the message you replied to.\n";

            return await bot.SendTextMessageAsync(message.Chat.Id, usage, replyMarkup: new ReplyKeyboardRemove(), replyToMessageId: message.MessageId);
        }
    }

    private static async Task DeleteMessageAsync(ITelegramBotClient bot, long chatId, int messageId, bool silentMode)
    {
        try
        {
            await bot.DeleteMessageAsync(chatId, messageId);
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 400)
        {
            await SendTextMessageAsync(bot, chatId, "Looks like I dont have permission to delete messages. Could you please promote me?", silentMode, replyToMessageId: messageId);
        }
    }

    private static async Task SendTextMessageAsync(ITelegramBotClient bot, long chatId, string text, bool silentMode, int? replyToMessageId = null)
    {
        if (!silentMode)
        {
            await bot.SendTextMessageAsync(chatId, text, replyToMessageId: replyToMessageId);
        }
    }


    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(Exception exception)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }
}
