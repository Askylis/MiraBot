using Discord;
using Discord.WebSocket;
using MiraBot.DataAccess;

namespace MiraBot.Communication
{
    public class UserCommunications
    {
        private readonly DiscordSocketClient _client;
        public UserCommunications(DiscordSocketClient client)
        {
            _client = client;
        }

        // Will need to update this class to handle blacklisting/whitelisting and such. 
        public async Task SendMessageAsync(User recipient, string content)
        {
            var discordRecipient = await _client.Rest.GetUserAsync(recipient.DiscordId);
            var dm = await discordRecipient.CreateDMChannelAsync();

            await dm.SendMessageAsync(content);
        }
    }
}
