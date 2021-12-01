using Bot.Helpers;
using Bot.Services.Interfaces;

namespace Bot.Services;

public class MessageService : IMessageService
{
    private readonly ITelegramBotClient _bot;

    public MessageService(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task HandleAsync(Message message)
    {
        if (message.Text.StartsWith("/start"))
        {
            await _bot.SendTextMessageAsync(new(message.From.Id),
                "This bot allows you search & share songs, albums and artists from Spotify. It works on every " +
                "dialog, just type @sptfyqbot in message input",
                replyMarkup: InlineKeyboardMarkupHelpers.GetStartKeyboardMarkup());
        }
    }
}