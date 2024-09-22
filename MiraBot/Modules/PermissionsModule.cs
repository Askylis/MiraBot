using Discord.Interactions;
using MiraBot.DataAccess;
using MiraBot.Permissions;

namespace MiraBot.Modules
{
    public class PermissionsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PermissionsHandler _handler;
        private readonly ModuleHelpers _helpers;

        public PermissionsModule(PermissionsHandler handler, ModuleHelpers helpers)
        {
            _handler = handler;
            _helpers = helpers;
        }

        [RequireCustomPermission(1)]
        [SlashCommand("addpermission", "Add a permission to a user.")]
        public async Task AddPermissionAsync(string username = null)
        {
            User recipient;
            var permissions = await _handler.GetAllAsync();
            if (username is null)
            {
                recipient = await _handler.FindUserByDiscordIdAsync(Context.User.Id);
            }
            else
            {
                recipient = await _handler.FindUserByNameAsync(username);
                if (recipient is null)
                {
                    await RespondAsync("Could not find a user with that username.");
                    return;
                }
            }
            
            await RespondAsync($"Which permission would you like to add to user **\"{recipient.UserName}\"**?");
            var validPerms = await _handler.GetAllAsync();
            validPerms = validPerms
                .Where(p => !recipient.Permissions.Any(rp => rp.PermissionId == p.PermissionId))
                .ToList();
            if (validPerms.Count == 0)
            {
                await ReplyAsync("There are no permissions available to assign to this user.");
                return;
            }

            await ReplyAsync(await _handler.ListAllAsync(validPerms));
            int selection = await _helpers.GetValidNumberAsync(1, permissions.Count, Context);
            await _handler.AddPermissionToUserAsync(recipient, permissions[selection - 1]);
            await ReplyAsync($"User **{recipient.UserName}** has been given permission **{permissions[selection - 1].Name}**.");
        }

        [RequireCustomPermission(1)]
        [SlashCommand("removepermission", "Remove a permission from a user.")]
        public async Task RemovePermissionAsync()
        {

        }

        [RequireCustomPermission(1)]
        [SlashCommand("ban", "Blacklist a user from using Mira.")]
        public async Task BanUserAsync()
        {

        }

        [RequireCustomPermission(1)]
        [SlashCommand("unban", "Unban a user who was previously blacklisted from using Mira.")]
        public async Task UnbanUserAsync()
        {

        }

        [RequireCustomPermission(1)]
        [SlashCommand("newpermission", "Add a new permission to the database.")]
        public async Task AddNewPermissionAsync(string permissionName, string description)
        {
            await DeferAsync();
            await _handler.AddNewPermissionAsync(permissionName, description);
            var permission = await _handler.FindPermissionAsync(permissionName);
            await FollowupAsync($"Added permission \"**{permissionName}**\" with permissionId **{permission.PermissionId}**.");
        }

        [RequireCustomPermission(1)]
        [SlashCommand("deletepermission", "Deletes an existing permission from the database.")]
        public async Task DeleteExistingPermissionAsync()
        {

        }

        [RequireCustomPermission(1)]
        [SlashCommand("editpermission", "Edit information about an existing permission.")]
        public async Task EditPermissionAsync()
        {

        }

        [RequireCustomPermission(1)]
        [SlashCommand("listallpermissions", "Provides a list of all available permissions.")]
        public async Task ListAllPermissionsAsync()
        {
            await DeferAsync();
            var permissions = await _handler.GetAllAsync();
            if (permissions.Count == 0)
            {
                await FollowupAsync("There are no permissions currently saved.");
                return;
            }

            await FollowupAsync(await _handler.ListAllAsync());
        }

        [SlashCommand("listpermissionsfor", "Lists all permissions that a specified user has.")]
        public async Task ListPermissionsFor(string username = null)
        {
            await DeferAsync();
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
                    await RespondAsync("Could not find a user with that username.");
                    return;
                }
            }
            await FollowupAsync(_handler.ListPermissionsAsync(recipient));
        }
    }
}