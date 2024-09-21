using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MiraBot.DataAccess.Repositories;

namespace MiraBot.Permissions
{
    public class RequireCustomPermissionAttribute : PreconditionAttribute
    {
        private readonly int _permissionId;
        private readonly PermissionsRepository _permissionsRepository;
        private readonly UsersRepository _usersRepository;
        public RequireCustomPermissionAttribute(int permissionId, PermissionsRepository repository, UsersRepository usersRepository)
        {
            _permissionId = permissionId;
            _permissionsRepository = repository;
            _usersRepository = usersRepository;
        }
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var owner = await _usersRepository.GetUserByDiscordIdAsync(context.User.Id);
            if (await _permissionsRepository.UserHasPermission(owner.UserId, _permissionId))
            {
                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
            else return await Task.FromResult(PreconditionResult.FromError("User does not have permission to access this command."));
        }
    }
}