using MiraBot.DataAccess;
using MiraBot.DataAccess.Repositories;
using System.Text;

namespace MiraBot.Permissions
{
    public class PermissionsHandler
    {
        private readonly PermissionsRepository _repository;
        private readonly UsersRepository _usersRepository;

        public PermissionsHandler(PermissionsRepository permissionsRepository, UsersRepository usersRepository)
        {
            _repository = permissionsRepository;
            _usersRepository = usersRepository;
        }

        public async Task AddNewPermissionAsync(string name, string description)
        {
            var permission = new Permission { Name = name, Description = description};
            await _repository.AddNewPermissionAsync(permission);
        }

        public async Task AddPermissionToUserAsync(User user, Permission permission)
        {
            user.Permissions.Add(permission);
            await _usersRepository.ModifyUserAsync(user);
        }

        public async Task RemovePermissionFromUserAsync(User user, Permission permission)
        {
            user.Permissions.Remove(permission);
            await _usersRepository.ModifyUserAsync(user);
        }

        public async Task<List<Permission>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Permission> FindPermissionAsync(string name)
        {
            return await _repository.FindByNameAsync(name);
        }

        public async Task<User> FindUserByNameAsync(string name)
        {
            return await _usersRepository.GetUserByNameAsync(name);
        }

        public async Task<User> FindUserByDiscordIdAsync(ulong id)
        {
            return await _usersRepository.GetUserByDiscordIdAsync(id);
        }

        public async Task<bool> UserHasPermissionAsync(int userId, int permissionId)
        {
            return await _repository.UserHasPermissionAsync(userId, permissionId);
        }

        public async Task<string> ListAllAsync()
        {
            var permissions = await _repository.GetAllAsync();
            var sb = new StringBuilder();
            int counter = 1;
            foreach (var permission in permissions)
            {
                sb.Append($"{counter}. Permission name: **{permission.Name}** - Permission ID: **{permission.PermissionId}**");
                counter++;
            }

            return sb.ToString();
        }
    }
}