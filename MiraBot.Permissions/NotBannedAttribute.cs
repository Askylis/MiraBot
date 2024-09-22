using Discord;
using Discord.Interactions;

namespace MiraBot.Permissions
{
    public class NotBannedAttribute : PreconditionAttribute
    {
        public NotBannedAttribute() 
        { 

        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            var handler = (PermissionsHandler)services.GetService(typeof(PermissionsHandler));
            if (handler == null)
            {
                return PreconditionResult.FromError("PermissionsHandler not available.");
            }

            if (await handler.IsBannedAsync(context.User.Id))
            {
                return await Task.FromResult(PreconditionResult.FromError("User is banned."));
            }
            else return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
