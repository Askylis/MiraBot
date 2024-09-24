using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata.Ecma335;

namespace MiraBot.DataAccess.Repositories
{
    public class BugRepository
    {
        private readonly DatabaseOptions _databaseOptions;
        public BugRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            _databaseOptions = databaseOptions.Value;
        }

        public async Task SaveBugAsync(Bug bug, ulong discordId)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordId);
                bug.User = user;
                await context.Bugs.AddAsync(bug)
                    .ConfigureAwait(false);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<Bug> FindBugAsync(int id)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Bugs.FindAsync(id);
            }
        }

        public async Task<Bug> FindNewestBugAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Bugs
                    .OrderByDescending(b => b.Id)
                    .LastOrDefaultAsync();
            }
        }

        public async Task<List<Bug>> GetAllAsync()
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                return await context.Bugs
                    .Where(b => !b.IsFixed)
                    .ToListAsync();
            }
        }

        public async Task MarkBugAsFixedAsync(int id)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                var bug = await context.Bugs.FindAsync(id);
                bug.IsFixed = true;
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}
