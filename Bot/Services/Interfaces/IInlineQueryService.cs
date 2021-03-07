using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Bot.Services.Interfaces
{
    public interface IInlineQueryService
    {
        Task HandleAsync(InlineQuery inlineQuery);
    }
}
