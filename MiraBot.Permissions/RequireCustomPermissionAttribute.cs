using Discord.Commands;

namespace MiraBot.Permissions
{
    public class RequireCustomPermissionAttribute : PreconditionAttribute
    {
        private readonly int _permissionId;
        public RequireCustomPermissionAttribute(int permissionId)
        {
            _permissionId = permissionId;
        }
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var handler = (PermissionsHandler)services.GetService(typeof(PermissionsHandler));
            if (handler == null)
            {
                return PreconditionResult.FromError("PermissionsHandler not available.");
            }

            var owner = await handler.FindUserByDiscordIdAsync(context.User.Id);
            if (await handler.UserHasPermissionAsync(owner.UserId, _permissionId))
            {
                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
            else return await Task.FromResult(PreconditionResult.FromError("User does not have permission to access this command."));
        }
    }
}