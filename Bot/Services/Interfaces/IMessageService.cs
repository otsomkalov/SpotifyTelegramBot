namespace Bot.Services.Interfaces;

public interface IMessageService
{
    Task HandleAsync(Message message);
}