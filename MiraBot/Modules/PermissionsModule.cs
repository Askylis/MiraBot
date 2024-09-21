using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Microsoft.Extensions.DependencyInjection;
using MiraBot.DataAccess;

namespace MiraBot.Modules
{
    public class PermissionsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactive;

        public PermissionsModule(InteractiveService interactive)
        {
            _interactive = interactive;
        }

        [SlashCommand("addpermission", "Add a permission to a user.")]
        public async Task AddPermissionAsync()
        {

        }

        [SlashCommand("removepermission", "Remove a permission from a user.")]
        public async Task RemovePermissionAsync()
        {

        }

        [SlashCommand("ban", "Blacklist a user from using Mira.")]
        public async Task BanUserAsync()
        {

        }

        [SlashCommand("unban", "Unban a user who was previously blacklisted from using Mira.")]
        public async Task UnbanUserAsync()
        {

        }

        [SlashCommand("newpermission", "Add a new permission to the database.")]
        public async Task AddNewPermissionAsync()
        {

        }

        [SlashCommand("deletepermission", "Deletes an existing permission from the database.")]
        public async Task DeleteExistingPermissionAsync()
        {

        }
    }
}