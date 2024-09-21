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
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {

            }
        }

        public async Task AddNewPermissionAsync(Permission permission)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                await context.Permissions.AddAsync(permission)
                    .ConfigureAwait(false);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<Permission> FindByNameAsync(string name)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Permissions.FirstOrDefaultAsync(p => p.Name == name)
                    .ConfigureAwait(false);
            }
        }

        public async Task<Permission> FindByIdAsync(int id)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Permissions.FirstOrDefaultAsync(p => p.PermissionId == id)
                    .ConfigureAwait(false);
            }
        }

        public async Task DeletePermissionAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {

            }
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