using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiraBot.DataAccess.Exceptions;

namespace MiraBot.DataAccess.Repositories
{
    public class MiramindersRepository : IMiramindersRepository
    {
        private readonly DatabaseOptions _databaseOptions;

        public MiramindersRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            _databaseOptions = databaseOptions.Value;
        }

        public async Task AddReminderAsync(Reminder reminder)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                await context.Reminders.AddAsync(reminder)
                    .ConfigureAwait(false);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task UpdateReminderAsync(Reminder reminder)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var original = context.Reminders.Find(reminder.ReminderId);
                if (original is null)
                {
                    return;
                }

                context.Entry(original).CurrentValues.SetValues(reminder);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
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

        public async Task<List<Reminder>> GetAllRemindersAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Reminders.ToListAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task RemoveReminderAsync(int reminderId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var reminder = await context.Reminders.FirstOrDefaultAsync(r => r.ReminderId == reminderId)
                    .ConfigureAwait(false)
                    ?? throw new ReminderNotFoundException();

                reminder.IsCompleted = true;
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<User?> GetUserByNameAsync(string userName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users.FirstOrDefaultAsync(u => u.UserName == userName)
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
            return await ctx.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId)
                .ConfigureAwait(false);
        }

        public async Task<User?> GetUserByUserId(int userId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
                    .ConfigureAwait(false);
            }
        }

        public async Task<List<Reminder>> GetUpcomingRemindersAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var reminders =  await context
                    .Reminders
                    .Where(r => !r.IsCompleted)
                    .ToListAsync()
                    .ConfigureAwait(false);
                return reminders;
            }
        }

        public async Task<User?> GetUserByUserIdAsync(int userId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context
                    .Users
                    .FindAsync(userId)
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
    }
}