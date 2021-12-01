namespace Bot.Services.Interfaces;

public interface IInlineQueryService
{
    Task HandleAsync(InlineQuery inlineQuery);
}