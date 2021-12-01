using Bot.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types.Enums;

namespace Bot.Controllers;

[ApiController]
[Route("/update")]
public class UpdateController : ControllerBase
{
    private readonly IInlineQueryService _inlineQueryService;
    private readonly IMessageService _messageService;

    public UpdateController(IMessageService messageService, IInlineQueryService inlineQueryService)
    {
        _messageService = messageService;
        _inlineQueryService = inlineQueryService;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessUpdateAsync(Update update)
    {
        switch (update.Type)
        {
            case UpdateType.Unknown:
                break;

            case UpdateType.Message:
                await _messageService.HandleAsync(update.Message);

                break;

            case UpdateType.InlineQuery:
                await _inlineQueryService.HandleAsync(update.InlineQuery);

                break;

            case UpdateType.ChosenInlineResult:
                break;

            case UpdateType.CallbackQuery:
                break;

            case UpdateType.EditedMessage:
                break;

            case UpdateType.ChannelPost:
                break;

            case UpdateType.EditedChannelPost:
                break;

            case UpdateType.ShippingQuery:
                break;

            case UpdateType.PreCheckoutQuery:
                break;

            case UpdateType.Poll:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return new OkResult();
    }
}