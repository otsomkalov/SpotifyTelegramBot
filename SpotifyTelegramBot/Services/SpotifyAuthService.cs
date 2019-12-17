using System.Threading.Tasks;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;
using SpotifyTelegramBot.Services.Interfaces;
using SpotifyTelegramBot.Settings.Interfaces;

namespace SpotifyTelegramBot.Services
{
    public class SpotifyAuthService : ISpotifyAuthService
    {
        private readonly CredentialsAuth _authData;
        private Token _token;

        public SpotifyAuthService(ISpotifySettings spotifySettings)
        {
            _authData = new CredentialsAuth(spotifySettings.ClientId, spotifySettings.ClientSecret);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_token == null || _token.IsExpired())
                _token = await _authData.GetToken();

            return _token.AccessToken;
        }
    }
}
