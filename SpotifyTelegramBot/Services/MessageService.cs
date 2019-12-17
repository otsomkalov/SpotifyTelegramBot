using System.Threading.Tasks;
using SpotifyTelegramBot.Helpers;
using SpotifyTelegramBot.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SpotifyTelegramBot.Services
{
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
                await _bot.SendTextMessageAsync(new ChatId(message.From.Id),
                    "This bot allows you search & share songs, albums and artists from Spotify. It works on every " +
                    "dialog, just type @ExploreSpotifyBot in message input",
                    replyMarkup: InlineKeyboardMarkupHelpers.GetStartKeyboardMarkup());
        }
    }
}
