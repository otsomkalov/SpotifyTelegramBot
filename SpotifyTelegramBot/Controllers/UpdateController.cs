using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpotifyTelegramBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SpotifyTelegramBot.Controllers
{
    [ApiController]
    [Route("api/update")]
    public class UpdateController : ControllerBase
    {
        private readonly IInlineQueryService _inlineQueryService;
        private readonly IMessageService _messageService;

        public UpdateController(IInlineQueryService inlineQueryService, IMessageService messageService)
        {
            _inlineQueryService = inlineQueryService;
            _messageService = messageService;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessUpdateAsync(Update update)
        {
            switch (update.Type)
            {
                case UpdateType.Message:

                    await _messageService.HandleAsync(update.Message);

                    break;

                case UpdateType.InlineQuery:

                    await _inlineQueryService.HandleAsync(update.InlineQuery);

                    break;
            }

            return Ok();
        }
    }
}
