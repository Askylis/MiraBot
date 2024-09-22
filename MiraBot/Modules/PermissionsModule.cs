using Discord.Commands;
using MiraBot.DataAccess;
using MiraBot.Permissions;

namespace MiraBot.Modules
{
    public class PermissionsModule : ModuleBase<SocketCommandContext>
    {
        private readonly PermissionsHandler _handler;

        public PermissionsModule(PermissionsHandler handler)
        {
            _handler = handler;
        }

        [RequireCustomPermission(1)]
        [Command("addpermission")]
        public async Task AddPermissionAsync(string username, int permissionId)
        {
            var recipient = await _handler.FindUserByNameAsync(username);
            if (recipient is null)
            {
                await Context.Channel.SendMessageAsync($"Could not find specified user: **\"{username}\"**.");
            }

            var permission = await _handler.FindPermissionAsync(permissionId);
            if (permission is null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find a permission with the specified ID.");
            }

            await _handler.AddPermissionToUserAsync(recipient, permission);
            await Context.Channel.SendMessageAsync($"User **{recipient.UserName}** has been given permission **{permission.Name}**.");
        }

        [RequireCustomPermission(1)]
        [Command("removepermission")]
        public async Task RemovePermissionAsync()
        {

        }

        [RequireCustomPermission(1)]
        [Command("ban")]
        public async Task BanUserAsync()
        {

        }

        [RequireCustomPermission(1)]
        [Command("unban")]
        public async Task UnbanUserAsync()
        {

        }

        [RequireCustomPermission(1)]
        [Command("newpermission")]
        public async Task AddNewPermissionAsync(string permissionName, string description)
        {
            await _handler.AddNewPermissionAsync(permissionName, description);
            var permission = await _handler.FindNewestPermissionAsync();
            await Context.Channel.SendMessageAsync($"Added permission \"**{permissionName}**\" with permissionId **{permission.PermissionId}**.");
        }

        [RequireCustomPermission(1)]
        [Command("deletepermission")]
        public async Task DeleteExistingPermissionAsync()
        {

        }

        [RequireCustomPermission(1)]
        [Command("editpermission")]
        public async Task EditPermissionAsync()
        {

        }

        [RequireCustomPermission(1)]
        [Command("listallpermissions")]
        public async Task ListAllPermissionsAsync()
        {
            var permissions = await _handler.GetAllAsync();
            if (permissions.Count == 0)
            {
                await Context.Channel.SendMessageAsync("There are no permissions currently saved.");
                return;
            }

            await Context.Channel.SendMessageAsync(await _handler.ListAllAsync());
        }

        [Command("listpermissionsfor")]
        public async Task ListPermissionsFor(string username = null)
        {
            User recipient;
            if (username is null)
            {
                recipient = await _handler.FindUserByDiscordIdAsync(Context.User.Id);
            }
            else
            {
                recipient = await _handler.FindUserByNameAsync(username);
                if (recipient is null)
                {
                    await Context.Channel.SendMessageAsync("Could not find a user with that username.");
                    return;
                }
            }
            await Context.Channel.SendMessageAsync(_handler.ListPermissionsAsync(recipient));
        }
    }
}