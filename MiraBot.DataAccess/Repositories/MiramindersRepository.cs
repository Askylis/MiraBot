using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<Reminder>> GetRemindersAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Reminders.ToListAsync();
            }
        }

        public async Task<string> GetUserTimeZone(string userName)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var user = context.Users.Where(u => u.UserName == userName).FirstOrDefault();
                return user.TimeZone;
            }
        }
    }
}