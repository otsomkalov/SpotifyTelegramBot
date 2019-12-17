using System.Threading.Tasks;

namespace SpotifyTelegramBot.Services.Interfaces
{
    public interface ISpotifyAuthService
    {
        Task<string> GetAccessTokenAsync();
    }
}
