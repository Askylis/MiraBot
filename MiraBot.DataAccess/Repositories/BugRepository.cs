using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiraBot.DataAccess.Repositories
{
    public class BugRepository
    {
        private readonly DatabaseOptions _databaseOptions;
        public BugRepository(IOptions<DatabaseOptions> databaseOptions)
        {
            _databaseOptions = databaseOptions.Value;
        }

        public async Task SaveBugAsync(Bug bug)
        {
            using (var context = new MiraBotContext(_databaseOptions.ConnectionString))
            {
                await context.Bugs.AddAsync(bug)
                    .ConfigureAwait(false);
                await context.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}
