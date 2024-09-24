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

        public async Task<bool> UserHasPermissionAsync(int userId, int permissionId)
        {
            var user = await _usersRepository.GetUserByUserIdAsync(userId);
            return user.Permissions.Any(p => p.PermissionId == permissionId);
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

        public async Task<Permission> FindNewestPermissionAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Permissions.LastAsync();
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

        public async Task BanUserAsync(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId).ConfigureAwait(false);
                user.IsBanned = true;
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task UnbanUserAsync(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId).ConfigureAwait(false);
                user.IsBanned = false;
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<bool> UserIsBannedAsync(ulong discordId)
        {
            var user = await _usersRepository.GetUserByDiscordIdAsync(discordId);
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return user.IsBanned;
            }
        }
    }
}