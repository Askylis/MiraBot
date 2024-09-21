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
        private readonly string _name;
        private readonly PermissionsRepository _permissionsRepository;
        private readonly UsersRepository _usersRepository;
        public RequireCustomPermissionAttribute(string name, PermissionsRepository repository, UsersRepository usersRepository)
        {
            _name = name;
            _permissionsRepository = repository;
            _usersRepository = usersRepository;
        }
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var owner = await _usersRepository.GetUserByDiscordIdAsync(context.User.Id);
            if (owner.Permissions.Contains(_name))
            {
                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
            else return await Task.FromResult(PreconditionResult.FromError("User does not have permission to access this command."));
        }
    }
}