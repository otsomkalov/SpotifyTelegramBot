using System.Threading.Tasks;

namespace Bot.Services.Interfaces
{
    public interface ISpotifyAuthService
    {
        Task<string> GetAccessTokenAsync();
    }
}
