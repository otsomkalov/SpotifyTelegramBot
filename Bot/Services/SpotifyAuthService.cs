using System.Threading.Tasks;
using Bot.Services.Interfaces;
using Bot.Settings;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;

namespace Bot.Services
{
    public class SpotifyAuthService : ISpotifyAuthService
    {
        private readonly CredentialsAuth _authData;
        private Token _token;

        public SpotifyAuthService(SpotifySettings spotifySettings)
        {
            _authData = new CredentialsAuth(spotifySettings.ClientId, spotifySettings.ClientSecret);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_token == null || _token.IsExpired())
            {
                _token = await _authData.GetToken();
            }

            return _token.AccessToken;
        }
    }
}