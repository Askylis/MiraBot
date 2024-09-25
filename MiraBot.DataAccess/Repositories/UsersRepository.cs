using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiraBot.DataAccess.Exceptions;

namespace MiraBot.DataAccess.Repositories
{
    public class UsersRepository : IUsersRepository
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
                var dbUser = await context.Users.FindAsync(user.UserId);
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
                var dbUser = await context.Users.FindAsync(user.UserId);
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

        public async Task BlacklistUserAsync(ulong recipientDiscordId, int senderId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var sender = await context.Users.FindAsync(senderId);
                var recipient = await context.Users.FirstOrDefaultAsync(r => r.DiscordId == recipientDiscordId);
                var existingBlacklist = await context.Blacklists.FirstOrDefaultAsync(b => b.SenderUserId == sender.UserId && b.RecipientUserId == recipient.UserId);
                if (existingBlacklist != null)
                {
                    throw new BlacklistAlreadyExistsException();
                }
                var blacklist = new Blacklist
                {
                    RecipientUserId = recipient.UserId,
                    SenderUserId = sender.UserId
                };

                context.Blacklists.Add(blacklist);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task WhitelistUserAsync(ulong recipientDiscordId, int senderId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var sender = await context.Users.FindAsync(senderId);
                var recipient = await context.Users.FirstOrDefaultAsync(r => r.DiscordId == recipientDiscordId);
                var existingWhitelist = await context.Whitelists.FirstOrDefaultAsync(w => w.SenderUserId == sender.UserId && w.RecipientUserId == recipient.UserId);
                if (existingWhitelist != null)
                {
                    throw new WhitelistAlreadyExistsException();
                }
                var whitelist = new Whitelist
                {
                    RecipientUserId = recipient.UserId,
                    SenderUserId = sender.UserId
                };

                context.Whitelists.Add(whitelist);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<bool> UserIsBlacklistedAsync(ulong senderDiscordId, int recipientId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var sender = await context.Users.FirstOrDefaultAsync(s => s.DiscordId == senderDiscordId);
                var recipient = await context.Users.FindAsync(recipientId);
                return await context.Blacklists
                    .AnyAsync(b => b.SenderUserId == sender.UserId && b.RecipientUserId == recipient.UserId);
            }
        }

        public async Task<bool> UserIsWhitelistedAsync(ulong senderDiscordId, int recipientId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var sender = await context.Users.FirstOrDefaultAsync(s => s.DiscordId == senderDiscordId);
                var recipient = await context.Users.FindAsync(recipientId);
                return await context.Whitelists
                    .AnyAsync(b => b.SenderUserId == sender.UserId && b.RecipientUserId == recipient.UserId);
            }
        }
    }
}