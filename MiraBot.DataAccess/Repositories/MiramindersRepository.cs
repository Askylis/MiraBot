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
    }
}