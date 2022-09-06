using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Examples.WebHook.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly string  _botName;

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
            UpdateType.Message            => BotOnMessageReceived(update.Message!),
            UpdateType.EditedMessage      => BotOnMessageReceived(update.EditedMessage!),
            _                             => UnknownUpdateHandlerAsync(update)
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

        Message sentMessage = null;
        if (command.Equals($"/pin@{_botName}", StringComparison.InvariantCultureIgnoreCase))
        {
            sentMessage = await PinMessage(_botClient, message);
        }
        else if (command.Equals($"/help@{_botName}", StringComparison.InvariantCultureIgnoreCase))
        {
            sentMessage = await Usage(_botClient, message, _botName);
        } 
        else
        {
            return;
        }


        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage?.MessageId);

        static async Task<Message> PinMessage(ITelegramBotClient bot, Message message)
        {
            await bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

            if (message.ReplyToMessage != null)
            {
                try
                {
                    await bot.PinChatMessageAsync(message.Chat.Id, message.ReplyToMessage.MessageId, false);
                    var link = message.Chat.Type == ChatType.Private ? "the message" : $"[this message](http://t.me/c/{-(message.Chat.Id + 1000000000000)}/{message.ReplyToMessage.MessageId})";
                    return await bot.SendTextMessageAsync(message.Chat.Id, $"I have pinned {link}.", ParseMode.Markdown, replyToMessageId: message.MessageId);
                }
                catch(ApiRequestException ex) when (ex.ErrorCode == 400)
                {
                    return await bot.SendTextMessageAsync(message.Chat.Id, "Looks like I dont have permission to pin messages. Could you please promote me?", replyToMessageId: message.MessageId);
                }
            } 
            else
            {
                return await bot.SendTextMessageAsync(message.Chat.Id, "You need to reply to a message to pin it!", replyToMessageId: message.MessageId);
            }
        }

        static async Task<Message> Usage(ITelegramBotClient bot, Message message, string botName)
        {
            var usage = "Usage:\n" +
                        "/pin@" + botName + "   - Pin the message you replied to.\n";

            return await bot.SendTextMessageAsync(message.Chat.Id, usage, replyMarkup: new ReplyKeyboardRemove(), replyToMessageId: message.MessageId);
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
