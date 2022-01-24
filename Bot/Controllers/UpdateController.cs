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
    private readonly ILogger<UpdateController> _logger;

    public UpdateController(IMessageService messageService, IInlineQueryService inlineQueryService, ILogger<UpdateController> logger)
    {
        _messageService = messageService;
        _inlineQueryService = inlineQueryService;
        _logger = logger;
    }

    [HttpPost]
    public async Task ProcessUpdateAsync(Update update)
    {
        var processUpdateTask = update.Type switch
        {
            UpdateType.Message => _messageService.HandleAsync(update.Message),
            UpdateType.InlineQuery => _inlineQueryService.HandleAsync(update.InlineQuery),
            _ => Task.CompletedTask
        };

        try
        {
            await processUpdateTask;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during processing update:");
        }
    }
}