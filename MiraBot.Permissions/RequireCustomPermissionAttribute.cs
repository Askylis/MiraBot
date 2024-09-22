using Discord;
using Discord.Interactions;
using System.Text;

namespace MiraBot.Permissions
{
    public class RequireCustomPermissionAttribute : PreconditionAttribute
    {
        private readonly int _permissionId;
        public RequireCustomPermissionAttribute(int permissionId)
        {
            _permissionId = permissionId;
        }
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            var handler = (PermissionsHandler)services.GetService(typeof(PermissionsHandler));
            if (handler == null)
            {
                return PreconditionResult.FromError("PermissionsHandler not available.");
            }

            var owner = await handler.FindUserByDiscordIdAsync(context.User.Id);
            var sb = new StringBuilder();
            foreach (var permission in owner.Permissions)
            {
                sb.AppendLine(permission.Name);
            }
            Console.WriteLine($"{owner.UserName} used a command that requires permissions.");
            Console.WriteLine($"This command requires permission: {_permissionId}.");
            Console.WriteLine($"{owner.UserName} has the following permissions: {sb}");
            if (await handler.UserHasPermissionAsync(owner.UserId, _permissionId))
            {
                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
            else return await Task.FromResult(PreconditionResult.FromError("User does not have permission to access this command."));
        }
    }
}