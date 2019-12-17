using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpotifyTelegramBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SpotifyTelegramBot
{
    public class UpdateFunctions
    {
        private readonly IInlineQueryService _inlineQueryService;
        private readonly IMessageService _messageService;

        public UpdateFunctions(IMessageService messageService, IInlineQueryService inlineQueryService)
        {
            _messageService = messageService;
            _inlineQueryService = inlineQueryService;
        }

        [FunctionName(nameof(ProcessUpdateAsync))]
        public async Task<IActionResult> ProcessUpdateAsync(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "update")]
            string updateString,
            HttpRequest request,
            ILogger logger)
        {
            var update = JsonConvert.DeserializeObject<Update>(updateString);

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
}
