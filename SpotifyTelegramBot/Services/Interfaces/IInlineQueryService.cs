using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace SpotifyTelegramBot.Services.Interfaces
{
    public interface IInlineQueryService
    {
        Task HandleAsync(InlineQuery inlineQuery);
    }
}
