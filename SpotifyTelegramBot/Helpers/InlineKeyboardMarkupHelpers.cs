using Telegram.Bot.Types.ReplyMarkups;

namespace SpotifyTelegramBot.Helpers
{
    public static class InlineKeyboardMarkupHelpers
    {
        public static InlineKeyboardMarkup GetStartKeyboardMarkup()
        {
            return new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("🔍 Search songs, albums, artists and playlists"),
                InlineKeyboardButton.WithSwitchInlineQuery("🔗 Find and share songs, albums, artists and playlists")
            });
        }
    }
}