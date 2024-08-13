using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace MiraBot.DataAccess.Repositories
{
    public class MiramindersRepository
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
                context.Reminders.Add(reminder);
                await context.SaveChangesAsync();
            }
        }

        public async Task AddNewUserAsync(User user)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Reminder>> GetRemindersAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Reminders.ToListAsync();
            }
        }

        public async Task MarkCompletedAsync(Reminder completed)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var reminder = await context.Reminders.FirstOrDefaultAsync(r => r.ReminderId == completed.ReminderId);
                reminder.IsCompleted = true;
                await context.SaveChangesAsync();
            }
        }

        public async Task<User> GetUserByNameAsync(string userName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            }
        }

        public async Task<User> GetUserByDiscordIdAsync(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await GetUserByDiscordIdAsync(discordId, context);
            }
        }

        private async Task<User> GetUserByDiscordIdAsync(ulong discordId, MiraBotContext ctx)
        {
            return await ctx.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);
        }

        public async Task<User> GetUserByUserId(int userId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            }
        }

        public async Task<string?> GetUserTimeZone(ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var user = await context.Users.Where(u => u.DiscordId == discordId).FirstOrDefaultAsync();
                return user?.TimeZone;
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