using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiraBot.DataAccess.Repositories
{
    public class PermissionsRepository
    {
        private readonly DatabaseOptions _databaseOptions;
        private readonly UsersRepository _usersRepository;

        public PermissionsRepository(IOptions<DatabaseOptions> databaseOptions, UsersRepository usersRepository)
        {
            _databaseOptions = databaseOptions.Value;
            _usersRepository = usersRepository;
        }

        public async Task<bool> UserHasPermission(int userId, int permissionId)
        {
            var user = await _usersRepository.GetUserByUserIdAsync(userId);
            return user.Permissions.Any(p => p.PermissionId == permissionId);
        }

        public async Task UpdateUserPermissionsAsync()
        {

        }

        public async Task AddNewPermission()
        {

        }

        public async Task DeletePermissionAsync()
        {

        }

        public async Task<List<Permission>> GetAllAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Permissions.ToListAsync().ConfigureAwait(false);
            }
        }
    }
}