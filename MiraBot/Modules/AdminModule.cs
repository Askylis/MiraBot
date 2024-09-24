using Discord.Commands;
using MiraBot.Common;
using MiraBot.DataAccess;
using MiraBot.Permissions;

namespace MiraBot.Modules
{
    [RequireCustomPermission(1)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        private readonly PermissionsHandler _handler;
        private readonly ModuleHelpers _helper;

        public AdminModule(PermissionsHandler handler, ModuleHelpers helper)
        {
            _handler = handler;
            _helper = helper;
        }

        [Command("addpermission")]
        public async Task AddPermissionAsync(ulong discordId, int permissionId)
        {
            var recipient = await _handler.FindUserByDiscordIdAsync(discordId);
            if (recipient is null)
            {
                await Context.Channel.SendMessageAsync($"Could not find the specified user.");
            }

            var permission = await _handler.FindPermissionAsync(permissionId);
            if (permission is null)
            {
                await Context.Channel.SendMessageAsync($"Unable to find a permission with the specified ID.");
            }

            await _handler.AddPermissionToUserAsync(recipient, permission);
            await Context.Channel.SendMessageAsync($"User **{recipient.UserName}** has been given permission **{permission.Name}**.");
        }

        [Command("removepermission")]
        public async Task RemovePermissionAsync(ulong discordId, int permissionId)
        {

        }

        [Command("ban")]
        public async Task BanUserAsync(ulong discordId)
        {
            await _handler.BanUserAsync(discordId);
            await Context.Channel.SendMessageAsync($"This user has been banned.");
        }

        [Command("unban")]
        public async Task UnbanUserAsync(ulong discordId)
        {
            await _handler.UnbanUserAsync(discordId);
            await Context.Channel.SendMessageAsync($"This user has been unbanned.");
        }

        [Command("newpermission")]
        public async Task AddNewPermissionAsync(string permissionName, string description)
        {
            await _handler.AddNewPermissionAsync(permissionName, description);
            var permission = await _handler.FindNewestPermissionAsync();
            await Context.Channel.SendMessageAsync($"Added permission \"**{permissionName}**\" with permissionId **{permission.PermissionId}**.");
        }

        [Command("deletepermission")]
        public async Task DeleteExistingPermissionAsync()
        {

        }

        [Command("editpermission")]
        public async Task EditPermissionAsync()
        {

        }

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
        public async Task ListPermissionsFor(ulong discordId)
        {
            var recipient = await _handler.FindUserByDiscordIdAsync(discordId);
            if (recipient is null)
            {
                await Context.Channel.SendMessageAsync("Could not find a user with that Discord ID.");
                return;
            }

            await Context.Channel.SendMessageAsync(_handler.ListPermissionsAsync(recipient));
        }

        [Command("bugs")]
        public async Task ListBugsAsync()
        {
            var bugs = await _helper.GetAllBugsAsync();
            // write bug info to file and send it
        }

        [Command("bugfixed")]
        public async Task FixBugAsync(int bugId)
        {
            await _helper.MarkBugAsFixedAsync(bugId);
            await Context.Channel.SendMessageAsync($"Bug with bug ID {bugId} has been marked as resolved.");
        }
    }
}