using System;
using System.Threading.Tasks;
using Bot.Services.Interfaces;
using Bot.Settings;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;

namespace Bot.Services
{
    public class SpotifyAuthService : ISpotifyAuthService
    {
        private readonly CredentialsAuth _authData;
        private Token _token;

        public SpotifyAuthService(IOptions<SpotifySettings> spotifySettings)
        {
            var settings = spotifySettings?.Value ?? throw new ArgumentNullException(nameof(spotifySettings));
            
            _authData = new CredentialsAuth(settings.ClientId, settings.ClientSecret);
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