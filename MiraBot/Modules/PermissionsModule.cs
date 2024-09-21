using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Microsoft.Extensions.DependencyInjection;
using MiraBot.DataAccess.Repositories;
using MiraBot.Permissions;
using System.Text;

namespace MiraBot.Modules
{
    public class PermissionsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly InteractiveService _interactive;
        private readonly PermissionsRepository _repository;
        private readonly PermissionsHandler _handler;

        public PermissionsModule(InteractiveService interactive, PermissionsRepository repository, PermissionsHandler handler)
        {
            _interactive = interactive;
            _repository = repository;
            _handler = handler;
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
        public async Task AddNewPermissionAsync(string permissionName, string description)
        {
            await DeferAsync();
            await _handler.AddNewPermissionAsync(permissionName, description);
            var permission = await _handler.FindPermissionAsync(permissionName);
            await FollowupAsync($"Added permission \"**{permissionName}**\" with permissionId **{permission.PermissionId}**.");
        }

        [SlashCommand("deletepermission", "Deletes an existing permission from the database.")]
        public async Task DeleteExistingPermissionAsync()
        {

        }

        [SlashCommand("editpermission", "Edit information about an existing permission.")]
        public async Task EditPermissionAsync()
        {

        }

        [SlashCommand("listpermissions", "Provides a list of all available permissions.")]
        public async Task ListPermissionsAsync()
        {
            await DeferAsync();
            var permissions = await _repository.GetAllAsync();
            if (permissions.Count == 0)
            {
                await FollowupAsync("There are no permissions currently saved.");
                return;
            }
            var sb = new StringBuilder();
            int counter = 0;
            foreach (var permission in permissions)
            {
                sb.Append($"{counter}. Permission name: **{permission.Name}** - Permission ID: **{permission.PermissionId}**");
                counter++;
            }

            await FollowupAsync(sb.ToString());
        }
    }
}