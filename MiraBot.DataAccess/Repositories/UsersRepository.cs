using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiraBot.DataAccess.Repositories
{
    public class UsersRepository
    {
        private readonly DatabaseOptions _databaseOptions;
        public UsersRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            _databaseOptions = databaseOptions.Value;
        }

        public async Task AddNewUserAsync(User user)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                await context.Users.AddAsync(user)
                    .ConfigureAwait(false);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<User?> GetUserByNameAsync(string userName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users
                    .Include(p => p.Permissions)
                    .FirstOrDefaultAsync(u => u.UserName == userName)
                    .ConfigureAwait(false);
            }
        }

        public async Task<User?> GetUserByDiscordIdAsync(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await GetUserByDiscordIdAsync(discordId, context)
                    .ConfigureAwait(false);
            }
        }

        private static async Task<User?> GetUserByDiscordIdAsync(ulong discordId, MiraBotContext ctx)
        {
            return await ctx.Users
                .Include(p => p.Permissions)
                .FirstOrDefaultAsync(u => u.DiscordId == discordId)
                .ConfigureAwait(false);
        }


        public async Task<User?> GetUserByUserIdAsync(int userId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context
                    .Users
                    .Include(p => p.Permissions)
                    .FirstOrDefaultAsync(u => u.UserId == userId)
                    .ConfigureAwait(false);
            }
        }

        public async Task<List<UserNameAndId>> GetUserNamesAndIdsAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users.Select(u => new UserNameAndId(u.UserName, u.UserId)).ToListAsync();
            }
        }

        public async Task ModifyUserAsync(User user)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var dbUser = context.Users.Find(user.UserId);
                if (dbUser is null)
                {
                    return;
                }

                context.Entry(dbUser).CurrentValues.SetValues(user);

                await context
                    .SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task UpdatePermissionsAsync(User user, Permission permission)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var dbUser = context.Users.Find(user.UserId);
                var dbPermission = context.Permissions.Find(permission.PermissionId);
                if (dbUser is null)
                {
                    return;
                }

                if (!dbUser.Permissions.Any(p => p.PermissionId == permission.PermissionId))
                {
                    dbUser.Permissions.Add(dbPermission);

                    await context.SaveChangesAsync()
                        .ConfigureAwait(false);
                }
            }
        }

        public async Task<bool> UserExistsAsync(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users.AnyAsync(u => u.DiscordId == discordId);
            }
        }
    }
}